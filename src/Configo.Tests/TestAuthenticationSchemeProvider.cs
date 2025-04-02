// using Configo.Server.Domain;
// using Microsoft.AspNetCore.Authentication;
// using Microsoft.Extensions.Options;
//
// namespace Configo.Tests;
//
// public sealed class TestAuthenticationSchemeProvider : AuthenticationSchemeProvider
// {
//     public TestAuthenticationSchemeProvider(IOptions<AuthenticationOptions> options) : base(options) { }
//     public TestAuthenticationSchemeProvider(IOptions<AuthenticationOptions> options, IDictionary<string, AuthenticationScheme> schemes) : base(options, schemes) { }
//
//     public override async Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
//     {
//         var apiKeyScheme = await GetSchemeAsync(ApiKeyAuthenticationHandler.AuthenticationScheme);
//         if (apiKeyScheme is null)
//         {
//             throw new InvalidOperationException("API key authentication scheme is not configured");
//         }
//
//         var testAuthenticationScheme = await GetSchemeAsync(TestAuthenticationHandler.AuthenticationScheme);
//         if (testAuthenticationScheme is null)
//         {
//             throw new InvalidOperationException("Test authentication scheme is not configured");
//         }
//
//         return [apiKeyScheme, testAuthenticationScheme];
//     }
// }
