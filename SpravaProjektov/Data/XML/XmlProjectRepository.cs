using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Options;
using SpravaProjektov.Application.Config;
using SpravaProjektov.Application.Projects;

namespace SpravaProjektov.Data.Xml;

public sealed class XmlProjectRepository(IOptions<AppConfig> options, ILogger<XmlProjectRepository> logger) : IProjectRepository
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly IOptions<AppConfig> _options = options;
    private readonly ILogger<XmlProjectRepository> _logger = logger;

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Fetching all projects");
            var proj = LoadProjects();
            var list = proj.Projects
                .Select(p => new Project(p.Id, p.Name, p.Abbreviation, p.Customer))
                .ToList();
            _logger.LogDebug("Loaded {Count} projects", list.Count);
            return list;
        }
        finally
        {
            Gate.Release();
        }
    }

    

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project.Id);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Adding project {Id}", project.Id);
            var proj = LoadProjects();
            if (proj.Projects.Any(x => string.Equals(x.Id, project.Id, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Project {Id} already exists", project.Id);
                throw new InvalidOperationException($"Project with id '{project.Id}' already exists.");
            }

            proj.Projects.Add(new ProjectXml
            {
                Id = project.Id,
                Name = project.Name ?? string.Empty,
                Abbreviation = project.Abbreviation ?? string.Empty,
                Customer = project.Customer ?? string.Empty
            });
            SaveProjects(proj);
            _logger.LogInformation("Added project {Id}", project.Id);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project.Id);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Updating project {Id}", project.Id);
            var proj = LoadProjects();
            var e = proj.Projects.FirstOrDefault(x => string.Equals(x.Id, project.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new KeyNotFoundException($"Project '{project.Id}' not found.");
            e.Name = project.Name ?? string.Empty;
            e.Abbreviation = project.Abbreviation ?? string.Empty;
            e.Customer = project.Customer ?? string.Empty;
            SaveProjects(proj);
            _logger.LogInformation("Updated project {Id}", project.Id);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Deleting project {Id}", id);
            var proj = LoadProjects();
            var idx = proj.Projects.FindIndex(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                _logger.LogDebug("Project {Id} not found for delete", id);
                return;
            }
            proj.Projects.RemoveAt(idx);
            SaveProjects(proj);
            _logger.LogInformation("Deleted project {Id}", id);
        }
        finally
        {
            Gate.Release();
        }
    }

    private ProjectsXml LoadProjects()
    {
        var fullPath = ResolvePath(_options.Value.Storage.ProjectsPath);
        _logger.LogDebug("Loading projects XML from {Path}", fullPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Projects XML not found at '{fullPath}'.");
        }

        var ser = new XmlSerializer(typeof(ProjectsXml));
        using var fs = File.OpenRead(fullPath);
        var data = ser.Deserialize(fs) as ProjectsXml
            ?? throw new InvalidOperationException("Failed to deserialize projects XML.");
        _logger.LogDebug("Deserialized {Count} projects from {Path}", data.Projects?.Count ?? 0, fullPath);
        return data;
    }

    private void SaveProjects(ProjectsXml projects)
    {
        var fullPath = ResolvePath(_options.Value.Storage.ProjectsPath);
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        var tempPath = Path.Combine(dir, $".{Path.GetFileName(fullPath)}.tmp");
        _logger.LogDebug("Saving projects XML to temp file {Temp} then replacing {Path}", tempPath, fullPath);
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.GetEncoding(1250),
            Indent = true,
            NewLineOnAttributes = false,
            OmitXmlDeclaration = false
        };

        var ser = new XmlSerializer(typeof(ProjectsXml));
        using (var fs = File.Create(tempPath))
        using (var xw = XmlWriter.Create(fs, settings))
        {
            xw.WriteStartDocument();
            ser.Serialize(xw, projects);
        }

        File.Move(tempPath, fullPath, overwrite: true);
        _logger.LogDebug("Saved projects XML to {Path}", fullPath);
    }

    private static string ResolvePath(string relOrAbs)
        => Path.IsPathRooted(relOrAbs) ? relOrAbs : Path.Combine(AppContext.BaseDirectory, relOrAbs);
}
