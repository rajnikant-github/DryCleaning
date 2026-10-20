using System.Net;
using System.Net.Http.Json;
using DryCleaning.Admin.Models;

namespace DryCleaning.Admin.Services;

/// <summary>Signs an admin in against the API (runs from the login form post, before there is a session).</summary>
public sealed class AdminLoginService(IHttpClientFactory factory)
{
    /// <summary>Returns the tokens, or a message to show on the login page.</summary>
    public async Task<(AuthResponse? Result, string? Error)> LoginAsync(string identifier, string password)
    {
        var http = factory.CreateClient(AdminApiClient.HttpClientName);
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("api/auth/login", new LoginRequest { Identifier = identifier.Trim(), Password = password }, AdminApiClient.Json);
        }
        catch (HttpRequestException)
        {
            return (null, "Can't reach the API. Is it running?");
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
                return (null, "Incorrect email/mobile number or password.");
            if (!response.IsSuccessStatusCode)
                return (null, await AdminApiClient.ReadProblemAsync(response));

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(AdminApiClient.Json);
            if (auth is null)
                return (null, "The server sent an empty answer.");
            if (auth.User.Role != UserRole.Admin)
                return (null, "This portal is for administrators only.");
            return (auth, null);
        }
    }
}
