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

using MediaPortal.Core.Services.PluginManager.Builders;

namespace MediaPortal.UI.Services.Players.Builders
{
  public class PlayerBuilder : InstanceBuilder
  {
    // At the moment, we simply use an InstanceBuilder here. In the future, we could provide the option to
    // give a set of supported file extensions in the player builder item registration, which can be evaluated before
    // requesting the player builder -> lazy load the player builders on request
  }
}