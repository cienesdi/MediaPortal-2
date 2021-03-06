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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dokan;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.PathManager;

namespace MediaPortal.Core.Services.MediaManagement
{
  // TODO: Make the driver create a real readonly filesystem. Currently, the explorer still provides a
  // "New" entry. And DokanNet.ERROR_ACCESS_DENIED isn't really what we should return, but what else? DokanNet doesn't provide
  // us so many options...
  public class ResourceMountingService : IResourceMountingService, DokanOperations
  {
    #region Consts

    /// <summary>
    /// Volume label for the virtual drive if our mount point is a drive letter.
    /// </summary>
    public static string VOLUME_LABEL = "MediaPortal 2 resource access";

    protected const FileAttributes FILE_ATTRIBUTES = FileAttributes.ReadOnly;
    protected const FileAttributes DIRECTORY_ATTRIBUTES = FileAttributes.ReadOnly | FileAttributes.NotContentIndexed | FileAttributes.Directory;

    protected static readonly DateTime MIN_FILE_DATE = DateTime.FromFileTime(0); // If we use lower values, resources are not recognized by the Explorer/Windows API

    #endregion

    #region Inner classes

    /// <summary>
    /// Handle for a virtual resource.
    /// </summary>
    /// <remarks>
    /// Multithreading safety is ensured by locking on the mounting service's synchronization object.
    /// </remarks>
    protected abstract class VirtualFileSystemResource
    {
      protected string _name;

      protected IResourceAccessor _resourceAccessor;
      protected ICollection<FileHandle> _fileHandles = new HashSet<FileHandle>();
      protected DateTime _creationTime;

      protected VirtualFileSystemResource(string name, IResourceAccessor resourceAccessor)
      {
        _name = name;
        _resourceAccessor = resourceAccessor;
        _creationTime = DateTime.Now;
      }

      public virtual void Dispose()
      {
        foreach (FileHandle handle in _fileHandles)
          handle.Cleanup();
        if (_resourceAccessor != null)
          try
          {
            _resourceAccessor.Dispose();
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("ResourceMountingServer: Error disposing resource accessor '{0}'", e, _resourceAccessor);
          }
        _resourceAccessor = null;
      }

      public IResourceAccessor ResourceAccessor
      {
        get { return _resourceAccessor; }
      }

      public DateTime CreationTime
      {
        get { return _creationTime; }
      }

      public void AddFileHandle(FileHandle handle)
      {
        _fileHandles.Add(handle);
      }

      public void RemoveFileHandle(FileHandle handle)
      {
        _fileHandles.Remove(handle);
      }
    }

    /// <summary>
    /// Handle for a virtual file.
    /// </summary>
    protected class VirtualFile : VirtualFileSystemResource
    {
      public VirtualFile(string name, IResourceAccessor resourceAccessor) :
        base(name, resourceAccessor) { }

      public override string ToString()
      {
        return string.Format("Virtual file '{0}'", _name);
      }
    }

    protected abstract class VirtualBaseDirectory : VirtualFileSystemResource
    {
      protected VirtualBaseDirectory(string name, IResourceAccessor resourceAccessor) : base(name, resourceAccessor) { }

      public abstract IDictionary<string, VirtualFileSystemResource> ChildResources { get; }
    }

    /// <summary>
    /// Handle for a virtual root directory.
    /// </summary>
    protected class VirtualRootDirectory : VirtualBaseDirectory
    {
      protected IDictionary<string, VirtualFileSystemResource> _children =
          new Dictionary<string, VirtualFileSystemResource>(StringComparer.InvariantCultureIgnoreCase);

      public VirtualRootDirectory(string name) : base(name, null) { }

      public override void Dispose()
      {
        foreach (VirtualFileSystemResource resource in _children.Values)
          resource.Dispose();
        base.Dispose();
      }

      public override IDictionary<string, VirtualFileSystemResource> ChildResources
      {
        get { return _children; }
      }

      public void AddResource(string name, VirtualFileSystemResource resource)
      {
        _children.Add(name, resource);
      }

      public void RemoveResource(string name)
      {
        _children.Remove(name);
      }

      public override string ToString()
      {
        return string.Format("Virtual root directory '{0}'", _name);
      }
    }

    /// <summary>
    /// Handle for a virtual directory.
    /// </summary>
    protected class VirtualDirectory : VirtualBaseDirectory
    {
      protected IDictionary<string, VirtualFileSystemResource> _children = null; // Lazily initialized

