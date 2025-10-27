using FluentAssertions;
using GestorInventario.Application.Categories.Commands;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestorInventario.Application.Tests.Categories;

public class CreateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateCategoryWithParent()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldCreateCategoryWithParent));

        var parent = new Category { Name = "Electrónica" };
        context.Categories.Add(parent);
        await context.SaveChangesAsync();

        var handler = new CreateCategoryCommandHandler(context);
        var command = new CreateCategoryCommand("Smartphones", "Dispositivos móviles", parent.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.ParentId.Should().Be(parent.Id);
        result.Children.Should().BeEmpty();

        var created = await context.Categories.Include(category => category.Parent).SingleAsync(category => category.Id == result.Id);
        created.ParentId.Should().Be(parent.Id);
        created.Name.Should().Be("Smartphones");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenParentDoesNotExist()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrowNotFound_WhenParentDoesNotExist));
        var handler = new CreateCategoryCommandHandler(context);
        var command = new CreateCategoryCommand("Audio", null, 999);

        // Act
        var action = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
