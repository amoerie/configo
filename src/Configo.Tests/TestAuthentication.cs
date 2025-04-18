﻿using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Configo.Tests;

public sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    public Func<Task<AuthenticateResult>>? Authenticate { get; set; }
    public Func<ClaimsPrincipal, AuthenticationProperties?, Task>? SignIn { get; set; }
    public Func<AuthenticationProperties?, Task>? SignOut { get; set; }
}

public sealed class TestAuthenticationHandler : SignInAuthenticationHandler<TestAuthenticationOptions>
{
    public const string AuthenticationScheme = "TestAuthentication";

    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) { }
    
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authenticate = Options.Authenticate;
        if (authenticate == null)
        {
            throw new InvalidOperationException(
                $"Test authentication is not configured properly. Please configure {nameof(TestAuthenticationOptions)}.{
                    nameof(TestAuthenticationOptions.Authenticate)}"
            );
        }
        return authenticate();
    }

    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var signIn = Options.SignIn;
        if (signIn == null)
        {
            throw new InvalidOperationException(
                $"Test authentication is not configured properly. Please configure {nameof(TestAuthenticationOptions)}.{
                    nameof(TestAuthenticationOptions.SignIn)}"
            );
        }
        return signIn(user, properties);
    }

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        var signOut = Options.SignOut;
        if (signOut == null)
        {
            throw new InvalidOperationException(
                $"Test authentication is not configured properly. Please configure {nameof(TestAuthenticationOptions)}.{
                    nameof(TestAuthenticationOptions.SignOut)}"
            );
        }
        return signOut(properties);
    }
}

public sealed class RemoveUnsupportedAuthenticationSchemesHostedService(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    ILogger<RemoveUnsupportedAuthenticationSchemesHostedService> logger)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        string[] schemesToRemove = [CookieAuthenticationDefaults.AuthenticationScheme, MicrosoftAccountDefaults.AuthenticationScheme];
        foreach (var scheme in schemesToRemove)
        {
            logger.LogInformation("Removing authentication scheme {Scheme}", scheme);
            authenticationSchemeProvider.RemoveScheme(scheme);
        }

        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
