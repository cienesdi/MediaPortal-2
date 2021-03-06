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
using MediaPortal.Core.Messaging;

namespace MediaPortal.Core.Services.Messaging
{
  public class MessageBroker : IMessageBroker
  {
    protected const int GC_INTERVAL = 5000;

    protected IDictionary<string, IList<WeakReference>> _registeredChannels =
        new Dictionary<string, IList<WeakReference>>();
    protected object _syncObj = new object();
    protected Thread _garbageCollectorThread;

    public MessageBroker()
    {
      _garbageCollectorThread = new Thread(DoBackgroundWork)
        {
          Name = "MsgBrGC", //garbage collector thread
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };
      _garbageCollectorThread.Start();
    }

    ~MessageBroker()
    {
      if (_garbageCollectorThread != null)
      {
        if (_garbageCollectorThread.ThreadState != ThreadState.Stopped)
        {
          _garbageCollectorThread.Abort();
        }
        _garbageCollectorThread = null;
      }
    }

    protected void GarbageCollectHandlers()
    {
      lock (_syncObj)
      {
        foreach (string channel in new List<string>(_registeredChannels.Keys))
        {
          IList<WeakReference> receivers = _registeredChannels[channel];
          bool needCleanup = false;
          foreach (WeakReference receiver in receivers)
            if (receiver.Target == null)
            {
              needCleanup = true;
              break;
            }
          if (needCleanup)
          {
            IList<WeakReference> oldReceivers = receivers;
            _registeredChannels[channel] = receivers = new List<WeakReference>(oldReceivers.Count);
            foreach (WeakReference r in oldReceivers)
            {
              IMessageReceiver receiver = (IMessageReceiver) r.Target;
              if (receiver == null)
                continue;
              receivers.Add(new WeakReference(receiver));
            }
            if (receivers.Count == 0)
              _registeredChannels.Remove(channel);
          }
        }
      }
    }

    protected void DoBackgroundWork()
    {
      while (true)
      {
        GarbageCollectHandlers();
        Thread.Sleep(GC_INTERVAL);
      }
    }

    public void RegisterMessageReceiver(string channel, IMessageReceiver receiver)
    {
      lock (_syncObj)
      {
        IList<WeakReference> receivers;
        if (!_registeredChannels.TryGetValue(channel, out receivers))
          _registeredChannels[channel] = receivers = new List<WeakReference>();
        receivers.Add(new WeakReference(receiver));
      }
    }

    public void UnregisterMessageReceiver(string channel, IMessageReceiver receiver)
    {
      lock (_syncObj)
      {
        IList<WeakReference> receivers;
        if (_registeredChannels.TryGetValue(channel, out receivers))
        {
          foreach (WeakReference r in receivers)
            if (r.Target == receiver)
            {
              receivers.Remove(r);
              break;
            }
          if (receivers.Count == 0)
            _registeredChannels.Remove(channel);
        }
      }
    }

    public void Send(string channelName, SystemMessage msg)
    {
      msg.ChannelName = channelName;
      IList<WeakReference> receivers;
      lock (_syncObj)
      {
        if (!_registeredChannels.TryGetValue(channelName, out receivers))
          return;
        receivers = new List<WeakReference>(receivers);
      }
      foreach (WeakReference r in receivers)
      {
        IMessageReceiver receiver = (IMessageReceiver) r.Target;
        if (receiver != null)
          receiver.Receive(msg);
      }
    }
  }
}
