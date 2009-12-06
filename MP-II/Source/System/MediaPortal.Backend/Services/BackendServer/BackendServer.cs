#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;
using System.Net;
using HttpServer;
using HttpServer.HttpModules;
using MediaPortal.Backend.BackendServer;
using MediaPortal.Backend.BackendServer.Settings;
using MediaPortal.Backend.Services.ClientCommunication;
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Core.SystemResolver;
using UPnP.Infrastructure;
using ILogger=MediaPortal.Core.Logging.ILogger;
using UPnPLogger = UPnP.Infrastructure.ILogger;

namespace MediaPortal.Backend.Services.BackendServer
{
  public class BackendServer : IBackendServer, IDisposable
  {
    public const string MP2SERVER_DEVICEVERSION = "MediaPortal-II/1.0";

    public class UPnPLoggerDelegate : UPnPLogger
    {
      public void Debug(string format, params object[] args)
      {
        ServiceScope.Get<ILogger>().Debug(format, args);
      }

      public void Debug(string format, Exception ex, params object[] args)
      {
        ServiceScope.Get<ILogger>().Debug(format, ex, args);
      }

      public void Info(string format, params object[] args)
      {
        ServiceScope.Get<ILogger>().Info(format, args);
      }

      public void Info(string format, Exception ex, params object[] args)
      {
        ServiceScope.Get<ILogger>().Info(format, ex, args);
      }

      public void Warn(string format, params object[] args)
      {
        ServiceScope.Get<ILogger>().Warn(format, args);
      }

      public void Warn(string format, Exception ex, params object[] args)
      {
        ServiceScope.Get<ILogger>().Warn(format, ex, args);
      }

      public void Error(string format, params object[] args)
      {
        ServiceScope.Get<ILogger>().Error(format, args);
      }

      public void Error(string format, Exception ex, params object[] args)
      {
        ServiceScope.Get<ILogger>().Error(format, ex, args);
      }

      public void Error(Exception ex)
      {
        ServiceScope.Get<ILogger>().Error(ex);
      }

      public void Critical(string format, params object[] args)
      {
        ServiceScope.Get<ILogger>().Critical(format, args);
      }

      public void Critical(string format, Exception ex, params object[] args)
      {
        ServiceScope.Get<ILogger>().Critical(format, ex, args);
      }

      public void Critical(Exception ex)
      {
        ServiceScope.Get<ILogger>().Critical(ex);
      }
    }

    protected readonly HttpServer.HttpServer _httpServerV4;
    protected readonly HttpServer.HttpServer _httpServerV6;
    protected readonly UPnPBackendServer _upnpServer;

    internal class HttpLogWriter : ILogWriter
    {
      public void Write(object source, LogPrio priority, string message)
      {
        string msg = source + ": " + message;
        ILogger logger = ServiceScope.Get<ILogger>();
        switch (priority)
        {
          case LogPrio.Trace:
            // Don't write trace messages (we don't support a trace level in MP - would have to map it to debug level)
            break;
          case LogPrio.Debug:
            logger.Debug(msg);
            break;
          case LogPrio.Info:
            logger.Info(msg);
            break;
          case LogPrio.Warning:
            logger.Warn(msg);
            break;
          case LogPrio.Error:
            logger.Error(msg);
            break;
          case LogPrio.Fatal:
            logger.Critical(msg);
            break;
        }
      }
    }

    public BackendServer()
    {
      _httpServerV4 = new HttpServer.HttpServer(new HttpLogWriter());
      _httpServerV6 = new HttpServer.HttpServer(new HttpLogWriter());
      Configuration.PRODUCT_VERSION = MP2SERVER_DEVICEVERSION;
      Configuration.LOGGER = new UPnPLoggerDelegate();

      ISystemResolver systemResolver = ServiceScope.Get<ISystemResolver>();
      _upnpServer = new UPnPBackendServer(systemResolver.LocalSystemId);
    }

    public void Dispose()
    {
      _upnpServer.Dispose();
    }

    #region IBackendServer implementation

    public void Startup()
    {
      BackendServerSettings settings = ServiceScope.Get<ISettingsManager>().Load<BackendServerSettings>();
      _httpServerV4.Start(IPAddress.Any, settings.HttpServerPort);
      _httpServerV6.Start(IPAddress.IPv6Any, settings.HttpServerPort);
      _upnpServer.Start();
    }

    public void Shutdown()
    {
      _httpServerV4.Stop();
      _httpServerV6.Stop();
      _upnpServer.Stop();
    }

    public void AddHttpModule(HttpModule module)
    {
      _httpServerV4.Add(module);
      _httpServerV6.Add(module);
    }

    public void RemoveHttpModule(HttpModule module)
    {
      _httpServerV4.Remove(module);
      _httpServerV6.Remove(module);
    }

    #endregion
  }
}