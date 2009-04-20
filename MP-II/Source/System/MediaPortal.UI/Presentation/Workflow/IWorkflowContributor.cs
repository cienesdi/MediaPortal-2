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

namespace MediaPortal.Presentation.Workflow
{
  public delegate void ContributorStateChangeDelegate();

  /// <summary>
  /// Interface which needs to be implemented by workflow action contributor models.
  /// </summary>
  public interface IWorkflowContributor
  {
    /// <summary>
    /// Will be raised when the <see cref="IsActionEnabled"/> or <see cref="IsActionVisible"/> states have
    /// changed.
    /// </summary>
    event ContributorStateChangeDelegate StateChanged;

    /// <summary>
    /// Returns the information if the associated workflow action should be visible.
    /// </summary>
    bool IsActionVisible { get; }

    /// <summary>
    /// Returns the information if the associated workflow action should be enabled.
    /// </summary>
    bool IsActionEnabled { get; }

    /// <summary>
    /// Initializes this workflow action contributor. The implementor can execute any initialization code here.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Executes the contributor's code when the action is triggered.
    /// </summary>
    void Execute();
  }
}