      // ReSharper disable SuggestBaseTypeForParameter
      public VirtualDirectory(string name, IFileSystemResourceAccessor resourceAccessor) : base(name, resourceAccessor) { }
      // ReSharper restore SuggestBaseTypeForParameter

      public override void Dispose()
      {
        if (_children != null)
          foreach (VirtualFileSystemResource resource in _children.Values)
            resource.Dispose();
        _children = null;
        base.Dispose();
      }

      public IFileSystemResourceAccessor Directory
      {
        get { return (IFileSystemResourceAccessor) _resourceAccessor; }
      }

      public override IDictionary<string, VirtualFileSystemResource> ChildResources
      {
        get
        {
          if (_children == null)
          {
            _children = new Dictionary<string, VirtualFileSystemResource>(StringComparer.InvariantCultureIgnoreCase);
            try
            {
              foreach (IFileSystemResourceAccessor childDirectoryAccessor in Directory.GetChildDirectories())
                _children[childDirectoryAccessor.ResourceName] = new VirtualDirectory(
                    childDirectoryAccessor.ResourceName, childDirectoryAccessor);
              foreach (IFileSystemResourceAccessor fileAccessor in Directory.GetFiles())
                _children[fileAccessor.ResourceName] = new VirtualFile(fileAccessor.ResourceName, fileAccessor);
            }
            catch (Exception e)
            {
              ServiceRegistration.Get<ILogger>().Warn("ResourceMountingServer: Error collecting child resources of directory '{0}'", e, _name);
            }
          }
          return _children;
        }
      }

      public override string ToString()
      {
        return string.Format("Virtual directory '{0}'", _name);
      }
    }

    /// <summary>
    /// File handle of a resource which is associated to a user file handle.
    /// </summary>
    protected class FileHandle
    {
      protected VirtualFileSystemResource _resource;
      protected Stream _stream = null;

      public FileHandle(VirtualFileSystemResource resource)
      {
        _resource = resource;
      }

      public VirtualFileSystemResource Resource
      {
        get { return _resource; }
      }

      public void Cleanup()
      {
        try
        {
          if (_stream != null)
            _stream.Dispose();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ResourceMountingServer: Error cleaning up resource '{0}'", e, _resource.ResourceAccessor);
        }
        _stream = null;
      }

      public Stream GetOrOpenStream()
      {
        if (_stream == null)
        {
          IResourceAccessor resourceAccessor = _resource.ResourceAccessor;
          try
          {
            _stream = resourceAccessor == null ? null : resourceAccessor.OpenRead();
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("ResourceMountingServer: Error creating stream for resource '{0}'", e, resourceAccessor);
          }
        }
        return _stream;
      }
    }

    #endregion

    protected object _syncObj = new object();
    protected bool _started = false;
    protected string _mountPoint = null;
    protected Thread _mountThread;
    protected VirtualRootDirectory _root = new VirtualRootDirectory("/");

    protected void Run()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      IPathManager pathManager = ServiceRegistration.Get<IPathManager>();

      try
      {
        _mountPoint = pathManager.GetPath("<REMOTERESOURCES>");
        if (!Directory.Exists(_mountPoint))
          Directory.CreateDirectory(_mountPoint);
      }
      catch (Exception exception)
      {
        logger.Warn("ResourceMountingService: Unable to access or create remote resource directory '{0}' in filesystem", exception);
        return;
      }

      if (DokanNet.DokanRemoveMountPoint(_mountPoint) == 1)
        logger.Info("ResourceMountingService: Successfully unmounted remote resource directory '{0}' from unclean shutdown", _mountPoint);

      DokanOptions opt = new DokanOptions
        {
          MountPoint = _mountPoint,
          VolumeLabel = VOLUME_LABEL,
        };
      int result = DokanNet.DokanMain(opt, this);
      if (result == DokanNet.DOKAN_SUCCESS)
        logger.Debug("ResourceMountingService: DokanMain returned successfully");
      else
        logger.Warn("ResourceMountingService: DokanMain returned with error code {0}", result);
    }

    protected VirtualFileSystemResource ParseFileName(string fileName)
    {
      if (fileName == "\\")
        return _root;
      if (!fileName.StartsWith("\\"))
        return null;
      string[] pathSegments = fileName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
      VirtualFileSystemResource resource = _root;
      lock (_syncObj)
        foreach (string pathSegment in pathSegments)
        {
          if (resource is VirtualBaseDirectory)
          {
            if (!((VirtualBaseDirectory) resource).ChildResources.TryGetValue(pathSegment, out resource))
              return null;
          }
          else
            return null;
        }
      return resource;
    }

