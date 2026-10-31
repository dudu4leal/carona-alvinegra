using System.Text.Json;
using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class VanAlocadaConfiguration : IEntityTypeConfiguration<VanAlocada>
{
    public void Configure(EntityTypeBuilder<VanAlocada> builder)
    {
        builder.ToTable("VansAlocadas");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Motorista).HasMaxLength(100).IsRequired();
        builder.Property(v => v.ResponsavelId).IsRequired();
        builder.Property(v => v.Ocupacao).IsRequired();

        builder.Ignore(v => v.GrupoIds);

        builder.Property<List<Guid>>("_grupoIds")
            .HasColumnName("GrupoIds")
            .HasColumnType("text")
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
                ids => ids.ToList()));
    }
}
