using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class SpinConfiguration : IEntityTypeConfiguration<Spin>
{
    public void Configure(EntityTypeBuilder<Spin> builder)
    {
        builder.ToTable("Spins");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Rota).HasConversion<string>().HasMaxLength(20).IsRequired();
    }
}