    protected VirtualRootDirectory GetRootDirectory(string rootDirectoryName)
    {
      VirtualFileSystemResource directoryResource;
      if (!_root.ChildResources.TryGetValue(rootDirectoryName, out directoryResource))
        return null;
      return (VirtualRootDirectory) directoryResource;
    }

    #region IResourceMountingService implementation

    public string MountPoint
    {
      get
      {
        lock (_syncObj)
          return _mountPoint;
      }
    }

    public ICollection<string> RootDirectories
    {
      get
      {
        lock (_syncObj)
          return new List<string>(_root.ChildResources.Keys);
      }
    }

    public void Startup()
    {
      lock (_syncObj)
      {
        _mountThread = new Thread(Run) { Name = "ResMount" }; // Resource mounting service
        _mountThread.Start();
        _started = true;
      }
    }

    public void Shutdown()
    {
      string mountPoint;
      lock (_syncObj)
      {
        _root.Dispose();
        if (!_started)
          return;
        mountPoint = _mountPoint;
      }
      if (!String.IsNullOrEmpty(mountPoint))
        try
        {
          if (DokanNet.DokanRemoveMountPoint(mountPoint) == 0)
            ServiceRegistration.Get<ILogger>().Error("Dokan failed to unmount remote resource directory '{0}'", mountPoint);
          else
            ServiceRegistration.Get<ILogger>().Info("Successfully unmounted remote resource directory '{0}'", mountPoint);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Error removing Dokan mount point", e);
        }
    }

