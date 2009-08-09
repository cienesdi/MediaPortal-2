using System;
using System.Collections.Generic;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.CP.SSDP
{
  /// <summary>
  /// Contains SSDP advertisement data for a collection of device and service advertisements which are located in the same root
  /// device.
  /// The entries are lazily initialized, as the SSDP protocol doesn't provide a strict order of advertisement messages of
  /// the single entries.
  /// </summary>
  public class RootEntry
  {
    protected string _descriptionLocation;
    protected EndpointConfiguration _endpoint;
    protected UPnPVersion _upnpVersion;
    protected HTTPVersion _httpVersion;
    protected string _osVersion;
    protected string _productVersion;
    protected DateTime _expirationTime;
    protected IDictionary<string, DeviceEntry> _devices = new Dictionary<string, DeviceEntry>(); // Device UIDs to DeviceEntry structures
    protected string _rootDeviceID = null; // UID of the root device
    protected int _searchPort = Consts.DEFAULT_SSDP_SEARCH_PORT;
    protected uint _bootID = 0;
    protected uint _configID = 0;
    protected IDictionary<string, object> _clientProperties = new Dictionary<string, object>();

    /// <summary>
    /// Creates a new <see cref="RootEntry"/> instance.
    /// </summary>
    /// <param name="descriptionLocation">Description URL of the root device.</param>
    /// <param name="config">UPnP endpoint where the advertisement was received.</param>
    /// <param name="upnpVersion">UPnP version the remote device is using.</param>
    /// <param name="httpVersion">HTTP version our partner is using.</param>
    /// <param name="osVersion">OS and version our partner is using.</param>
    /// <param name="productVersion">Product and version our partner is using.</param>
    /// <param name="expirationTime">Time when the advertisement will expire.</param>
    public RootEntry(string descriptionLocation, EndpointConfiguration config, UPnPVersion upnpVersion, HTTPVersion httpVersion,
        string osVersion, string productVersion, DateTime expirationTime)
    {
      _descriptionLocation = descriptionLocation;
      _endpoint = config;
      _upnpVersion = upnpVersion;
      _httpVersion = httpVersion;
      _osVersion = osVersion;
      _productVersion = productVersion;
      _expirationTime = expirationTime;
    }

    /// <summary>
    /// Returns the URL of the description for this device advertisement.
    /// </summary>
    public string DescriptionLocation
    {
      get { return _descriptionLocation; }
    }

    /// <summary>
    /// Returns the network endpoint where this root device's advertisement is tracked.
    /// </summary>
    public EndpointConfiguration Endpoint
    {
      get { return _endpoint; }
    }

    /// <summary>
    /// Returns the UPnP version the device's server uses for communication.
    /// </summary>
    public UPnPVersion UPnPVersion
    {
      get { return _upnpVersion; }
    }

    /// <summary>
    /// Returns the HTTP version the device's server uses for communication.
    /// </summary>
    public HTTPVersion HTTPVersion
    {
      get { return _httpVersion; }
    }

    /// <summary>
    /// Returns the OS and version of the device's server.
    /// </summary>
    public string OS_Version
    {
      get { return _osVersion; }
    }

    /// <summary>
    /// Returns the product and version of the device.
    /// </summary>
    public string Product_Version
    {
      get { return _productVersion; }
    }

    /// <summary>
    /// Returns the search port the remote SSDP server uses for unicast messaging.
    /// </summary>
    public int SearchPort
    {
      get { return _searchPort; }
      internal set { _searchPort = value; }
    }

    /// <summary>
    /// Gets or sets the boot ID of the UPnP server.
    /// </summary>
    public uint BootID
    {
      get { return _bootID; }
      internal set { _bootID = value; }
    }

    /// <summary>
    /// Gets or sets the configuration ID of the UPnP server. If the configuration ID changes, all description document caches must
    /// be flushed and rebuilt, if necessary.
    /// </summary>
    public uint ConfigID
    {
      get { return _configID; }
      internal set { _configID = value; }
    }

    /// <summary>
    /// Gets or sets the expiration time for the last advertisement of any of this root entrie's devices or services.
    /// </summary>
    public DateTime ExpirationTime
    {
      get { return _expirationTime; }
      internal set { _expirationTime = value; }
    }

    /// <summary>
    /// Gets a mapping of device UUIDs to <see cref="DeviceEntry"/> instances describing the contained devices.
    /// </summary>
    public IDictionary<string, DeviceEntry> Devices
    {
      get { return _devices; }
    }

    /// <summary>
    /// Gets or sets the root device's UUID.
    /// </summary>
    public string RootDeviceID
    {
      get { return _rootDeviceID; }
      internal set { _rootDeviceID = value; }
    }

    /// <summary>
    /// Returns a dictionary where clients can add additional properties to this root entry.
    /// </summary>
    public IDictionary<string, object> ClientProperties
    {
      get { return _clientProperties; }
    }

    /// <summary>
    /// Gets the existing device entry contained in this root entry for the specified <paramref name="uuid"/> or creates it.
    /// </summary>
    /// <param name="uuid">UUID of the device, whose entry should be returned.</param>
    /// <returns><see cref="DeviceEntry"/> instance with the given <see cref="uuid"/>.</returns>
    internal DeviceEntry GetOrCreateDeviceEntry(string uuid)
    {
      DeviceEntry result;
      if (_devices.TryGetValue(uuid, out result))
        return result;
      return _devices[uuid] = new DeviceEntry(uuid);
    }
  }
}