using System.Text.Json;
using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class AlocacaoConfiguration : IEntityTypeConfiguration<Alocacao>
{
    public void Configure(EntityTypeBuilder<Alocacao> builder)
    {
        builder.ToTable("Alocacoes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.JogoId).IsRequired();
        builder.Property(a => a.Rota).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Ignore(a => a.GruposEscalados);

        builder.Property<List<Guid>>("_gruposEscalados")
            .HasColumnName("GruposEscalados")
            .HasColumnType("text")
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
                ids => ids.ToList()));

        builder.HasMany(a => a.Vans)
            .WithOne()
            .HasForeignKey("AlocacaoId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Vans)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(a => a.Spin)
            .WithOne()
            .HasForeignKey<SpinAlocado>("AlocacaoId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Jogo>()
            .WithMany()
            .HasForeignKey(a => a.JogoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
