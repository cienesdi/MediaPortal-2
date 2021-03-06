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
using System.Threading;
using System.Timers;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.Core.Runtime;
using Timer = System.Timers.Timer;

namespace MediaPortal.UI.SkinEngine.SpecialElements.Controls
{
  /// <summary>
  /// Visible control providing the overview data for one player slot. This control can be decorated by different
  /// templates providing the player data.
  /// </summary>
  public class PlayerControl : SkinEngine.Controls.Visuals.Control
  {
    #region Consts

    public const string RES_NO_MEDIA_ITEM = "[PlayerControl.NoMediaItem]";
    public const string RES_NO_PLAYER = "[PlayerControl.NoPlayer]";
    public const string RES_PLAYER_PLAYING = "[PlayerControl.Playing]";
    public const string RES_PLAYER_PAUSED = "[PlayerControl.Paused]";
    public const string RES_PLAYER_SEEKING_FORWARD = "[PlayerControl.SeekingForward]";
    public const string RES_PLAYER_SEEKING_BACKWARD = "[PlayerControl.SeekingBackward]";
    public const string RES_PLAYER_SLOWMOTION_BACKWARD = "[PlayerControl.SlowMotionBackward]";
    public const string RES_PLAYER_SLOWMOTION_FORWARD = "[PlayerControl.SlowMotionForward]";
    public const string RES_PLAYER_STOPPED = "[PlayerControl.Stopped]";
    public const string RES_PLAYER_ENDED = "[PlayerControl.Ended]";
    public const string RES_PLAYER_ACTIVE = "[PlayerControl.Active]";
    public const string RES_UNKNOWN_MEDIA_ITEM = "[PlayerControl.UnknownMediaItem]";
    public const string RES_UNKNOWN_PLAYER_CONTEXT_NAME = "[PlayerControl.UnknownPlayerContextName]";
    public const string RES_HEADER_NORMAL = "[PlayerControl.HeaderNormal]";
    public const string RES_HEADER_PIP = "[PlayerControl.HeaderPiP]";
    public const string RES_PLAYBACK_RATE_HINT = "[PlayerControl.PlaybackRateHint]";

    public const string STR_STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG = "428326CE-9DE1-41ff-A33B-BBB80C8AFAC5";
    public static Guid STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG = new Guid(STR_STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG);

    public const string KEY_PLAYER_SLOT = "PlayerSlot";
    public const string KEY_SHOW_MUTE = "ShowMute";

    public static readonly ICollection<string> EMPTY_NAMES_COLLECTION = new List<string>().AsReadOnly();
    protected static readonly TimeSpan TIMEOUT_INFINITE = TimeSpan.FromMilliseconds(-1);

    #endregion

    #region Protected fields

    // Direct properties/fields
    protected AbstractProperty _playerContextProperty;
    protected AbstractProperty _autoVisibilityProperty;
    protected float _fixedVideoWidth;
    protected float _fixedVideoHeight;
    protected Timer _timer;
    protected bool _initialized = false;
    protected bool _updating = false;
    protected AsynchronousMessageQueue _messageQueue = null;
    protected ManualResetEvent _updateFinished = new ManualResetEvent(true);
    protected object _syncObj = new object();

    // Derived properties/fields
    protected MediaItem _currentMediaItem;
    protected AbstractProperty _isPlayerPresentProperty;
    protected AbstractProperty _isVideoPlayerPresentProperty;
    protected AbstractProperty _isPicturePlayerPresentProperty;
    protected AbstractProperty _titleProperty;
    protected AbstractProperty _mediaItemTitleProperty;
    protected AbstractProperty _nextMediaItemTitleProperty;
    protected AbstractProperty _hasNextMediaItemProperty;
    protected AbstractProperty _hasAudioProperty;
    protected AbstractProperty _volumeProperty;
    protected AbstractProperty _isAudioProperty;
    protected AbstractProperty _isMutedProperty;
    protected AbstractProperty _isPlayingProperty;
    protected AbstractProperty _isPausedProperty;
    protected AbstractProperty _isSeekingForwardProperty;
    protected AbstractProperty _isSeekingBackwardProperty;
    protected AbstractProperty _seekHintProperty;
    protected AbstractProperty _isCurrentPlayerProperty;
    protected AbstractProperty _percentPlayedProperty;
    protected AbstractProperty _currentTimeProperty;
    protected AbstractProperty _durationProperty;
    protected AbstractProperty _playerStateTextProperty;
    protected AbstractProperty _showMouseControlsProperty;
    protected AbstractProperty _canPlayProperty;
    protected AbstractProperty _canPauseProperty;
    protected AbstractProperty _canStopProperty;
    protected AbstractProperty _canSkipForwardProperty;
    protected AbstractProperty _canSkipBackProperty;
    protected AbstractProperty _canSeekForwardProperty;
    protected AbstractProperty _canSeekBackwardProperty;
    protected AbstractProperty _isPlayerActiveProperty;
    protected AbstractProperty _isPipProperty;
    protected AbstractProperty _videoWidthProperty;
    protected AbstractProperty _videoHeightProperty;
    protected AbstractProperty _videoGenreProperty;
    protected AbstractProperty _videoYearProperty;
    protected AbstractProperty _videoActorsProperty;
    protected AbstractProperty _videoStoryPlotProperty;
    protected AbstractProperty _audioArtistsProperty;
    protected AbstractProperty _audioYearProperty;
    protected AbstractProperty _pictureSourcePathProperty;
    protected AbstractProperty _pictureRotateDegreesProperty;

    protected AbstractProperty _fullscreenContentWFStateIDProperty;
    protected AbstractProperty _currentlyPlayingWFStateIDProperty;

    protected IResourceString _headerNormalResource;
    protected IResourceString _headerPiPResource;
    protected IResourceString _playbackRateHintResource;

    protected IResourceLocator _currentPictureSourceLocator = null;
    protected ILocalFsResourceAccessor _currentPictureResourceAccessor = null;

    #endregion

    #region Ctor

