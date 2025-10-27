using FluentAssertions;
using GestorInventario.Application.Authentication.Commands;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Models;
using Moq;
using Xunit;

namespace GestorInventario.Application.Tests.Authentication;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAuthResponse_WhenIdentityServiceSucceeds()
    {
        // Arrange
        var command = new RegisterUserCommand("planner", "planner@example.com", "Secure123$", "Planificador");
        var expectedResponse = new AuthResponseDto(
            "token",
            DateTime.UtcNow.AddHours(1),
            new UserSummaryDto(1, "planner", "planner@example.com", "Planificador", true),
            false,
            null,
            null);

        var identityServiceMock = new Mock<IIdentityService>();
        identityServiceMock
            .Setup(service => service.RegisterAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Success(expectedResponse));

        var handler = new RegisterUserCommandHandler(identityServiceMock.Object);

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().Be(expectedResponse);
        identityServiceMock.Verify(service => service.RegisterAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenIdentityServiceFails()
    {
        // Arrange
        var command = new RegisterUserCommand("planner", "planner@example.com", "Secure123$", "Planificador");

        var identityServiceMock = new Mock<IIdentityService>();
        identityServiceMock
            .Setup(service => service.RegisterAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Failure("error"));

        var handler = new RegisterUserCommandHandler(identityServiceMock.Object);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
