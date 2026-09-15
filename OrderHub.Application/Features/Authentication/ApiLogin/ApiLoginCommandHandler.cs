using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Services;
using OrderHub.Domain.Common;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Authentication.ApiLogin;
internal class ApiLoginCommandHandler : IRequestHandler<ApiLoginCommand, Result<AuthResponse>>
{
    private readonly IDatabaseConfigurationService _databaseConfigurationService;
    private readonly IApplicationContext _applicationContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserSessionStorage _userSessionStorage;
    private readonly IAppLogger _appLogger;

    public ApiLoginCommandHandler(
        IDatabaseConfigurationService databaseConfigurationService,
        IApplicationContext applicationContext,
        IHttpClientFactory httpClientFactory,
        IUserSessionStorage userSessionStorage,
        IAppLogger appLogger)
    {
        _databaseConfigurationService = databaseConfigurationService;
        _applicationContext = applicationContext;
        _httpClientFactory = httpClientFactory;
        _userSessionStorage = userSessionStorage;
        _appLogger = appLogger;
    }

    public async Task<Result<AuthResponse>> Handle(ApiLoginCommand request, CancellationToken cancellationToken)
    {
        // Login the user through api post request
        HttpClient httpClient = _httpClientFactory.CreateClient("OrderHubApi");

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "auth/login",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);

            return Result<AuthResponse>.Failure(errorMessage);
        }

        AuthResponse authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

        if (authResponse is null || authResponse.DatabaseInfo is null || authResponse.UserInfo is null)
            return Result<AuthResponse>.Failure("استجابة تسجيل الدخول غير صحيحة.");

        // Save database settings
        var databaseInfo = authResponse.DatabaseInfo;
        _databaseConfigurationService.SaveSettings(new Configuration.DatabaseSettings()
        {
            Host = databaseInfo.Host,
            Port = databaseInfo.Port,
            Database = databaseInfo.Database,
            Username = databaseInfo.UserName,
            Password = databaseInfo.Password
        });

        UserSession userSession = new UserSession(
            authResponse.UserInfo.Id,
            authResponse.UserInfo.UserName,
            authResponse.UserInfo.DisplayName,
            authResponse.UserInfo.IsAdmin,
            authResponse.UserInfo.UserPermissions.Select(permission => new UserPermissionSession(
                permission.Feature,
                permission.CanRead,
                permission.CanWrite))
            .ToList());

        _applicationContext.SetUser(userSession);

        // Remember the user session
        if (request.RememberMe)
            _userSessionStorage.Save(userSession);
        else
            _userSessionStorage.Clear();

        // Return success
        return Result<AuthResponse>.Success(authResponse);
    }
}
