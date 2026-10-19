using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("Pagamentos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ParticipanteId).IsRequired();
        builder.Property(p => p.JogoId).IsRequired();
        builder.Property(p => p.Pago).IsRequired();

        builder.HasOne<Participante>()
            .WithMany()
            .HasForeignKey(p => p.ParticipanteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Jogo>()
            .WithMany()
            .HasForeignKey(p => p.JogoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ParticipanteId, p.JogoId }).IsUnique();
    }
}