    public PlayerControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _playerContextProperty = new SProperty(typeof(PlayerChoice), PlayerChoice.PrimaryPlayer);
      _autoVisibilityProperty = new SProperty(typeof(bool), false);
      _isPlayerPresentProperty = new SProperty(typeof(bool), false);
      _isVideoPlayerPresentProperty = new SProperty(typeof(bool), false);
      _isPicturePlayerPresentProperty = new SProperty(typeof(bool), false);
      _titleProperty = new SProperty(typeof(string), null);
      _mediaItemTitleProperty = new SProperty(typeof(string), null);
      _nextMediaItemTitleProperty = new SProperty(typeof(string), string.Empty);
      _hasNextMediaItemProperty = new SProperty(typeof(bool), false);
      _hasAudioProperty = new SProperty(typeof(bool), true);
      _volumeProperty = new SProperty(typeof(int), 0);
      _isAudioProperty = new SProperty(typeof(bool), false);
      _isMutedProperty = new SProperty(typeof(bool), false);
      _isPlayingProperty = new SProperty(typeof(bool), false);
      _isPausedProperty = new SProperty(typeof(bool), false);
      _isSeekingForwardProperty = new SProperty(typeof(bool), false);
      _isSeekingBackwardProperty = new SProperty(typeof(bool), false);
      _seekHintProperty = new SProperty(typeof(string), string.Empty);
      _isCurrentPlayerProperty = new SProperty(typeof(bool), false);
      _percentPlayedProperty = new SProperty(typeof(float), 0f);
      _currentTimeProperty = new SProperty(typeof(string), string.Empty);
      _durationProperty = new SProperty(typeof(string), string.Empty);
      _playerStateTextProperty = new SProperty(typeof(string), string.Empty);
      _showMouseControlsProperty = new SProperty(typeof(bool), false);
      _canPlayProperty = new SProperty(typeof(bool), false);
      _canPauseProperty = new SProperty(typeof(bool), false);
      _canStopProperty = new SProperty(typeof(bool), false);
      _canSkipForwardProperty = new SProperty(typeof(bool), false);
      _canSkipBackProperty = new SProperty(typeof(bool), false);
      _canSeekForwardProperty = new SProperty(typeof(bool), false);
      _canSeekBackwardProperty = new SProperty(typeof(bool), false);
      _isPlayerActiveProperty = new SProperty(typeof(bool), false);
      _isPipProperty = new SProperty(typeof(bool), false);
      _fixedVideoWidth = 0f;
      _fixedVideoHeight = 0f;
      _videoWidthProperty = new SProperty(typeof(float), 0f);
      _videoHeightProperty = new SProperty(typeof(float), 0f);
      _videoGenreProperty = new SProperty(typeof(string), string.Empty);
      _videoYearProperty = new SProperty(typeof(int?), null);
      _videoActorsProperty = new SProperty(typeof(IEnumerable<string>), EMPTY_NAMES_COLLECTION);
      _videoStoryPlotProperty = new SProperty(typeof(string), string.Empty);
      _pictureSourcePathProperty = new SProperty(typeof(string), string.Empty);
      _pictureRotateDegreesProperty = new SProperty(typeof(int), 0);

      _audioArtistsProperty = new SProperty(typeof(IEnumerable<string>), EMPTY_NAMES_COLLECTION);
      _audioYearProperty = new SProperty(typeof(int?), null);

      _fullscreenContentWFStateIDProperty = new SProperty(typeof(Guid?), null);
      _currentlyPlayingWFStateIDProperty = new SProperty(typeof(Guid?), null);

      _headerNormalResource = LocalizationHelper.CreateResourceString(RES_HEADER_NORMAL);
      _headerPiPResource = LocalizationHelper.CreateResourceString(RES_HEADER_PIP);
      _playbackRateHintResource = LocalizationHelper.CreateResourceString(RES_PLAYBACK_RATE_HINT);
    }

    void Attach()
    {
      _playerContextProperty.Attach(OnPropertyChanged);
      _autoVisibilityProperty.Attach(OnPropertyChanged);
      _isMutedProperty.Attach(OnMuteChanged);

      VisibilityProperty.Attach(OnVisibilityChanged);
    }

    void Detach()
    {
      _playerContextProperty.Detach(OnPropertyChanged);
      _autoVisibilityProperty.Detach(OnPropertyChanged);
      _isMutedProperty.Detach(OnMuteChanged);

      VisibilityProperty.Detach(OnVisibilityChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      PlayerControl pc = (PlayerControl) source;

      PlayerContext = pc.PlayerContext;
      AutoVisibility = pc.AutoVisibility;
      _fixedVideoWidth = pc._fixedVideoWidth;
      _fixedVideoHeight = pc._fixedVideoHeight;

      Attach();
      UpdateProperties();
    }

    public override void Dispose()
    {
      StopTimer();
      _updateFinished.Close();
      UnsubscribeFromMessages();
      base.Dispose();
    }

    #endregion

    #region Private & protected methods

    void OnVisibilityChanged(AbstractProperty property, object oldvalue)
    {
      CheckHeartBeat();
    }

    void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      UpdateProperties();
    }

    void OnMuteChanged(AbstractProperty prop, object oldValue)
    {
      if (!_initialized)
        // Avoid changing the player manager's mute state in the initialization phase
        return;
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Muted = IsMuted;
    }

