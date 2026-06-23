using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeveloperStore.ORM.Mapping;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Id)
            .HasConversion(value => value.Value, value => SaleId.Create(value))
            .ValueGeneratedNever();
        builder.Property(sale => sale.SaleNumber)
            .HasConversion(value => value.Value, value => SaleNumber.Create(value))
            .HasColumnName("sale_number")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(sale => sale.SoldAt)
            .HasConversion(value => value.Value, value => SoldAt.Create(value))
            .HasColumnName("sold_at")
            .IsRequired();
        builder.OwnsOne(sale => sale.Customer, owned =>
        {
            owned.Property(reference => reference.Id).HasColumnName("customer_external_id").HasMaxLength(64).IsRequired();
            owned.Property(reference => reference.Description).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
        });
        builder.OwnsOne(sale => sale.Branch, owned =>
        {
            owned.Property(reference => reference.Id).HasColumnName("branch_external_id").HasMaxLength(64).IsRequired();
            owned.Property(reference => reference.Description).HasColumnName("branch_name").HasMaxLength(200).IsRequired();
        });
        builder.Property(sale => sale.TotalAmount)
            .HasConversion(value => value.Value, value => Money.FromDecimal(value))
            .HasColumnName("total_amount")
            .HasPrecision(18, 2);
        builder.Property(sale => sale.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(sale => sale.SaleNumber).IsUnique();

        builder.HasMany(sale => sale.Items)
            .WithOne()
            .HasForeignKey(item => item.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(sale => sale.Items).AutoInclude();
    }
}
