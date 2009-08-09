using System.Collections.Generic;
using System.Globalization;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Callback interface to retrieve informational, localized, vendor-defined data about a UPnP device.
  /// </summary>
  /// <remarks>
  /// Some are required, recommended, optional.
  /// Some should be localized or may be localized.
  /// </remarks>
  public interface ILocalizedDeviceInformation
  {
    /// <summary>
    /// Gets the associated device's short description for the end user. Should be localized.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized string.</returns>
    string GetFriendlyName(CultureInfo culture);

    /// <summary>
    /// Gets the manufacturer of the associated device. May be localized.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized string.</returns>
    string GetManufacturer(CultureInfo culture);

    /// <summary>
    /// Gets the URL of the associated device's manufacturer. May be localized. This value is optional and may be <c>null</c>.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized URL string or <c>null</c>.</returns>
    string GetManufacturerURL(CultureInfo culture);

    /// <summary>
    /// Gets the associated device's long description for the end user. Should be localized.
    /// This value is recommended but may be <c>null</c>.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized string or <c>null</c>.</returns>
    string GetModelDescription(CultureInfo culture);

    /// <summary>
    /// Gets the model name of the associated device. May be localized.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized string.</returns>
    string GetModelName(CultureInfo culture);

    /// <summary>
    /// Gets the model number of the associated device. May be localized.
    /// This value is recommended but may be <c>null</c>.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized string or <c>null</c>.</returns>
    string GetModelNumber(CultureInfo culture);

    /// <summary>
    /// Gets the URL of the associated device's model. May be localized. This value is optional and may be <c>null</c>.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized URL string or <c>null</c>.</returns>
    string GetModelURL(CultureInfo culture);

    /// <summary>
    /// Gets the serial number of the associated device. May be localized.
    /// This value is recommended but may be <c>null</c>.
    /// </summary>
    /// <param name="culture">The culture to localize the returned data.</param>
    /// <returns>Localized string or <c>null</c>.</returns>
    string GetSerialNumber(CultureInfo culture);
    
    /// <summary>
    /// Universal Product Code. 12-digit, all-numeric code that identifies the consumer package.
    /// Managed by the Uniform Code Council. This value is optional and may be <c>null</c>.
    /// </summary>
    /// <returns>Universal product code string or <c>null</c>.</returns>
    string GetUPC();

    /// <summary>
    /// Returns the descriptors for 0-n icons for the device. The icon resources must be managed outside and available
    /// via the URL returned by 
    /// </summary>
    /// <param name="culture">The culture to localize the returned icons.</param>
    /// <returns>Collection of icon descriptors.</returns>
    ICollection<IconDescriptor> GetIcons(CultureInfo culture);
  }
}