    void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      lock (_syncObj)
      {
        if (_timer == null)
          // Avoid calls after timer was stopped
          return;
        _updateFinished.Reset();
      }
      CheckShowMouseControls();
      UpdateProperties();
      _updateFinished.Set();
    }

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        if (_messageQueue != null)
          return;
        _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
             PlayerManagerMessaging.CHANNEL,
             PlayerContextManagerMessaging.CHANNEL,
             SystemMessaging.CHANNEL,
          });
        _messageQueue.MessageReceived += OnMessageReceived;
        messageQueue = _messageQueue;
      }
      messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        if (_messageQueue == null)
          return;
        messageQueue = _messageQueue;
        _messageQueue = null;
      }
      messageQueue.Shutdown();
    }

    protected void StartTimer()
    {
      lock (_syncObj)
      {
        if (_timer != null)
          return;
        _timer = new Timer(200) {Enabled = true};
        _timer.Elapsed += OnTimerElapsed;
      }
    }

    protected void StopTimer()
    {
      lock (_syncObj)
      {
        if (_timer == null)
          return;
        _timer.Enabled = false;
        _timer.Dispose();
        _timer = null;
      }
      _updateFinished.WaitOne(TIMEOUT_INFINITE);
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
        UpdateProperties();
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
        UpdateProperties();
      else if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
          if (sss.CurrentState == SystemState.ShuttingDown)
          {
            UnsubscribeFromMessages();
            StopTimer();
          }
        }
      }
    }

    protected void CheckHeartBeat()
    {
      if (_allocated)
      {
        SubscribeToMessages();
        if (IsVisible)
        {
          UpdateProperties();
          StartTimer();
        }
        else
          StopTimer();
      }
      else
      {
        UnsubscribeFromMessages();
        StopTimer();
      }
    }

    protected IPlayerContext GetPlayerContext()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      return playerContextManager.GetPlayerContext(PlayerContext);
    }

    protected void CheckShowMouseControls()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      Screen screen = Screen;
      ShowMouseControls = inputManager.IsMouseUsed && screen != null && screen.HasInputFocus;
    }

    protected int GetRotationMetadata(MediaItem mediaItem)
    {
      MediaItemAspect pictureAspect;
      if (mediaItem != null && mediaItem.Aspects.TryGetValue(PictureAspect.ASPECT_ID, out pictureAspect))
      {
        Int32 rotationDegree;
        PictureAspect.OrientationToDegrees((int) pictureAspect[PictureAspect.ATTR_ORIENTATION], out rotationDegree);
        return rotationDegree;
      }
      return 0;
    }

    protected void UpdatePictureSourcePath(IResourceLocator locator)
    {
      if (_currentPictureSourceLocator != locator)
      {
        ILocalFsResourceAccessor oldAccessor = _currentPictureResourceAccessor;
        _currentPictureSourceLocator = locator;
        if (_currentPictureSourceLocator != null)
          try
          {
            _currentPictureResourceAccessor = _currentPictureSourceLocator.CreateLocalFsAccessor();
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("PlayerControl: Problem creating local filesystem accessor for picture '{0}'",
                e, _currentPictureSourceLocator);
            PictureSourcePath = null;
            return;
          }
        PictureSourcePath = _currentPictureResourceAccessor == null ? null : _currentPictureResourceAccessor.LocalFileSystemPath;
        if (oldAccessor != null)
          oldAccessor.Dispose();
      }
    }

    protected void DisposeCurrentPictureResourceAccessor()
    {
      if (_currentPictureResourceAccessor != null)
        _currentPictureResourceAccessor.Dispose();
      _currentPictureResourceAccessor = null;
    }

    // Hack! The following code was copied from PlayerConfigurationDialogModel.OpenAudioMenuDialog.
    // Actually, we are now allowed to access the SkinBase plugin, because we have not declared an explicit dependency...
    protected static void OpenAudioMenuDialog(int slotIndex, bool showMute)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG, new NavigationContextConfig
        {
          AdditionalContextVariables = new Dictionary<string, object>
            {
                {KEY_PLAYER_SLOT, slotIndex},
                {KEY_SHOW_MUTE, showMute}
            }
        });
    }

    protected void UpdateProperties()
    {
      if (_updating)
        return;
      _updating = true;
      try
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        IPlayerContext playerContext = playerContextManager.GetPlayerContext(PlayerContext);
        IPlayerSlotController playerSlotController = playerContext == null ? null : playerContext.PlayerSlotController;
        IPlayer player = playerSlotController == null ? null : playerSlotController.CurrentPlayer;
        int? slotIndex = playerSlotController == null ? (int?) null : playerSlotController.SlotIndex;

        if (playerContext == null)
        {
          FullscreenContentWFStateID = null;
          CurrentlyPlayingWFStateID = null;
        }
        else
        {
          FullscreenContentWFStateID = playerContext.FullscreenContentWorkflowStateId;
          CurrentlyPlayingWFStateID = playerContext.CurrentlyPlayingWorkflowStateId;
        }

        _currentMediaItem = playerContext == null ? null : playerContext.CurrentMediaItem;

        IsPlayerPresent = player != null;
        IVideoPlayer vp = player as IVideoPlayer;
        IPicturePlayer pp = player as IPicturePlayer;
        if (vp == null)
        {
          IsVideoPlayerPresent = false;
          VideoWidth = 0f;
          VideoHeight = 0f;
        }
        else
        {
          IsVideoPlayerPresent = true;
          if (FixedVideoWidth > 0f && FixedVideoHeight > 0f)
          {
            VideoWidth = FixedVideoWidth;
            VideoHeight = FixedVideoHeight;
          }
          else if (FixedVideoWidth > 0f)
          { // Calculate the video height from the width
            VideoWidth = FixedVideoWidth;
            VideoHeight = FixedVideoWidth*vp.VideoAspectRatio.Height/vp.VideoAspectRatio.Width;
          }
          else
          { // FixedVideoHeight > 0f
            VideoHeight = FixedVideoHeight;
            VideoWidth = FixedVideoHeight*vp.VideoAspectRatio.Width/vp.VideoAspectRatio.Height;
          }
        }
        if (pp == null)
        {
          IsPicturePlayerPresent = false;
          DisposeCurrentPictureResourceAccessor();
          PictureSourcePath = null;
          PictureRotateDegrees = 0;
        }
        else
        {
          IsPicturePlayerPresent = true;
          UpdatePictureSourcePath(pp.CurrentPictureResourceLocator);
          PictureRotateDegrees = GetRotationMetadata(_currentMediaItem);
        }

        MediaItemAspect mediaAspect;
        if (_currentMediaItem == null || !_currentMediaItem.Aspects.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
          mediaAspect = null;
        MediaItemAspect videoAspect;
        if (_currentMediaItem == null || !_currentMediaItem.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out videoAspect))
          videoAspect = null;
        MediaItemAspect audioAspect;
        if (_currentMediaItem == null || !_currentMediaItem.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out audioAspect))
          audioAspect = null;
        IsCurrentPlayer = slotIndex.HasValue && playerContextManager.CurrentPlayerIndex == slotIndex.Value;
        if (player == null)
        {
          Title = playerContext == null ? RES_NO_PLAYER : playerContext.Name;
          MediaItemTitle = RES_NO_MEDIA_ITEM;
          NextMediaItemTitle = string.Empty;
          HasNextMediaItem = false;
          HasAudio = false;
          IsAudio = false;
          IsPlaying = false;
          IsPaused = false;
          IsSeekingForward = false;
          IsSeekingBackward = false;
          SeekHint = string.Empty;
          PercentPlayed = 0f;
          CurrentTime = string.Empty;
          Duration = string.Empty;
          PlayerStateText = string.Empty;
          if (playerContext == null)
          {
            CanPlay = false;
            CanPause = false;
            CanStop = false;
            CanSkipBack = false;
            CanSkipForward = false;
            CanSeekBackward = false;
            CanSeekForward = false;
          }
          else
          {
            CanPlay = playerContext.CurrentMediaItem != null || playerContext.Playlist.ItemList.Count > 0;
            CanPause = false;
            CanStop = false;
            CanSkipBack = false;
            CanSkipForward = false;
            CanSeekBackward = false;
            CanSeekForward = false;
          }
          IsPlayerActive = false;
          IsPip = false;
        }
        else
        {
          IsPip = slotIndex == PlayerManagerConsts.SECONDARY_SLOT && (player is IVideoPlayer || player is IPicturePlayer);
          string pcName = LocalizationHelper.CreateResourceString(playerContext.Name).Evaluate();
          Title = IsPip ? _headerPiPResource.Evaluate(pcName) : _headerNormalResource.Evaluate(pcName);
          string mit = player.MediaItemTitle;
          if (mit == null)
          {
            if (mediaAspect != null)
              mit = mediaAspect[MediaAspect.ATTR_TITLE] as string;
            if (mit == null)
              mit = RES_UNKNOWN_MEDIA_ITEM;
          }
          MediaItemTitle = mit;
          MediaItem nextMediaItem = playerContext.Playlist[1];
          if (nextMediaItem == null)
          {
            NextMediaItemTitle = null;
            HasNextMediaItem = false;
          }
          else
          {
            MediaItemAspect nextMediaAspect;
            if (nextMediaItem.Aspects.TryGetValue(MediaAspect.ASPECT_ID, out nextMediaAspect))
            {
              NextMediaItemTitle = nextMediaAspect[MediaAspect.ATTR_TITLE] as string;
              HasNextMediaItem = !string.IsNullOrEmpty(NextMediaItemTitle);
            }
          }
          IVolumeControl volumeControl = player as IVolumeControl;
          Volume = volumeControl != null ? volumeControl.Volume : 0;

          HasAudio = player is IAudioPlayer || player is IVideoPlayer;
          IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
          IsAudio = playerSlotController.IsAudioSlot;
          TimeSpan currentTime = mediaPlaybackControl == null ? new TimeSpan() : mediaPlaybackControl.CurrentTime;
          TimeSpan duration = mediaPlaybackControl == null ? new TimeSpan() : mediaPlaybackControl.Duration;
          if (duration.TotalMilliseconds == 0)
          {
            PercentPlayed = 0;
            CurrentTime = string.Empty;
            Duration = string.Empty;
          }
          else
          {
            PercentPlayed = (float) (100*currentTime.TotalMilliseconds/duration.TotalMilliseconds);
            CurrentTime = FormattingUtils.FormatMediaDuration(currentTime);
            Duration = FormattingUtils.FormatMediaDuration(duration);
          }
          string seekHint = string.Empty;
          bool playing = false;
          bool paused = false;
          bool seekingForward = false;
          bool seekingBackward = false;
          switch (player.State)
          {
            case PlayerState.Active:
              if (mediaPlaybackControl == null)
              {
                playing = true;
                PlayerStateText = RES_PLAYER_ACTIVE;
              }
              else
              {
                if (mediaPlaybackControl.IsPaused)
                {
                  paused = true;
                  PlayerStateText = RES_PLAYER_PAUSED;
                }
                else if (mediaPlaybackControl.IsPlayingAtNormalRate)
                {
                  playing = true;
                  PlayerStateText = RES_PLAYER_PLAYING;
                }
                else
                {
                  string playerStateTextResource;
                  double playbackRate = mediaPlaybackControl.PlaybackRate;
                  string format = "#";
                  if (playbackRate > 1.0)
                  {
                    seekingForward = true;
                    playerStateTextResource = RES_PLAYER_SEEKING_FORWARD;
                  }
                  else if (playbackRate < -1.0)
                  {
                    seekingBackward = true;
                    playerStateTextResource = RES_PLAYER_SEEKING_BACKWARD;
                  }
                  else if (playbackRate > 0.0)
                  {
                    seekingForward = true;
                    playerStateTextResource = RES_PLAYER_SLOWMOTION_FORWARD;
                    format = "0.#";
                  }
                  else // playbackRate < 0.0
                  {
                    seekingBackward = true;
                    playerStateTextResource = RES_PLAYER_SLOWMOTION_BACKWARD;
                    format = "0.#";
                  }
                  seekHint = _playbackRateHintResource.Evaluate(string.Format("{0:" + format + "}", Math.Abs(playbackRate)));
                  PlayerStateText = LocalizationHelper.CreateResourceString(playerStateTextResource).Evaluate(seekHint);
                }
              }
              break;
            case PlayerState.Stopped:
              PlayerStateText = RES_PLAYER_STOPPED;
              break;
            case PlayerState.Ended:
              PlayerStateText = RES_PLAYER_ENDED;
              break;
          }
          IsPlaying = playing;
          IsPaused = paused;
          IsSeekingForward = seekingForward;
          IsSeekingBackward = seekingBackward;
          SeekHint = seekHint;
          IsPlayerActive = player.State == PlayerState.Active;
          CanPlay = mediaPlaybackControl == null || mediaPlaybackControl.IsPaused || seekingForward || seekingBackward;
          CanPause = mediaPlaybackControl != null && !mediaPlaybackControl.IsPaused && !mediaPlaybackControl.IsSeeking;
          CanStop = true;
          CanSkipBack = playerContext.Playlist.HasPrevious;
          CanSkipForward = playerContext.Playlist.HasNext;
          CanSeekBackward = mediaPlaybackControl != null && mediaPlaybackControl.CanSeekBackwards;
          CanSeekForward = mediaPlaybackControl != null && mediaPlaybackControl.CanSeekForwards;
        }
        if (mediaAspect == null)
        {
          VideoYear = null;
          AudioYear = null;
        }
        else
        {
          DateTime? recordingTime = (DateTime?) mediaAspect[MediaAspect.ATTR_RECORDINGTIME];
          VideoYear = recordingTime.HasValue ? (int?) recordingTime.Value.Year : null;
          AudioYear = VideoYear;
        }
        if (videoAspect == null)
        {
          VideoGenre = string.Empty;
          VideoActors = EMPTY_NAMES_COLLECTION;
          VideoStoryPlot = string.Empty;
        }
        else
        {
          VideoGenre = (string) videoAspect[VideoAspect.ATTR_GENRE];
          VideoActors = (IEnumerable<string>) videoAspect[VideoAspect.ATTR_ACTORS];
          VideoStoryPlot = (string) videoAspect[VideoAspect.ATTR_STORYPLOT];
        }
        if (audioAspect == null)
        {
          AudioArtists = EMPTY_NAMES_COLLECTION;
        }
        else
        {
          AudioArtists = (IEnumerable<string>) audioAspect[AudioAspect.ATTR_ARTISTS];
        }
        IsMuted = playerManager.Muted;
        CheckShowMouseControls();
        if (AutoVisibility)
        {
          bool isVisible = playerSlotController != null && playerSlotController.IsActive;
          SimplePropertyDataDescriptor dd;
          if (SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(this, "IsVisible", out dd))
            SetValueInRenderThread(dd, isVisible);
          else
            IsVisible = isVisible;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerControl: Error updating properties", e);
      }
      finally
      {
        _initialized = true;
        _updating = false;
      }
    }

    #endregion

    #region Public members to be accessed via the GUI

    #region Configuration properties, to be set from the outside

    public AbstractProperty PlayerContextProperty
    {
      get { return _playerContextProperty; }
    }

    /// <summary>
    /// Determines, which player's properties are tracked by this player control.
    /// </summary>
    public PlayerChoice PlayerContext
    {
      get { return (PlayerChoice) _playerContextProperty.GetValue(); }
      set { _playerContextProperty.SetValue(value); }
    }

    public AbstractProperty AutoVisibilityProperty
    {
      get { return _autoVisibilityProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, this <see cref="PlayerControl"/> will automatically show up when the underlaying
    /// player slot is active and will automatically hide when it is deactivated.
    /// </summary>
    public bool AutoVisibility
    {
      get { return (bool) _autoVisibilityProperty.GetValue(); }
      set { _autoVisibilityProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets a fixed width for the <see cref="VideoWidth"/> property. If <see cref="FixedVideoHeight"/> is set to
    /// <c>0</c>, the <see cref="VideoHeight"/> will be calculated automatically using the current player's aspect ratio.
    /// If both <see cref="FixedVideoWidth"/> and <see cref="FixedVideoHeight"/> are set, the player's aspect ratio will be
    /// ignored.
    /// </summary>
    public float FixedVideoWidth
    {
      get { return _fixedVideoWidth; }
      set { _fixedVideoWidth = value; }
    }

    /// <summary>
    /// Gets or sets a fixed height for the <see cref="VideoHeight"/> property. If <see cref="FixedVideoWidth"/> is set to
    /// <c>0</c>, the <see cref="VideoWidth"/> will be calculated automatically using the current player's aspect ratio.
    /// If both <see cref="FixedVideoWidth"/> and <see cref="FixedVideoHeight"/> are set, the player's aspect ratio will be
    /// ignored.
    /// </summary>
    public float FixedVideoHeight
    {
      get { return _fixedVideoHeight; }
      set { _fixedVideoHeight = value; }
    }

    #endregion

    #region Derived properties to update the GUI

    public AbstractProperty IsPlayerPresentProperty
    {
      get { return _isPlayerPresentProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player slot currently has a player.
    /// </summary>
    public bool IsPlayerPresent
    {
      get { return (bool) _isPlayerPresentProperty.GetValue(); }
      internal set { _isPlayerPresentProperty.SetValue(value); }
    }

    public AbstractProperty IsVideoPlayerPresentProperty
    {
      get { return _isVideoPlayerPresentProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player slot currently has a video player.
    /// </summary>
    public bool IsVideoPlayerPresent
    {
      get { return (bool) _isVideoPlayerPresentProperty.GetValue(); }
      internal set { _isVideoPlayerPresentProperty.SetValue(value); }
    }

    public AbstractProperty IsPicturePlayerPresentProperty
    {
      get { return _isPicturePlayerPresentProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player slot currently has a picture player.
    /// </summary>
    public bool IsPicturePlayerPresent
    {
      get { return (bool) _isPicturePlayerPresentProperty.GetValue(); }
      internal set { _isPicturePlayerPresentProperty.SetValue(value); }
    }

    public AbstractProperty TitleProperty
    {
      get { return _titleProperty; }
    }

    /// <summary>
    /// Gets the title of this player control, i.e. the name of the player, like "Video (PiP)".
    /// </summary>
    public string Title
    {
      get { return (string) _titleProperty.GetValue(); }
      internal set { _titleProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemTitleProperty
    {
      get { return _mediaItemTitleProperty; }
    }

    /// <summary>
    /// Gets the title of the current media item.
    /// </summary>
    public string MediaItemTitle
    {
      get { return (string) _mediaItemTitleProperty.GetValue(); }
      internal set { _mediaItemTitleProperty.SetValue(value); }
    }

    public AbstractProperty NextMediaItemTitleProperty
    {
      get { return _nextMediaItemTitleProperty; }
    }

    /// <summary>
    /// Gets the title of the next media item in the playlist or <c>null</c>.
    /// </summary>
    public string NextMediaItemTitle
    {
      get { return (string) _nextMediaItemTitleProperty.GetValue(); }
      set { _nextMediaItemTitleProperty.SetValue(value); }
    }

    public AbstractProperty HasNextMediaItemProperty
    {
      get { return _hasNextMediaItemProperty; }
    }

    /// <summary>
    /// <c>true</c>, if a next media item is available in the playlist. In that case,
    /// <see cref="NextMediaItemTitle"/> contains the title of the next media item.
    /// </summary>
    public bool HasNextMediaItem
    {
      get { return (bool) _hasNextMediaItemProperty.GetValue(); }
      set { _hasNextMediaItemProperty.SetValue(value); }
    }

    public AbstractProperty HasAudioProperty
    {
      get { return _hasAudioProperty; }
    }

    /// <summary>
    /// Gets the information if the current player context can provide an audio signal.
    /// </summary>
    public bool HasAudio
    {
      get { return (bool) _hasAudioProperty.GetValue(); }
      internal set { _hasAudioProperty.SetValue(value); }
    }

    public AbstractProperty VolumeProperty
    {
      get { return _volumeProperty; }
    }

    /// <summary>
    /// Gets the volume of the player.
    /// </summary>
    public int Volume
    {
      get { return (int) _volumeProperty.GetValue(); }
      internal set { _volumeProperty.SetValue(value); }
    }

    public AbstractProperty IsAudioProperty
    {
      get { return _isAudioProperty; }
    }

    /// <summary>
    /// Gets the information if the current player context currently provides the audio signal.
    /// </summary>
    public bool IsAudio
    {
      get { return (bool) _isAudioProperty.GetValue(); }
      internal set { _isAudioProperty.SetValue(value); }
    }

    public AbstractProperty IsMutedProperty
    {
      get { return _isMutedProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is the audio player but is muted.
    /// </summary>
    public bool IsMuted
    {
      get { return (bool) _isMutedProperty.GetValue(); }
      internal set { _isMutedProperty.SetValue(value); }
    }

    public AbstractProperty IsCurrentPlayerProperty
    {
      get { return _isCurrentPlayerProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is currently playing.
    /// </summary>
    public bool IsPlaying
    {
      get { return (bool) _isPlayingProperty.GetValue(); }
      internal set { _isPlayingProperty.SetValue(value); }
    }

    public AbstractProperty IsPlayingProperty
    {
      get { return _isPlayingProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is currently paused.
    /// </summary>
    public bool IsPaused
    {
      get { return (bool) _isPausedProperty.GetValue(); }
      internal set { _isPausedProperty.SetValue(value); }
    }

    public AbstractProperty IsPausedProperty
    {
      get { return _isPausedProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is currently seeking forward.
    /// </summary>
    public bool IsSeekingForward
    {
      get { return (bool) _isSeekingForwardProperty.GetValue(); }
      internal set { _isSeekingForwardProperty.SetValue(value); }
    }

    public AbstractProperty IsSeekingForwardProperty
    {
      get { return _isSeekingForwardProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is currently seeking backward.
    /// </summary>
    public bool IsSeekingBackward
    {
      get { return (bool) _isSeekingBackwardProperty.GetValue(); }
      internal set { _isSeekingBackwardProperty.SetValue(value); }
    }

    public AbstractProperty IsSeekingBackwardProperty
    {
      get { return _isSeekingBackwardProperty; }
    }

    /// <summary>
    /// Gets a string which contains the current seeking rate (for example: "2x").
    /// </summary>
    public string SeekHint
    {
      get { return (string) _seekHintProperty.GetValue(); }
      internal set { _seekHintProperty.SetValue(value); }
    }

    public AbstractProperty SeekHintProperty
    {
      get { return _seekHintProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is currently focused for remote or keyboard input.
    /// </summary>
    public bool IsCurrentPlayer
    {
      get { return (bool) _isCurrentPlayerProperty.GetValue(); }
      internal set { _isCurrentPlayerProperty.SetValue(value); }
    }

    public AbstractProperty PercentPlayedProperty
    {
      get { return _percentPlayedProperty; }
    }

    /// <summary>
    /// Gets a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public float PercentPlayed
    {
      get { return (float) _percentPlayedProperty.GetValue(); }
      internal set { _percentPlayedProperty.SetValue(value); }
    }

    public AbstractProperty CurrentTimeProperty
    {
      get { return _currentTimeProperty; }
    }

    /// <summary>
    /// Gets the current play time (or empty).
    /// </summary>
    public string CurrentTime
    {
      get { return (string) _currentTimeProperty.GetValue(); }
      internal set { _currentTimeProperty.SetValue(value); }
    }

    public AbstractProperty DurationProperty
    {
      get { return _durationProperty; }
    }

    /// <summary>
    /// Gets the duration of the current media item (or empty).
    /// </summary>
    public string Duration
    {
      get { return (string) _durationProperty.GetValue(); }
      internal set { _durationProperty.SetValue(value); }
    }

    public AbstractProperty PlayerStateTextProperty
    {
      get { return _playerStateTextProperty; }
    }

    /// <summary>
    /// Gets a string which denotes the current playing state, for example "Playing" or "Seeking forward (2x)".
    /// </summary>
    public string PlayerStateText
    {
      get { return (string) _playerStateTextProperty.GetValue(); }
      internal set { _playerStateTextProperty.SetValue(value); }
    }

    public AbstractProperty ShowMouseControlsProperty
    {
      get { return _showMouseControlsProperty; }
    }

    /// <summary>
    /// Gets the information if the mouse is being used, i.e. if mouse controls should be shown, if appropriate.
    /// </summary>
    public bool ShowMouseControls
    {
      get { return (bool) _showMouseControlsProperty.GetValue(); }
      internal set { _showMouseControlsProperty.SetValue(value); }
    }

    public AbstractProperty CanPlayProperty
    {
      get { return _canPlayProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is able to play in the current state, i.e. if the "Play" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanPlay
    {
      get { return (bool) _canPlayProperty.GetValue(); }
      internal set { _canPlayProperty.SetValue(value); }
    }

    public AbstractProperty CanPauseProperty
    {
      get { return _canPauseProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is able to pause in the current state, i.e. if the "Pause" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanPause
    {
      get { return (bool) _canPauseProperty.GetValue(); }
      internal set { _canPauseProperty.SetValue(value); }
    }

    public AbstractProperty CanStopProperty
    {
      get { return _canStopProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is able to stop in the current state, i.e. if the "Stop" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanStop
    {
      get { return (bool) _canStopProperty.GetValue(); }
      internal set { _canStopProperty.SetValue(value); }
    }

    public AbstractProperty CanSkipForwardProperty
    {
      get { return _canSkipForwardProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is able to skip forward in the current state, i.e. if the "SkipForward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSkipForward
    {
      get { return (bool) _canSkipForwardProperty.GetValue(); }
      internal set { _canSkipForwardProperty.SetValue(value); }
    }

    public AbstractProperty CanSkipBackProperty
    {
      get { return _canSkipBackProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is able to skip backward in the current state, i.e. if the "SkipBackward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSkipBack
    {
      get { return (bool) _canSkipBackProperty.GetValue(); }
      internal set { _canSkipBackProperty.SetValue(value); }
    }

    public AbstractProperty CanSeekForwardProperty
    {
      get { return _canSeekForwardProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is able to seek forward in the current state, i.e. if the "SeekForward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSeekForward
    {
      get { return (bool) _canSeekForwardProperty.GetValue(); }
      internal set { _canSeekForwardProperty.SetValue(value); }
    }

    public AbstractProperty CanSeekBackwardProperty
    {
      get { return _canSeekBackwardProperty; }
    }

    /// <summary>
    /// Gets the information if the underlaying player is able to seek backward in the current state, i.e. if the "SeekBackward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSeekBackward
    {
      get { return (bool) _canSeekBackwardProperty.GetValue(); }
      internal set { _canSeekBackwardProperty.SetValue(value); }
    }

    public AbstractProperty IsPlayerActiveProperty
    {
      get { return _isPlayerActiveProperty; }
    }

    /// <summary>
    /// Gets the activity state of the underlaying player. When <see cref="IsPlayerActive"/> is <c>true</c>,
    /// the underlaying player is either playing or paused or seeking. Else, it is stopped or ended.
    /// </summary>
    public bool IsPlayerActive
    {
      get { return (bool) _isPlayerActiveProperty.GetValue(); }
      internal set { _isPlayerActiveProperty.SetValue(value); }
    }

    public AbstractProperty IsPipProperty
    {
      get { return _isPipProperty; }
    }

    /// <summary>
    /// Gets the information whether a picture-in-picture player is playing.
    /// </summary>
    public bool IsPip
    {
      get { return (bool) _isPipProperty.GetValue(); }
      internal set { _isPipProperty.SetValue(value); }
    }

    public AbstractProperty VideoWidthProperty
    {
      get { return _videoWidthProperty; }
    }

    /// <summary>
    /// Gets the fixed or calculated video width.
    /// </summary>
    /// <remarks>
    /// This property returns the video size returned by the video player, if neither <see cref="FixedVideoWidth"/>
    /// nor <see cref="FixedVideoHeight"/> is set. If one of them is set, the other dimension is calculated from the
    /// fixed dimension according to the current video's aspect ratio. This can be used for displaying a
    /// picture-in-picture rectangle, for example.
    /// </remarks>
    public float VideoWidth
    {
      get { return (float) _videoWidthProperty.GetValue(); }
      internal set { _videoWidthProperty.SetValue(value); }
    }

    public AbstractProperty VideoHeightProperty
    {
      get { return _videoHeightProperty; }
    }

    /// <summary>
    /// Gets the fixed or calculated video height.
    /// </summary>
    /// <remarks>
    /// This property returns the video size returned by the video player, if neither <see cref="FixedVideoWidth"/>
    /// nor <see cref="FixedVideoHeight"/> is set. If one of them is set, the other dimension is calculated from the
    /// fixed dimension according to the current video's aspect ratio. This can be used for displaying a
    /// picture-in-picture rectangle, for example.
    /// </remarks>
    public float VideoHeight
    {
      get { return (float) _videoHeightProperty.GetValue(); }
      internal set { _videoHeightProperty.SetValue(value); }
    }

    public AbstractProperty VideoYearProperty
    {
      get { return _videoYearProperty; }
    }

    /// <summary>
    /// Gets the year of the currently playing video, if the current media item is a video item and if this
    /// information is available.
    /// </summary>
    public int? VideoYear
    {
      get { return (int?) _videoYearProperty.GetValue(); }
      internal set { _videoYearProperty.SetValue(value); }
    }

    public AbstractProperty VideoGenreProperty
    {
      get { return _videoGenreProperty; }
    }

    /// <summary>
    /// Gets the genre string of the currently playing video, if the current media item is a video item and if this
    /// information is available.
    /// </summary>
    public string VideoGenre
    {
      get { return (string) _videoGenreProperty.GetValue(); }
      internal set { _videoGenreProperty.SetValue(value); }
    }

    public AbstractProperty VideoActorsProperty
    {
      get { return _videoActorsProperty; }
    }

    /// <summary>
    /// Gets an enumeration of actor names of the currently playing video, if the current media item is a video item
    /// and if this information is available.
    /// </summary>
    public IEnumerable<string> VideoActors
    {
      get { return (IEnumerable<string>) _videoActorsProperty.GetValue(); }
      internal set { _videoActorsProperty.SetValue(value); }
    }

    public AbstractProperty VideoStoryPlotProperty
    {
      get { return _videoStoryPlotProperty; }
    }

    /// <summary>
    /// Gets the story plot of the currently playing video, if the current media item is a video item and if this
    /// information is available.
    /// </summary>
    public string VideoStoryPlot
    {
      get { return (string) _videoStoryPlotProperty.GetValue(); }
      internal set { _videoStoryPlotProperty.SetValue(value); }
    }

    public AbstractProperty PictureSourcePathProperty
    {
      get { return _pictureSourcePathProperty; }
    }

    /// <summary>
    /// Gets the local file path to the current picture, if <see cref="IsPicturePlayerPresent"/> is <c>true</c>.
    /// </summary>
    public string PictureSourcePath
    {
      get { return (string) _pictureSourcePathProperty.GetValue(); }
      set { _pictureSourcePathProperty.SetValue(value); }
    }

    public AbstractProperty PictureRotateDegreesProperty
    {
      get { return _pictureRotateDegreesProperty; }
    }

    /// <summary>
    /// Retuns the rotation of the current picture in degrees, if <see cref="IsPicturePlayerPresent"/> is <c>true</c>.
    /// </summary>
    public int PictureRotateDegrees
    {
      get { return (int) _pictureRotateDegreesProperty.GetValue(); }
      set { _pictureRotateDegreesProperty.SetValue(value); }
    }

    public AbstractProperty AudioArtistsProperty
    {
      get { return _audioArtistsProperty; }
    }

    /// <summary>
    /// Gets an enumeration of artist names of the currently playing audio, if the current media item is an audio item
    /// and if this information is available.
    /// </summary>
    public IEnumerable<string> AudioArtists
    {
      get { return (IEnumerable<string>) _audioArtistsProperty.GetValue(); }
      set { _audioArtistsProperty.SetValue(value); }
    }

    public AbstractProperty AudioYearProperty
    {
      get { return _audioYearProperty; }
    }

    /// <summary>
    /// Gets the year of the currently playing audio, if the current media item is an audio item and if this
    /// information is available.
    /// </summary>
    public int? AudioYear
    {
      get { return (int?) _audioYearProperty.GetValue(); }
      set { _audioYearProperty.SetValue(value); }
    }

    public AbstractProperty FullscreenContentWFStateIDProperty
    {
      get { return _fullscreenContentWFStateIDProperty; }
    }

    /// <summary>
    /// Gets the workflow ID of the "fullscreen content" workflow state for the current player.
    /// </summary>
    public Guid? FullscreenContentWFStateID
    {
      get { return (Guid?) _fullscreenContentWFStateIDProperty.GetValue(); }
      internal set { _fullscreenContentWFStateIDProperty.SetValue(value); }
    }

    public AbstractProperty CurrentlyPlayingWFStateIDProperty
    {
      get { return _currentlyPlayingWFStateIDProperty; }
    }

    /// <summary>
    /// Gets the workflow ID of the "currently playing" workflow state for the current player.
    /// </summary>
    public Guid? CurrentlyPlayingWFStateID
    {
      get { return (Guid?) _currentlyPlayingWFStateIDProperty.GetValue(); }
      internal set { _currentlyPlayingWFStateIDProperty.SetValue(value); }
    }

    #endregion

    /// <summary>
    /// Called from the skin if the user presses the "audio" button. This will move the audio to the player slot of the
    /// underlaying player, mute the player or show up the audio menu, depending on the current muted state and the
    /// number of available audio streams.
    /// </summary>
    public void AudioButtonPressed()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      IPlayerContext playerContext = GetPlayerContext();
      if (playerContext == null)
        return;
      IPlayerSlotController psc = playerContext.PlayerSlotController;
      AudioStreamDescriptor currentAudioStream;
      IList<AudioStreamDescriptor> audioStreamDescriptors =
          new List<AudioStreamDescriptor>(playerContext.GetAudioStreamDescriptors(out currentAudioStream));
      int slotIndex = psc.SlotIndex;
      if (audioStreamDescriptors.Count <= 1)
        if (IsAudio)
          playerManager.Muted ^= true;
        else
        {
          playerManager.AudioSlotIndex = slotIndex;
          playerManager.Muted = false;
        }
      else
        OpenAudioMenuDialog(slotIndex, IsAudio);
    }

    /// <summary>
    /// Called from the skin if the user presses the "play" button. This will start the underlaying player.
    /// </summary>
    public void Play()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      if (pc.PlaybackState == PlaybackState.Paused || pc.PlaybackState == PlaybackState.Seeking)
        pc.Play();
      else
        pc.Restart();
    }

    /// <summary>
    /// Called from the skin if the user presses the "pause" button. This will pause the underlaying player.
    /// </summary>
    public void Pause()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.Pause();
    }

    /// <summary>
    /// Called from the skin if the user presses the "toggle pause" button. This will start or pause the underlaying
    /// player, depending on the current play state.
    /// </summary>
    public void TogglePause()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      switch (pc.PlaybackState) {
        case PlaybackState.Playing:
          pc.Pause();
          break;
        case PlaybackState.Paused:
          pc.Play();
          break;
        default:
          pc.Restart();
          break;
      }
    }

    /// <summary>
    /// Called from the skin if the user presses the "stop" button. This will stop the underlaying player and perhaps
    /// close its player slot, if this behaviour is configured at the player manager.
    /// </summary>
    public void Stop()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.Stop();
    }

    /// <summary>
    /// Called from the skin if the user presses the "seek backward" button. This will start the seeking process in the
    /// underlaying player.
    /// </summary>
    public void SeekBackward()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.SeekBackward();
    }

    /// <summary>
    /// Called from the skin if the user presses the "seek forward" button. This will start the seeking process in the
    /// underlaying player.
    /// </summary>
    public void SeekForward()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.SeekForward();
    }

    /// <summary>
    /// Called from the skin if the user invokes the "InstantSkip" action. This will start the InstantSkip in the
    /// underlaying player.
    /// </summary>
    /// <param name="percent">Percent to skip (+/- supported).</param>
    public void InstantSkip(int percent)
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.InstantSkip(percent);
    }


    /// <summary>
    /// Called from the skin if the user presses the "previous" button. This will play the previous media item in the
    /// playlist of the underlaying player.
    /// </summary>
    public void Previous()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.PreviousItem();
    }

    /// <summary>
    /// Called from the skin if the user presses the "next" button. This will play the next media item in the
    /// playlist of the underlaying player.
    /// </summary>
    public void Next()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.NextItem();
    }

    /// <summary>
    /// Called from the skin if the user presses the "toggle mute" button. This will mute or unmute the player manager,
    /// depending on the current muted state.
    /// </summary>
    public void ToggleMute()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Muted ^= true;
      UpdateProperties();
    }

    /// <summary>
    /// Called from the skin to make the underlaying player the current player. The current player is the player which
    /// receives all play commands from the remote.
    /// </summary>
    public void MakeCurrent()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerContext);
      if (pc == null)
        return;
      IPlayerSlotController psc = pc.PlayerSlotController;
      if (psc == null)
        return;
      playerContextManager.CurrentPlayerIndex = psc.SlotIndex;
    }

    /// <summary>
    /// Called from the skin to switch the primary video player and the secondary video player. This function only
    /// works if both players are active and both players are video players.
    /// </summary>
    public void SwitchPip()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.SwitchPipPlayers();
      // The workflow state will be changed to the new primary player's FSC- or CP-state automatically by the PCM,
      // if necessary
    }

    #endregion

    #region Base overrides

    public override void Allocate()
    {
      base.Allocate();
      CheckHeartBeat();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      CheckHeartBeat();
    }

    #endregion
  }
}