    public string CreateRootDirectory(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        if (String.IsNullOrEmpty(_mountPoint))
          return null;
        _root.AddResource(rootDirectoryName, new VirtualRootDirectory(rootDirectoryName));
        return Path.Combine(_mountPoint, rootDirectoryName);
      }
    }

    public void DisposeRootDirectory(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        VirtualRootDirectory rootDirectory = GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return;
        _root.RemoveResource(rootDirectoryName);
        rootDirectory.Dispose();
      }
    }

    public ICollection<IResourceAccessor> GetResources(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        VirtualRootDirectory rootDirectory = GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return null;
        return rootDirectory.ChildResources.Values.Select(resource => resource.ResourceAccessor).ToList();
      }
    }

    public string AddResource(string rootDirectoryName, IResourceAccessor resourceAccessor)
    {
      lock (_syncObj)
      {
        if (String.IsNullOrEmpty(_mountPoint))
          return null;
        VirtualRootDirectory rootDirectory = GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return null;
        string resourceName = resourceAccessor.ResourceName;
        IFileSystemResourceAccessor fsra = resourceAccessor as IFileSystemResourceAccessor;
        rootDirectory.AddResource(resourceName, fsra != null && fsra.IsDirectory ?
            (VirtualFileSystemResource) new VirtualDirectory(resourceName, fsra) :
            new VirtualFile(resourceName, resourceAccessor));
        return Path.Combine(_mountPoint, rootDirectoryName + "\\" + resourceName);
      }
    }

    public void RemoveResource(string rootDirectoryName, IResourceAccessor resourceAccessor)
    {
      lock (_syncObj)
      {
        VirtualRootDirectory rootDirectory = GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return;
        string resourceName = resourceAccessor.ResourceName;
        VirtualFileSystemResource toRemove;
        if (!rootDirectory.ChildResources.TryGetValue(resourceName, out toRemove))
          return;
        rootDirectory.ChildResources.Remove(resourceName);
        toRemove.Dispose();
      }
    }

    #endregion

    #region DokanOperations implementation

    public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        VirtualFileSystemResource resource = ParseFileName(filename);
        if (resource == null)
          return -DokanNet.ERROR_FILE_NOT_FOUND;
        FileHandle handle = new FileHandle(resource);
        info.Context = handle;
        resource.AddFileHandle(handle);
        if (resource is VirtualBaseDirectory)
          info.IsDirectory = true; // Necessary for the Dokan driver to set this, see docs
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int OpenDirectory(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        VirtualFileSystemResource resource = ParseFileName(filename);
        if (resource == null || !(resource is VirtualBaseDirectory))
          return -DokanNet.ERROR_PATH_NOT_FOUND;
        FileHandle handle = new FileHandle(resource);
        info.Context = handle;
        resource.AddFileHandle(handle);
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int CreateDirectory(string filename, DokanFileInfo info)
    {
      return DokanNet.ERROR_ACCESS_DENIED;
    }

    public int Cleanup(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        if (handle != null)
          handle.Cleanup();
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int CloseFile(string filename, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        if (handle != null)
          handle.Resource.RemoveFileHandle(handle);
        info.Context = null;
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
    {
      FileHandle handle = (FileHandle) info.Context;
      Stream stream;
      lock (_syncObj)
      {
        if (handle == null)
          return -DokanNet.ERROR_FILE_NOT_FOUND;
        try
        {
          stream = handle.GetOrOpenStream();
          if (stream == null)
            return -DokanNet.ERROR_FILE_NOT_FOUND;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ReesourceMountingService: Error creating file stream of resource '{0}'", e, handle.Resource);
          return DokanNet.DOKAN_ERROR;
        }
      }
      // Do the reading outside the lock - might block when not enough bytes are available
      try
      {
        stream.Seek(offset, SeekOrigin.Begin);
        readBytes = (uint) stream.Read(buffer, 0, buffer.Length);
        return DokanNet.DOKAN_SUCCESS;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ReesourceMountingService: Error reading from stream of resource '{0}'", e, handle.Resource);
        return DokanNet.DOKAN_ERROR;
      }
    }

    public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int FlushFileBuffers(string filename, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        if (handle == null)
          return -DokanNet.ERROR_FILE_NOT_FOUND;
        VirtualFileSystemResource resource = handle.Resource;
        VirtualFile file = resource as VirtualFile;
        IResourceAccessor resourceAccessor = resource.ResourceAccessor;
        VirtualBaseDirectory directory = resource as VirtualBaseDirectory;
        fileinfo.FileName = filename;
        fileinfo.CreationTime = resource.CreationTime;
        fileinfo.LastAccessTime = resourceAccessor == null ? resource.CreationTime : resourceAccessor.LastChanged;
        if (fileinfo.LastAccessTime < MIN_FILE_DATE)
          fileinfo.LastAccessTime = MIN_FILE_DATE;
        fileinfo.LastWriteTime = resource.CreationTime;
        if (fileinfo.LastWriteTime < MIN_FILE_DATE)
          fileinfo.LastWriteTime = MIN_FILE_DATE;
        if (file != null)
        {
          fileinfo.Attributes = FILE_ATTRIBUTES;
          fileinfo.Length = resourceAccessor == null ? 0 : resourceAccessor.Size;
          return DokanNet.DOKAN_SUCCESS;
        }
        if (directory != null)
        {
          fileinfo.Attributes = DIRECTORY_ATTRIBUTES;
          fileinfo.Length = 0;
          return DokanNet.DOKAN_SUCCESS;
        }
        return -DokanNet.ERROR_FILE_NOT_FOUND;
      }
    }

    public int FindFiles(string filename, ArrayList files, DokanFileInfo info)
    {
      lock (_syncObj)
      {
        FileHandle handle = (FileHandle) info.Context;
        VirtualBaseDirectory directory = handle == null ? null : handle.Resource as VirtualBaseDirectory;
        if (directory == null)
          return -DokanNet.ERROR_PATH_NOT_FOUND;
        foreach (KeyValuePair<string, VirtualFileSystemResource> entry in directory.ChildResources)
        {
          VirtualFileSystemResource resource = entry.Value;
          IResourceAccessor resourceAccessor = resource.ResourceAccessor;
          FileInformation fi = new FileInformation
            {
              Attributes = resource is VirtualFile ? FILE_ATTRIBUTES : DIRECTORY_ATTRIBUTES,
              CreationTime = resource.CreationTime,
              LastAccessTime = resourceAccessor == null ? resource.CreationTime : resourceAccessor.LastChanged,
              LastWriteTime = directory.CreationTime, // When using DateTime.MinValue, the resource is not recognized
              Length = resourceAccessor == null ? 0 : resourceAccessor.Size,
              FileName = entry.Key
            };
          files.Add(fi);
        }
        return DokanNet.DOKAN_SUCCESS;
      }
    }

    public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int DeleteFile(string filename, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int DeleteDirectory(string filename, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int SetEndOfFile(string filename, long length, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int SetAllocationSize(string filename, long length, DokanFileInfo info)
    {
      return -DokanNet.ERROR_ACCESS_DENIED;
    }

    public int LockFile(string filename, long offset, long length, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
    {
      return DokanNet.DOKAN_SUCCESS;
    }

    // Function is called on system shutdown
    public int Unmount(DokanFileInfo info)
    {
      CloseFile(string.Empty, info);
      return DokanNet.DOKAN_SUCCESS;
    }

    #endregion
  }
}
