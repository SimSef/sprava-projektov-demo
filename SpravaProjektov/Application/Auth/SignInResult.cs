namespace SpravaProjektov.Application.Auth;

public sealed record SignInResult(bool Succeeded, string? Error)
{
    public static SignInResult Success() => new(true, null);
    public static SignInResult Fail(string? error) => new(false, error);
}

