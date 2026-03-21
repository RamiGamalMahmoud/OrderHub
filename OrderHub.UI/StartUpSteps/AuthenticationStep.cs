using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps;

public class AuthenticationStep : IStartupStep
{
    private readonly IAuthService _auth;
    private readonly ICredentialsService _credentials;
    private readonly ITokenStorageService _tokenStorage;
    private readonly ISessionManager _session;

    public AuthenticationStep(
        IAuthService auth,
        ICredentialsService credentials,
        ITokenStorageService tokenStorage,
        ISessionManager session)
    {
        _auth = auth;
        _credentials = credentials;
        _tokenStorage = tokenStorage;
        _session = session;
    }

    public int Order => 4;
    public string DisplayName => "جاري التحقق من المصادقة";

    public async Task ExecuteAsync()
    {
#if DEBUG
        var creds = await _credentials.GetClilentCredentialsAsync();
        if (creds == null) return;

        var token = await _tokenStorage.GetTokenAsync();
        if (token == null)
        {
            var result = await _auth.AuthorizeAsync(creds);
            if (result.IsSuccess)
                await _tokenStorage.SaveTokenAsync(result.Value);
        }

        await _session.StartNewSession();
#endif
    }
}
