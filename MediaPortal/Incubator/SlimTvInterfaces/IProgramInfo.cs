﻿#region Copyright (C) 2007-2011 Team MediaPortal

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

using System.Collections.Generic;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using System;

namespace MediaPortal.Plugins.SlimTvClient.Interfaces
{
  /// <summary>
  /// IProgramInfo defines all actions and properties for TV programs handling.
  /// </summary>
  public interface IProgramInfo
  {
    bool GetCurrentProgram(IChannel channel, out IProgram program);
    bool GetNextProgram(IChannel channel, out IProgram program);
    bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs);
    bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs);
    bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs);
    /// <summary>
    /// Gets a channel from an IProgram.
    /// </summary>
    /// <param name="program">Program.</param>
    /// <param name="channel">Channel.</param>
    /// <returns>True if succeeded.</returns>
    bool GetChannel(IProgram program, out IChannel channel);
  }
}
