using System.Text.Json;
using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class ListaEsperaConfiguration : IEntityTypeConfiguration<ListaEspera>
{
    public void Configure(EntityTypeBuilder<ListaEspera> builder)
    {
        builder.ToTable("ListasEspera");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.JogoId).IsRequired();
        builder.Property(l => l.Rota).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Ignore(l => l.ParticipanteIds);

        builder.Property<List<Guid>>("_participanteIds")
            .HasColumnName("ParticipanteIds")
            .HasColumnType("text")
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
                ids => ids.ToList()));

        builder.HasOne<Jogo>()
            .WithMany()
            .HasForeignKey(l => l.JogoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
