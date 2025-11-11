using System.Xml.Serialization;

namespace SpravaProjektov.Data.Xml;

[XmlRoot("projects")]
public class ProjectsXml
{
    [XmlElement("project")]
    public List<ProjectXml> Projects { get; set; } = [];
}

public class ProjectXml
{
    [XmlAttribute("id")] public string Id { get; set; } = string.Empty;
    [XmlElement("name")] public string Name { get; set; } = string.Empty;
    [XmlElement("abbreviation")] public string Abbreviation { get; set; } = string.Empty;
    [XmlElement("customer")] public string Customer { get; set; } = string.Empty;
}
