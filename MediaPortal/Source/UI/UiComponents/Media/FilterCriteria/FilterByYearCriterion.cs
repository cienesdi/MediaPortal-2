#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which filters by the year of the media item's recording time.
  /// </summary>
  public class FilterByYearCriterion : MLFilterCriterion
  {
    public const int MIN_YEAR = 2000;

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new List<FilterValue>();
      HomogenousMap valueGroups = cd.GetValueGroups(MediaAspect.ATTR_RECORDINGTIME, ProjectionFunction.DateToYear,
          necessaryMIATypeIds, filter, true);
      IList<FilterValue> result = new List<FilterValue>(valueGroups.Count);
      int numEmptyEntries = 0;
      foreach (KeyValuePair<object, object> group in valueGroups)
      {
        int? year = (int?) group.Key;
        if (year.HasValue)
        {
          result.Add(new FilterValue(year.Value.ToString(),
              new BooleanCombinationFilter(BooleanOperator.And, new IFilter[]
                {
                    new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.GE, new DateTime(year.Value, 1, 1)),
                    new RelationalFilter(MediaAspect.ATTR_RECORDINGTIME, RelationalOperator.LT, new DateTime(year.Value + 1, 1, 1)),
                }), (int) group.Value, this));
        }
        else
          numEmptyEntries += (int) group.Value;
      }
      if (numEmptyEntries > 0)
        result.Insert(0, new FilterValue(Consts.VALUE_EMPTY_TITLE, new EmptyFilter(MediaAspect.ATTR_RECORDINGTIME), numEmptyEntries, this));
      return result;
    }

    public override IFilter CreateFilter(FilterValue filterValue)
    {
      return (IFilter) filterValue.Value;
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter filter)
    {
      return null;
    }

    #endregion
  }
}
