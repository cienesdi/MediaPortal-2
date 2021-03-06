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

using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.Media.Models
{
  public class DefaultVideoPlayerUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    protected static string[] EMPTY_STRING_ARRAY = new string[] { };

    protected MediaWorkflowStateType _mediaWorkflowStateType;

    #region Constructor & maintainance

    public DefaultVideoPlayerUIContributor() : base(300)
    {
      _subtitlesAvailableProperty = new WProperty(typeof(bool), false);
      StartTimer();
    }

    #endregion

    #region Variables

    protected ISubtitlePlayer _player;
    protected AbstractProperty _subtitlesAvailableProperty;
    protected string[] _subtitles = EMPTY_STRING_ARRAY;
    protected ItemsList _subtitleMenuItems;

    #endregion

    public AbstractProperty SubtitlesAvailableProperty
    {
      get { return _subtitlesAvailableProperty; }
    }

    public bool SubtitlesAvailable
    {
      get { return (bool) _subtitlesAvailableProperty.GetValue(); }
      set { _subtitlesAvailableProperty.SetValue(value); }
    }

    /// <summary>
    /// Provides a list of items to be shown in the subtitle selection menu.
    /// </summary>
    public ItemsList SubtitleMenuItems
    {
      get
      {
        _subtitleMenuItems.Clear();
        ISubtitlePlayer subtitlePlayer = _player;
        if (subtitlePlayer != null && _subtitles.Length > 0)
        {
          string currentSubtitle = subtitlePlayer.CurrentSubtitle;
           
          foreach (string subtitle in _subtitles)
          {
            // Use local variable, otherwise delegate argument is not fixed
            string localSubtitle = subtitle;

            ListItem item = new ListItem(Consts.KEY_NAME, localSubtitle)
            {
              Command = new MethodDelegateCommand(() => subtitlePlayer.SetSubtitle(localSubtitle)),
              // Check if it is the selected subtitle, then mark it
              Selected = localSubtitle == currentSubtitle
            };

            _subtitleMenuItems.Add(item);
          }
        }
        return _subtitleMenuItems;
      }
    }


    public bool BackgroundDisabled
    {
      get { return _mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent; }
    }

    public MediaWorkflowStateType MediaWorkflowStateType
    {
      get { return _mediaWorkflowStateType; }
    }

    public string Screen
    {
      get
      {
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return Consts.SCREEN_CURRENTLY_PLAYING_VIDEO;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return Consts.SCREEN_FULLSCREEN_VIDEO;
        return null;
      }
    }

    public void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      _mediaWorkflowStateType = stateType;
      _player = player as ISubtitlePlayer;
      _subtitleMenuItems = new ItemsList();
    }

    // Update GUI properties
    protected override void Update()
    {
      if (_player != null)
      {
        _subtitles = _player.Subtitles;
        SubtitlesAvailable = _subtitles.Length > 0;
      }
      else
        _subtitles = EMPTY_STRING_ARRAY;
    }

    /// <summary>
    /// Opens the subtitle selection dialog.
    /// </summary>
    public void OpenChooseSubtitleDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseSubtitle");
    }

    /// <summary>
    /// Execute selected menu item for subtitle and chapter selection.
    /// </summary>
    /// <param name="item">One of the items of <see cref="SubtitleMenuItems"/>.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    public void ShowZoomModeDialog()
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = pcm.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      PlayerConfigurationDialogModel.OpenChooseGeometryDialog(pc);
    }
  }
}