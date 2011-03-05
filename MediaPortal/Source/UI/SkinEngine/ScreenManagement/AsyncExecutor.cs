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

using System.Collections.Generic;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public delegate void WorkDlgt();

  public class AsyncExecutor
  {
    protected static object _syncObj = new object();
    protected static AsyncExecutor _instance = null;

    protected volatile bool _terminated = false;
    protected Thread _workerThread;
    protected Queue<WorkDlgt> _workQueue = new Queue<WorkDlgt>(10);

    private AsyncExecutor()
    {
      _workerThread = new Thread(DoGarbageCollection)
        {
          Name = "SEAW",  // SkinEngineAsyncWorker
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };
      _workerThread.Start();
    }

    public void Terminate()
    {
      _terminated = true;
      lock (_syncObj)
        Monitor.PulseAll(_syncObj);
    }

    public void Join()
    {
      _workerThread.Join();
    }

    protected void Enqueue(WorkDlgt dlgt)
    {
      _workQueue.Enqueue(dlgt);
    }

    protected void DoGarbageCollection()
    {
      while (!_terminated)
      {
        while (ProcessEntry() && !_terminated) {}
        lock (_syncObj)
        {
          if (_terminated)
            break;
          Monitor.Wait(_syncObj);
        }
      }
    }

    protected bool ProcessEntry()
    {
      WorkDlgt dlgt;
      lock (_syncObj)
        dlgt = _workQueue.Count == 0 ? null : _workQueue.Dequeue();
      if (dlgt == null)
        return false;
      dlgt();
      return true;
    }

    public void DoFinishWork()
    {
      while (ProcessEntry()) {}
    }

    public static void ScheduleWork(WorkDlgt dlgt)
    {
      lock (_syncObj)
      {
        if (_instance == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("AsyncExecutor: Async execution thread terminated. Executing action synchronously.");
          dlgt();
        }
        else
          _instance.Enqueue(dlgt);
        Monitor.PulseAll(_syncObj);
      }
    }

    public static void Start()
    {
      lock (_syncObj)
        if (_instance == null)
          _instance = new AsyncExecutor();
    }

    public static void Shutdown(bool finishWork)
    {
      AsyncExecutor ae;
      lock (_syncObj)
        ae = _instance;
      if (ae != null)
      {
        ae.Terminate();
        ae.Join();
        if (finishWork)
          ae.DoFinishWork();
      }
    }

    public static void FinishWork()
    {
      AsyncExecutor ae;
      lock (_syncObj)
        ae = _instance;
      if (ae != null)
        ae.DoFinishWork();
    }
  }
}