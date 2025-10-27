using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.ProductAttributes.Commands;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.ProductAttributes;

public class ProductAttributeValueCommandTests
{
    [Fact]
    public async Task CreateValue_ShouldAssignSequentialDisplayOrder()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(CreateValue_ShouldAssignSequentialDisplayOrder));

        var group = new ProductAttributeGroup
        {
            Name = "Color",
            Slug = "color",
            TenantId = 1,
            AllowCustomValues = false
        };

        context.ProductAttributeGroups.Add(group);
        context.ProductAttributeValues.Add(new ProductAttributeValue
        {
            Group = group,
            Name = "Azul",
            DisplayOrder = 0,
            IsActive = true,
            TenantId = 1
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateProductAttributeValueCommandHandler(context);

        var result = await handler.Handle(
            new CreateProductAttributeValueCommand(group.Id, "Rojo", null, "#ff0000", null, true),
            CancellationToken.None);

        result.DisplayOrder.Should().Be(1);
        result.HexColor.Should().Be("#FF0000");
    }

    [Fact]
    public async Task UpdateValue_ShouldPersistChanges()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(UpdateValue_ShouldPersistChanges));

        var group = new ProductAttributeGroup
        {
            Name = "Talla",
            Slug = "talla",
            TenantId = 1,
            AllowCustomValues = false
        };

        var value = new ProductAttributeValue
        {
            Group = group,
            Name = "M",
            DisplayOrder = 0,
            IsActive = true,
            TenantId = 1
        };

        context.ProductAttributeGroups.Add(group);
        context.ProductAttributeValues.Add(value);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateProductAttributeValueCommandHandler(context);

        var updated = await handler.Handle(
            new UpdateProductAttributeValueCommand(group.Id, value.Id, "L", "Talla grande", "00ff00", 2, false),
            CancellationToken.None);

        updated.Name.Should().Be("L");
        updated.Description.Should().Be("Talla grande");
        updated.HexColor.Should().Be("#00FF00");
        updated.DisplayOrder.Should().Be(2);
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateValue_ShouldInsertAtSpecifiedDisplayOrder()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(CreateValue_ShouldInsertAtSpecifiedDisplayOrder));

        var group = new ProductAttributeGroup
        {
            Name = "Material",
            Slug = "material",
            TenantId = 1,
            AllowCustomValues = false
        };

        var first = new ProductAttributeValue
        {
            Group = group,
            Name = "Algodón",
            DisplayOrder = 0,
            IsActive = true,
            TenantId = 1
        };

        var second = new ProductAttributeValue
        {
            Group = group,
            Name = "Lana",
            DisplayOrder = 1,
            IsActive = true,
            TenantId = 1
        };

        context.ProductAttributeGroups.Add(group);
        context.ProductAttributeValues.AddRange(first, second);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateProductAttributeValueCommandHandler(context);

        var inserted = await handler.Handle(
            new CreateProductAttributeValueCommand(group.Id, "Poliéster", null, null, 1, true),
            CancellationToken.None);

        inserted.DisplayOrder.Should().Be(1);

        var orderedValues = context.ProductAttributeValues
            .Where(value => value.GroupId == group.Id)
            .OrderBy(value => value.DisplayOrder)
            .ToList();

        orderedValues.Select(value => value.Name).Should().ContainInOrder("Algodón", "Poliéster", "Lana");
        orderedValues.Select(value => value.DisplayOrder).Should().ContainInOrder(0, 1, 2);
    }
}
