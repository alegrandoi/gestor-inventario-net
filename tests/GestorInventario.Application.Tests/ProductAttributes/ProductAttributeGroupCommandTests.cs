using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.ProductAttributes.Commands;
using GestorInventario.Application.ProductAttributes.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.ProductAttributes;

public class ProductAttributeGroupCommandTests
{
    [Fact]
    public async Task CreateGroup_ShouldGenerateUniqueSlug_WhenNameRepeats()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(CreateGroup_ShouldGenerateUniqueSlug_WhenNameRepeats));

        context.ProductAttributeGroups.Add(new ProductAttributeGroup
        {
            Name = "Talla",
            Slug = "talla",
            AllowCustomValues = false,
            TenantId = 1
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateProductAttributeGroupCommandHandler(context);

        var result = await handler.Handle(new CreateProductAttributeGroupCommand("Talla", null, false), CancellationToken.None);

        result.Name.Should().Be("Talla");
        result.Slug.Should().StartWith("talla-");
        result.AllowCustomValues.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateGroup_ShouldUpdateFields_AndRegenerateSlug()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(UpdateGroup_ShouldUpdateFields_AndRegenerateSlug));

        var group = new ProductAttributeGroup
        {
            Name = "Color",
            Slug = "color",
            AllowCustomValues = true,
            TenantId = 1
        };
        context.ProductAttributeGroups.Add(group);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateProductAttributeGroupCommandHandler(context);

        var updated = await handler.Handle(
            new UpdateProductAttributeGroupCommand(group.Id, "Colores", "Variantes cromáticas", false),
            CancellationToken.None);

        updated.Name.Should().Be("Colores");
        updated.Slug.Should().StartWith("colores");
        updated.Description.Should().Be("Variantes cromáticas");
        updated.AllowCustomValues.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteGroup_ShouldRemoveEntity()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(DeleteGroup_ShouldRemoveEntity));

        var group = new ProductAttributeGroup
        {
            Name = "Material",
            Slug = "material",
            AllowCustomValues = false,
            TenantId = 1
        };
        context.ProductAttributeGroups.Add(group);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteProductAttributeGroupCommandHandler(context);
        await handler.Handle(new DeleteProductAttributeGroupCommand(group.Id), CancellationToken.None);

        context.ProductAttributeGroups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroups_ShouldReturnValuesSorted()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(GetGroups_ShouldReturnValuesSorted));

        var group = new ProductAttributeGroup
        {
            Name = "Talla",
            Slug = "talla",
            AllowCustomValues = false,
            TenantId = 1,
            Values =
            {
                new ProductAttributeValue { Name = "M", DisplayOrder = 1, IsActive = true, TenantId = 1 },
                new ProductAttributeValue { Name = "S", DisplayOrder = 0, IsActive = true, TenantId = 1 }
            }
        };
        context.ProductAttributeGroups.Add(group);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProductAttributeGroupsQueryHandler(context);
        var result = await handler.Handle(new GetProductAttributeGroupsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Values.Select(value => value.Name).Should().ContainInOrder("S", "M");
    }
}
