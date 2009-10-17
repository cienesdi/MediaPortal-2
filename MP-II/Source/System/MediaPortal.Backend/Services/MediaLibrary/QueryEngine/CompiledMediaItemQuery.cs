#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Database;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Creates an SQL query for a media item query, executes the query and reads the result.
  /// </summary>
  public class CompiledMediaItemQuery
  {
    protected MIAM_Management _miamManagement;
    protected ICollection<MediaItemAspectMetadata> _necessaryRequestedMIAs;
    protected IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> _mainSelectAttributes;
    protected ICollection<MediaItemAspectMetadata.AttributeSpecification> _explicitSelectAttributes;
    protected CompiledFilter _filter;

    public CompiledMediaItemQuery(
        MIAM_Management miamManagement,
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIAs,
        IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> mainSelectedAttributes,
        ICollection<MediaItemAspectMetadata.AttributeSpecification> explicitSelectedAttributes,
        CompiledFilter filter)
    {
      _miamManagement = miamManagement;
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _mainSelectAttributes = mainSelectedAttributes;
      _explicitSelectAttributes = explicitSelectedAttributes;
      _filter = filter;
    }

    public IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> MainSelectAttributes
    {
      get { return _mainSelectAttributes; }
    }

    public ICollection<MediaItemAspectMetadata.AttributeSpecification> ExplicitSelectAttributes
    {
      get { return _explicitSelectAttributes; }
    }

    public CompiledFilter Filter
    {
      get { return _filter; }
    }

    public static CompiledMediaItemQuery Compile(MIAM_Management miamManagement, MediaItemQuery query,
        IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes)
    {
      ICollection<MediaItemAspectMetadata> necessaryMIAs = new List<MediaItemAspectMetadata>();
      // Raise exception if necessary MIA types are not present
      foreach (Guid miaTypeID in query.NecessaryRequestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          throw new InvalidDataException("Necessary MIA type '{0}' is not present in the media library", miaTypeID);
        necessaryMIAs.Add(miam);
      }
      // Raise exception if MIA types are not present, which are contained in filter condition
      CompiledFilter filter = CompiledFilter.Compile(miamManagement, query.Filter);
      foreach (QueryAttribute qa in filter.FilterAttributes)
      {
        MediaItemAspectMetadata miam = qa.Attr.ParentMIAM;
        if (!availableMIATypes.ContainsKey(miam.AspectId))
          throw new InvalidDataException("MIA type '{0}', which is contained in filter condition, is not present in the media library", miam.Name);
      }

      // Maps (all selected main) MIAM.Attributes to QueryAttributes
      IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> mainSelectedAttributes =
          new Dictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute>();

      // Attributes selected in explicit queries
      ICollection<MediaItemAspectMetadata.AttributeSpecification> explicitSelectAttributes =
          new List<MediaItemAspectMetadata.AttributeSpecification>();

      ICollection<Guid> requestedMIATypeIDs = CollectionUtils.UnionSet(
          query.NecessaryRequestedMIATypeIDs, query.OptionalRequestedMIATypeIDs);
      foreach (Guid miaTypeID in requestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          continue;
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications)
        {
          if (attr.Cardinality == Cardinality.Inline)
            mainSelectedAttributes[attr] = new QueryAttribute(attr);
          else
            explicitSelectAttributes.Add(attr);
        }
      }

      return new CompiledMediaItemQuery(miamManagement, necessaryMIAs,
          mainSelectedAttributes, explicitSelectAttributes, filter);
    }

    public IList<MediaItem> Execute()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command;

        // 1. Request all complex attributes
        IDictionary<long, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>> complexAttributeValues =
            new Dictionary<long, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>>();
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in _explicitSelectAttributes)
        {
          ComplexAttributeQueryBuilder complexAttributeQueryBuilder = new ComplexAttributeQueryBuilder(
              attr, _necessaryRequestedMIAs, _filter);
          command = transaction.CreateCommand();
          string mediaItemIdAlias;
          string valueAlias;
          command.CommandText = complexAttributeQueryBuilder.GenerateSqlStatement(_miamManagement, new Namespace(), false,
              out mediaItemIdAlias, out valueAlias);

          IDataReader reader = command.ExecuteReader();
          try
          {
            while (reader.Read())
            {
              long mediaItemId = reader.GetInt64(reader.GetOrdinal(mediaItemIdAlias));
              object value = reader.GetValue(reader.GetOrdinal(valueAlias));
              IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>> attributeValues;
              if (!complexAttributeValues.TryGetValue(mediaItemId, out attributeValues))
                attributeValues = complexAttributeValues[mediaItemId] =
                    new Dictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>();
              ICollection<object> values;
              if (!attributeValues.TryGetValue(attr, out values))
                values = attributeValues[attr] = new List<object>();
              values.Add(value);
            }
          }
          finally
          {
            reader.Close();
          }
        }

        // 2. Main query
        MainQueryBuilder mainQueryBuilder = new MainQueryBuilder(_necessaryRequestedMIAs,
            _mainSelectAttributes.Values, _filter);

        command = transaction.CreateCommand();
        string mediaItemIdAlias2;
        IDictionary<MediaItemAspectMetadata, string> miamAliases;
        Namespace mainQueryNS = new Namespace();
        // Maps (selected and filtered) QueryAttributes to CompiledQueryAttributes in the SQL query
        IDictionary<QueryAttribute, CompiledQueryAttribute> qa2cqa;
        command.CommandText = mainQueryBuilder.GenerateSqlStatement(_miamManagement, mainQueryNS, false, out mediaItemIdAlias2,
            out miamAliases, out qa2cqa);

        ICollection<MediaItemAspectMetadata> selectedMIAs = new HashSet<MediaItemAspectMetadata>();
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in CollectionUtils.UnionList(_mainSelectAttributes.Keys, _explicitSelectAttributes))
          selectedMIAs.Add(attr.ParentMIAM);

        IDataReader reader2 = command.ExecuteReader();
        try
        {
          IList<MediaItem> result = new List<MediaItem>();
          while (reader2.Read())
          {
            long mediaItemId = reader2.GetInt64(reader2.GetOrdinal(mediaItemIdAlias2));
            IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>> attributeValues;
            if (!complexAttributeValues.TryGetValue(mediaItemId, out attributeValues))
                attributeValues = null;
            MediaItem mediaItem = new MediaItem();
            foreach (MediaItemAspectMetadata miam in selectedMIAs)
            {
              if (reader2.IsDBNull(reader2.GetOrdinal(miamAliases[miam])))
                // MIAM is not available for current media item
                continue;
              MediaItemAspect mia = new MediaItemAspect(miam);
              foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications)
                if (attr.Cardinality == Cardinality.Inline)
                {
                  QueryAttribute qa = _mainSelectAttributes[attr];
                  CompiledQueryAttribute cqa = qa2cqa[qa];
                  mia.SetAttribute(attr, reader2.GetValue(reader2.GetOrdinal(cqa.GetAlias(mainQueryNS))));
                }
                else
                {
                  ICollection<object> values;
                  if (attributeValues != null && attributeValues.TryGetValue(attr, out values))
                    mia.SetCollectionAttribute(attr, values);
                }
              mediaItem.Aspects[miam.AspectId] = mia;
            }
            result.Add(mediaItem);
          }
          return result;
        }
        finally
        {
          reader2.Close();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }
  }
}