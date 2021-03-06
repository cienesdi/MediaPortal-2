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

namespace MediaPortal.Core.MediaManagement.ResourceAccess
{
  /// <summary>
  /// Interface to provide access to physical media files.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This interface is the root interface for all media providers. Media providers are separated into
  /// <see cref="IBaseMediaProvider"/>s and <see cref="IChainedMediaProvider"/>s. See their interface docs for more
  /// information.
  /// </para>
  /// <para>
  /// The media provider is partitioned in its metadata part (see <see cref="Metadata"/>) and this worker class.
  /// </para>
  /// </remarks>
  // TODO: Rename to IResourceProvider, rename all related classes
  public interface IMediaProvider
  {
    /// <summary>
    /// Metadata descriptor for this media provider.
    /// </summary>
    MediaProviderMetadata Metadata { get; }
  }
}
