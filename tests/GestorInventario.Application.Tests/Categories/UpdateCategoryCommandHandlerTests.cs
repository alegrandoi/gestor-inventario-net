using FluentAssertions;
using GestorInventario.Application.Categories.Commands;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Tests.Categories;

public class UpdateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateCategoryAndParent()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldUpdateCategoryAndParent));

        var root = new Category { Name = "Electrónica" };
        var originalParent = new Category { Name = "Audio", Parent = root };
        var category = new Category { Name = "Auriculares", Parent = originalParent };
        var newParent = new Category { Name = "Accesorios" };

        context.AddRange(root, originalParent, category, newParent);
        await context.SaveChangesAsync();

        var handler = new UpdateCategoryCommandHandler(context);
        var command = new UpdateCategoryCommand(category.Id, "Auriculares", "Audio personal", newParent.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(category.Id);
        result.ParentId.Should().Be(newParent.Id);

        var updated = await context.Categories.Include(item => item.Parent).SingleAsync(item => item.Id == category.Id);
        updated.ParentId.Should().Be(newParent.Id);
        updated.Description.Should().Be("Audio personal");
    }

    [Fact]
    public async Task Handle_ShouldThrowValidation_WhenAssigningDescendantAsParent()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrowValidation_WhenAssigningDescendantAsParent));

        var root = new Category { Name = "Electrónica" };
        var child = new Category { Name = "Smartphones", Parent = root };
        var grandChild = new Category { Name = "Accesorios", Parent = child };

        context.AddRange(root, child, grandChild);
        await context.SaveChangesAsync();

        var handler = new UpdateCategoryCommandHandler(context);
        var command = new UpdateCategoryCommand(root.Id, "Electrónica", null, grandChild.Id);

        // Act
        var action = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ApplicationValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenParentDoesNotExist()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrowNotFound_WhenParentDoesNotExist));

        var category = new Category { Name = "Electrónica" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var handler = new UpdateCategoryCommandHandler(context);
        var command = new UpdateCategoryCommand(category.Id, "Electrónica", null, 999);

        // Act
        var action = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
