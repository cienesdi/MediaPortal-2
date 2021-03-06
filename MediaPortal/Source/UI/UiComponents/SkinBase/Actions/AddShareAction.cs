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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action to add a share. Depending on the evaluation if our home server runs at the same machine as the GUI,
  /// we either push the "choose media provider" or the "choose system" workflow state.
  /// In case the home server runs at the same machine, we don't provide the local shares configuration because
  /// in each case, the local server should host our shares.
  /// If the local server is not online, we don't provide any add shares action.
  /// </summary>
  public class AddShareAction : IWorkflowContributor
  {
    #region Consts

    public const string ADD_SHARE_CONTRIBUTOR_MODEL_ID_STR = "9E456C79-3FF1-4040-8CD7-4247C4C12817";

    public const string SHARE_ADD_CHOOSE_SYSTEM_STATE_ID_STR = "6F7EB06A-2AC6-4bcb-9003-F5DA44E03C26";
    public const string SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR = "F3163500-3015-4a6f-91F6-A3DA5DC3593C";

    public static readonly Guid ADD_SHARE_CONTRIBUTOR_MODEL_ID = new Guid(ADD_SHARE_CONTRIBUTOR_MODEL_ID_STR);

    public static readonly Guid SHARE_ADD_CHOOSE_SYSTEM_STATE_ID = new Guid(SHARE_ADD_CHOOSE_SYSTEM_STATE_ID_STR);
    public static readonly Guid SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID = new Guid(SHARE_EDIT_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR);

    public const string CANNOT_ADD_SHARES_TITLE_RES = "[SharesConfig.CannotAddSharesTitle]";
    public const string CANNOT_ADD_SHARE_LOCAL_HOME_SERVER_NOT_CONNECTED_RES = "[SharesConfig.CannotAddShareLocalHomeServerNotConnected]";

    #endregion

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return null; }
    }

    public void Initialize()
    {
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return true;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      // We could listen for the home server's attachment and connection state and change this return value according to those states.
      // But I think that makes too much work for a function which will only be used very rarely.
      return true;
    }

    public void Execute()
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
      bool localHomeServer = homeServerSystem != null && homeServerSystem.IsLocalSystem();
      bool homeServerConnected = serverConnectionManager.IsHomeServerConnected;
      if (localHomeServer && !homeServerConnected)
      {
        // This situation is an error condition: Our home server is local, i.e. all shares of this system must be configured
        // at the server, but the server is not online at the moment.
        IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
        dialogManager.ShowDialog(CANNOT_ADD_SHARES_TITLE_RES, CANNOT_ADD_SHARE_LOCAL_HOME_SERVER_NOT_CONNECTED_RES, DialogType.OkDialog, false,
            DialogButtonType.Ok);
        return;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(SHARE_ADD_CHOOSE_SYSTEM_STATE_ID);
    }

    #endregion
  }
}
