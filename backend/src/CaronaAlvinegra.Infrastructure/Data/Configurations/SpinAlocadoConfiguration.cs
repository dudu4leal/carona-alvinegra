using System.Text.Json;
using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class SpinAlocadoConfiguration : IEntityTypeConfiguration<SpinAlocado>
{
    public void Configure(EntityTypeBuilder<SpinAlocado> builder)
    {
        builder.ToTable("SpinsAlocados");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Ocupacao).IsRequired();

        builder.Ignore(s => s.GrupoIds);

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
