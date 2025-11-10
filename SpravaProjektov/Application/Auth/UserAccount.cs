namespace SpravaProjektov.Application.Auth;

public sealed record UserAccount(
    string Username,
    string? DisplayName,
    string[] Roles
);

