using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeveloperStore.ORM.Mapping;

public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items", table =>
        {
            table.HasCheckConstraint("ck_sale_items_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_sale_items_unit_price_non_neg", "unit_price >= 0");
            table.HasCheckConstraint("ck_sale_items_discount_range", "discount_percentage >= 0 AND discount_percentage <= 1");
        });

        builder.HasKey(item => item.Id).HasName("pk_sale_items");
        builder.Property(item => item.Id)
            .HasColumnName("id")
            .HasConversion(value => value.Value, value => SaleItemId.Create(value))
            .ValueGeneratedNever();
        builder.Property(item => item.SaleId)
            .HasConversion(value => value.Value, value => SaleId.Create(value))
            .HasColumnName("sale_id")
            .IsRequired();
        builder.OwnsOne(item => item.Product, owned =>
        {
            owned.Property(reference => reference.Id).HasColumnName("product_external_id").HasMaxLength(64).IsRequired();
            owned.Property(reference => reference.Description).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        });
        builder.Property(item => item.Quantity)
            .HasConversion(value => value.Value, value => ItemQuantity.Create(value))
            .HasColumnName("quantity")
            .IsRequired();
        builder.Property(item => item.UnitPrice)
            .HasConversion(value => value.Value, value => Money.Create(value, "item unit price", false))
            .HasColumnName("unit_price")
            .HasPrecision(18, 2);
        builder.Property(item => item.DiscountPercentage)
            .HasConversion(value => value.Value, value => MapDiscountRate(value))
            .HasColumnName("discount_percentage")
            .HasPrecision(5, 2);
        builder.Property(item => item.DiscountAmount)
            .HasConversion(value => value.Value, value => Money.FromDecimal(value))
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2);
        builder.Property(item => item.TotalAmount)
            .HasConversion(value => value.Value, value => Money.FromDecimal(value))
            .HasColumnName("total_amount")
            .HasPrecision(18, 2);
        builder.Property(item => item.IsCancelled).HasColumnName("is_cancelled").IsRequired();

        builder.HasIndex(item => item.SaleId).HasDatabaseName("idx_sale_items_sale_id");
    }

    private static DiscountRate MapDiscountRate(decimal value)
    {
        return value switch
        {
            0.20m => DiscountRate.TwentyPercent,
            0.10m => DiscountRate.TenPercent,
            _ => DiscountRate.Zero
        };
    }
}
