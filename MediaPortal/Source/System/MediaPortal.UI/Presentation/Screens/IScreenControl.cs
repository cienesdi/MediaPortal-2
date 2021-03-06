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

namespace MediaPortal.UI.Presentation.Screens
{
  public enum ScreenMode
  {
    NormalWindowed,
    FullScreen
  };

  public interface IScreenControl
  {
    bool IsScreenSaverActive { get; }
    bool IsScreenSaverEnabled { get; set; }

    /// <summary>
    /// Shuts MediaPortal down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Minimizes the MediaPortal application.
    /// </summary>
    void Minimize();

    /// <summary>
    /// Restores the window from minimized state.
    /// </summary>
    void Restore();

    /// <summary>
    /// Suspends the current Windows session to disc.
    /// </summary>
    void Hibernate();

    /// <summary>
    /// Switches between windowed and fullscreen mode.
    /// </summary>
    /// <param name="mode">The requested mode.</param>
    void SwitchMode(ScreenMode mode);

    /// <summary>
    /// Returns the information whether the application is in fullscreen mode or in windowed mode.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is fullscreen mode; otherwise, <c>false</c>.
    /// </value>
    bool IsFullScreen { get; }

    /// <summary>
    /// Returns available display modes.
    /// </summary>
    IList<string> DisplayModes { get; }

    /// <summary>
    /// Returns the window handle of the main window.
    /// </summary>
    IntPtr MainWindowHandle { get; }
  }
}
