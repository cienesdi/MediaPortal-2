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

using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.Plugins.BDHandler.Models
{
  public class BDMVPlayerModel
  {
    public const string MODEL_ID_STR = "1644E7AE-BBD1-43E5-8898-61956FA58CAF";
    public static AbstractProperty CurrentPlayer;

    #region Protected fields

    protected static ItemsList _bdmvFeatures;
    protected static AbstractProperty _bdmvTitleProperty;

    #endregion

    #region Constructor

    static BDMVPlayerModel()
    {
      _bdmvTitleProperty = new WProperty(typeof (string), string.Empty);
      CurrentPlayer = new SProperty(typeof(BDPlayer), null);
      CurrentPlayer.Attach(OnPlayerChanged);
    }

    protected static BDPlayer CurrentBDMVPlayer
    {
      get { return CurrentPlayer.GetValue() as BDPlayer; }
    }

    protected static void OnPlayerChanged(AbstractProperty property, object oldValue)
    {
      _bdmvFeatures = new ItemsList();

      if (CurrentBDMVPlayer == null)
        return;

      // Expose current title
      _bdmvTitleProperty.SetValue(CurrentBDMVPlayer.Title);

      // Copy feature information to list
      foreach (string dvdTitle in CurrentBDMVPlayer.DvdTitles)
      {
        string title = dvdTitle;
        ListItem item = new ListItem("Name", title)
                          {
                            Command = new MethodDelegateCommand(() => SelectFeature(title))
                          };
        _bdmvFeatures.Add(item);
      }
    }

    public static void SelectFeature(ListItem selectedItem)
    {
      if (selectedItem != null && selectedItem.Command != null)
        selectedItem.Command.Execute();
    }

    private static void SelectFeature(string title)
    {
      if (CurrentBDMVPlayer == null)
        return;

      CurrentBDMVPlayer.SetDvdTitle(title);
    }

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList BDMVFeatures
    {
      get { return _bdmvFeatures; }
    }
    
    public AbstractProperty BDMVTitleProperty
    {
      get { return _bdmvTitleProperty; }
    }

    public string BDMVTitle
    {
      get { return (string) _bdmvTitleProperty.GetValue(); }
      set { _bdmvTitleProperty.SetValue(value); }
    }
    #endregion
  }
}
