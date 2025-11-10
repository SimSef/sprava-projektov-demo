namespace SpravaProjektov.Application.Projects;

public sealed record Project(
    string Id,
    string Name,
    string Abbreviation,
    string Customer
);

