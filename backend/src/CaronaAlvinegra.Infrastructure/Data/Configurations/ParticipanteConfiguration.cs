using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class ParticipanteConfiguration : IEntityTypeConfiguration<Participante>
{
    public void Configure(EntityTypeBuilder<Participante> builder)
    {
        builder.ToTable("Participantes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(100).IsRequired();
        builder.Property(p => p.WhatsApp).HasMaxLength(20).IsRequired();
        builder.Property(p => p.GrupoId).IsRequired();
    }
}
