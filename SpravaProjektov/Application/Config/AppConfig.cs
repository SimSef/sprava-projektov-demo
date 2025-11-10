namespace SpravaProjektov.Application.Config;

public sealed class AppConfig
{
    public StorageConfig Storage { get; set; } = null!;
}

public sealed class StorageConfig
{
    public string ProjectsPath { get; set; } = null!;
    public string UsersPath { get; set; } = null!;
}
