using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class JogoConfiguration : IEntityTypeConfiguration<Jogo>
{
    public void Configure(EntityTypeBuilder<Jogo> builder)
    {
        builder.ToTable("Jogos");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.CompeticaoCodigo).HasMaxLength(20).IsRequired();
        builder.Property(j => j.Adversario).HasMaxLength(100).IsRequired();
        builder.Property(j => j.Data).IsRequired();
        builder.Property(j => j.HorarioSaida).IsRequired();
        builder.Property(j => j.PontoEmbarque).HasMaxLength(200).IsRequired();
    }
}
