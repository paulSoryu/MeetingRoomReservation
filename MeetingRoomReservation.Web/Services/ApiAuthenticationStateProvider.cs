using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MeetingRoomReservation.Web.Services;

public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private readonly AuthTokenProvider _tokenProvider;

    public ApiAuthenticationStateProvider(AuthTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
        _tokenProvider.Changed += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _tokenProvider.AccessToken;
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(new AuthenticationState(Anonymous));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt", nameType: ClaimTypes.Email, roleType: ClaimTypes.Role);

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
