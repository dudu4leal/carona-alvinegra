using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class VanConfiguration : IEntityTypeConfiguration<Van>
{
    public void Configure(EntityTypeBuilder<Van> builder)
    {
        builder.ToTable("Vans");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Rota).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.Motorista).HasMaxLength(100).IsRequired();
        builder.Property(v => v.ResponsavelId).IsRequired();
    }
}
