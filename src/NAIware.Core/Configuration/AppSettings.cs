using System.Xml;
using NAIware.Core.Security.Cryptography;

namespace NAIware.Core.Configuration;

/// <summary>
/// Provides runtime read/write access to application configuration files (.config XML).
/// </summary>
/// <remarks>
/// Changes are persisted directly to the XML config file. In-memory app settings
/// require a restart to reflect changes made through the standard ConfigurationManager.
/// </remarks>
public class AppSettings
{
    private readonly SymmetricServices? _cryptoservice;
    private readonly string _encryptionkey = string.Empty;

    private static bool _integratedAuthentication = true;
    private static int _appId = -1;

    /// <summary>Creates a new instance with no encryption.</summary>
    public AppSettings() : this(null, string.Empty) { }

    /// <summary>Creates a new instance with encryption enabled using the specified key.</summary>
    public AppSettings(string encryptionKey) : this(new SymmetricServices(), encryptionKey) { }

    /// <summary>Creates a new instance with the specified crypto service and key.</summary>
    public AppSettings(SymmetricServices? cryptoService, string encryptionKey)
    {
        _cryptoservice = cryptoService;
        _encryptionkey = encryptionKey;
    }

    /// <summary>
    /// Gets or sets a configuration setting, optionally encrypted.
    /// </summary>
    public string this[string setting]
    {
        get
        {
            if (_cryptoservice is null) return GetValue(setting);
            return _cryptoservice.Decrypt(GetValue(_cryptoservice.Encrypt(setting, _encryptionkey)), _encryptionkey);
        }
        set
        {
            if (_cryptoservice is null)
                SetValue(setting, value);
            else
                SetValue(_cryptoservice.Encrypt(setting, _encryptionkey), _cryptoservice.Encrypt(value, _encryptionkey));
        }
    }

    /// <summary>Gets or sets whether integrated Windows authentication is used.</summary>
    public static bool IntegratedAuthentication
    {
        get => _integratedAuthentication;
        set => _integratedAuthentication = value;
    }

    /// <summary>Gets the application ID from configuration.</summary>
    public static int AppId
    {
        get
        {
            if (_appId == -1)
            {
                _appId = int.Parse(
                    System.Configuration.ConfigurationManager.AppSettings["AppId"]
                    ?? throw new InvalidOperationException("AppId setting not found in configuration."));
            }
            return _appId;
        }
    }

    /// <summary>
    /// Gets a configuration setting value from the application config file.
    /// </summary>
    /// <param name="setting">The setting key to retrieve.</param>
    /// <returns>The setting value.</returns>
    public static string GetValue(string setting)
    {
        var asm = System.Reflection.Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("Unable to determine entry assembly.");

        var fi = new FileInfo(asm.Location + ".config");
        var doc = new XmlDocument();
        try
        {
            doc.Load(fi.FullName);
            foreach (XmlNode node in doc["configuration"]!["appSettings"]!)
            {
                if (node.Name == "add" &&
                    node.Attributes?.GetNamedItem("key")?.Value == setting)
                {
                    return node.Attributes.GetNamedItem("value")!.Value!;
                }
            }

            throw new KeyNotFoundException($"Setting '{setting}' not found.");
        }
        catch (KeyNotFoundException) { throw; }
        catch
        {
            throw new InvalidOperationException(
                "Unable to find the setting. Check that the configuration file exists and contains the appSettings element.");
        }
    }

    /// <summary>
    /// Sets a configuration setting value in the application config file.
    /// </summary>
    /// <param name="setting">The setting key to change or add.</param>
    /// <param name="val">The value to set.</param>
    public static void SetValue(string setting, string val)
    {
        bool changed = false;
        var asm = System.Reflection.Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("Unable to determine entry assembly.");

        var fi = new FileInfo(asm.Location + ".config");
        var doc = new XmlDocument();
        try
        {
            doc.Load(fi.FullName);
            foreach (XmlNode node in doc["configuration"]!["appSettings"]!)
            {
                if (node.Name == "add" &&
                    node.Attributes?.GetNamedItem("key")?.Value == setting)
                {
                    node.Attributes.GetNamedItem("value")!.Value = val;
                    changed = true;
                }
            }

            if (!changed)
            {
                var appSettingsNode = doc["configuration"]!["appSettings"]!;
                var elem = doc.CreateElement("add");
                var attrKey = doc.CreateAttribute("key");
                var attrVal = doc.CreateAttribute("value");
                elem.Attributes.SetNamedItem(attrKey).Value = setting;
                elem.Attributes.SetNamedItem(attrVal).Value = val;
                appSettingsNode.AppendChild(elem);
            }

            doc.Save(fi.FullName);
        }
        catch
        {
            throw new InvalidOperationException(
                "Unable to set the value. Check that the configuration file exists and contains the appSettings element.");
        }
    }

    /// <summary>
    /// Removes a configuration setting from the application config file.
    /// </summary>
    /// <param name="setting">The setting key to remove.</param>
    public static void RemoveSetting(string setting)
    {
        bool removed = false;
        var asm = System.Reflection.Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("Unable to determine entry assembly.");

        var fi = new FileInfo(asm.Location + ".config");
        var doc = new XmlDocument();
        try
        {
            doc.Load(fi.FullName);
            foreach (XmlNode node in doc["configuration"]!["appSettings"]!)
            {
                if (node.Name == "add" &&
                    node.Attributes?.GetNamedItem("key")?.Value == setting)
                {
                    node.ParentNode!.RemoveChild(node);
                    removed = true;
                    break;
                }
            }

            if (!removed) throw new KeyNotFoundException($"Setting '{setting}' not found.");
            doc.Save(fi.FullName);
        }
        catch (KeyNotFoundException) { throw; }
        catch
        {
            throw new InvalidOperationException(
                "Unable to remove the value. Check that the configuration file exists and contains the appSettings element.");
        }
    }
}
