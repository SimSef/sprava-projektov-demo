using System.Xml.Serialization;

namespace SpravaProjektov.Data.Xml;

[XmlRoot("users")]
public class UsersXml
{
    [XmlElement("user")]
    public List<UserXml> Users { get; set; } = [];
}

public class UserXml
{
    [XmlElement("username")] public string Username { get; set; } = string.Empty;
    [XmlElement("password")] public string Password { get; set; } = string.Empty;
    [XmlElement("displayName")] public string? DisplayName { get; set; }

    [XmlArray("roles"), XmlArrayItem("role")]
    public List<string> Roles { get; set; } = [];
}
