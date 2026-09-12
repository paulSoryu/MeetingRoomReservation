using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MeetingRoomReservation.Web.Components;
using MeetingRoomReservation.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// [Authorize] on a @page component is endpoint metadata, so ASP.NET Core's authorization
// middleware enforces it on every full/direct HTTP navigation - not just inside the
// interactive circuit - and needs a real IAuthenticationService to redirect unauthenticated
// requests. This cookie scheme is registered only to provide that and its LoginPath; the app
// never actually signs into it. The real "is this user logged in" check for in-app navigation
// comes entirely from ApiAuthenticationStateProvider/AuthTokenProvider below.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/login");
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthTokenProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddScoped<BookingHubClient>();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl configuration is required.");

builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
