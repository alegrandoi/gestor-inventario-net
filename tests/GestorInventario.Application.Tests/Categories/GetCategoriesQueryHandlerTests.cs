using FluentAssertions;
using GestorInventario.Application.Categories.Models;
using GestorInventario.Application.Categories.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.Categories;

public class GetCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnHierarchyOrderedByName()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldReturnHierarchyOrderedByName));

        var electronics = new Category { Name = "Electrónica" };
        var home = new Category { Name = "Hogar" };
        var phones = new Category { Name = "Smartphones", Parent = electronics };
        var laptops = new Category { Name = "Ordenadores", Parent = electronics };
        var kitchen = new Category { Name = "Cocina", Parent = home };

        context.AddRange(electronics, home, phones, laptops, kitchen);
        await context.SaveChangesAsync();

        var handler = new GetCategoriesQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(category => category.Name).Should().ContainInOrder("Electrónica", "Hogar");

        var electronicsNode = result.First(category => category.Name == "Electrónica");
        electronicsNode.Children.Should().HaveCount(2);
        electronicsNode.Children.Select(child => child.Name).Should().ContainInOrder("Ordenadores", "Smartphones");

        var homeNode = result.First(category => category.Name == "Hogar");
        homeNode.Children.Should().ContainSingle(child => child.Name == "Cocina");
    }

    [Fact]
    public async Task Handle_ById_ShouldReturnCategoryWithNestedChildren()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ById_ShouldReturnCategoryWithNestedChildren));

        var root = new Category { Name = "Electrónica" };
        var phones = new Category { Name = "Smartphones", Parent = root };
        var accessories = new Category { Name = "Accesorios", Parent = phones };

        context.AddRange(root, phones, accessories);
        await context.SaveChangesAsync();

        var handler = new GetCategoryByIdQueryHandler(context);

        // Act
        CategoryDto result = await handler.Handle(new GetCategoryByIdQuery(root.Id), CancellationToken.None);

        // Assert
        result.Name.Should().Be("Electrónica");
        result.Children.Should().HaveCount(1);
        var phonesNode = result.Children.Should().ContainSingle().Subject;
        phonesNode.Name.Should().Be("Smartphones");
        phonesNode.Children.Should().ContainSingle().Subject.Name.Should().Be("Accesorios");
    }
}
