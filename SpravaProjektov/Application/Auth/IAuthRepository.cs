using System.Threading;
using System.Threading.Tasks;

namespace SpravaProjektov.Application.Auth;

public interface IAuthRepository
{
    Task<SignInResult> SignInAsync(string username, string password, bool persistent = false, CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
}
