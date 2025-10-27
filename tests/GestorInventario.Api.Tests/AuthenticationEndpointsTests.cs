using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GestorInventario.Application.Authentication.Commands;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Constants;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Xunit;

namespace GestorInventario.Api.Tests;

public class AuthenticationEndpointsTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory factory;

    public AuthenticationEndpointsTests(TestingWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = "admin",
            password = "Admin123$"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        payload.Should().NotBeNull();
        payload!.RequiresTwoFactor.Should().BeFalse();
        payload.Token.Should().NotBeNullOrWhiteSpace();
        payload.User.Should().NotBeNull();
        payload.User!.Username.Should().Be("admin");
    }

    [Fact]
    public async Task Login_ShouldReturnMfaChallenge_WhenMfaEnabled()
    {
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";
        const string password = "Strong123$";
        string totpSecret;

        using (var scope = factory.Services.CreateScope())
        {
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            await identityService.RegisterAsync(new RegisterUserCommand(username, email, password, RoleNames.Planner), CancellationToken.None);

            var users = await identityService.GetUsersAsync(CancellationToken.None);
            var registeredUser = users.Single(user => user.Username == username);

            var setup = await identityService.GenerateTotpSetupAsync(registeredUser.Id, CancellationToken.None);
            setup.Succeeded.Should().BeTrue();
            setup.Value.Should().NotBeNull();

            totpSecret = setup.Value!.Secret;
            var totp = new Totp(Base32Encoding.ToBytes(totpSecret));
            var activationCode = totp.ComputeTotp();

            var activation = await identityService.ActivateTotpAsync(registeredUser.Id, activationCode, CancellationToken.None);
            activation.Succeeded.Should().BeTrue();
        }

        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = username,
            password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        loginPayload.Should().NotBeNull();
        loginPayload!.RequiresTwoFactor.Should().BeTrue();
        loginPayload.TwoFactorSessionId.Should().NotBeNullOrWhiteSpace();

        var verificationTotp = new Totp(Base32Encoding.ToBytes(totpSecret));
        var verifyResponse = await client.PostAsJsonAsync("/api/auth/login/mfa", new
        {
            usernameOrEmail = username,
            sessionId = loginPayload.TwoFactorSessionId,
            verificationCode = verificationTotp.ComputeTotp()
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        verifyPayload.Should().NotBeNull();
        verifyPayload!.RequiresTwoFactor.Should().BeFalse();
        verifyPayload.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PasswordReset_ShouldAllowNewLogin_WhenTokenIsValid()
    {
        var username = $"reset_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";
        const string password = "Reset123$";
        const string newPassword = "Reset456$";

        using (var scope = factory.Services.CreateScope())
        {
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            await identityService.RegisterAsync(new RegisterUserCommand(username, email, password, RoleNames.InventoryManager), CancellationToken.None);
        }

        var client = factory.CreateClient();
        var initiateResponse = await client.PostAsJsonAsync("/api/auth/password/forgot", new
        {
            usernameOrEmail = username
        });

        initiateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initiationPayload = await initiateResponse.Content.ReadFromJsonAsync<PasswordResetRequestDto>();
        initiationPayload.Should().NotBeNull();
        initiationPayload!.Token.Should().NotBeNullOrWhiteSpace();

        var resetResponse = await client.PostAsJsonAsync("/api/auth/password/reset", new
        {
            usernameOrEmail = username,
            token = initiationPayload.Token,
            newPassword
        });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = username,
            password = newPassword
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        loginPayload.Should().NotBeNull();
        loginPayload!.RequiresTwoFactor.Should().BeFalse();
        loginPayload.Token.Should().NotBeNullOrWhiteSpace();

        // double-check password changed by ensuring old password fails
        var previousPasswordResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = username,
            password
        });

        previousPasswordResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
