using CaronaAlvinegra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CaronaAlvinegra.Infrastructure.Data.Configurations;

public class GrupoConfiguration : IEntityTypeConfiguration<Grupo>
{
    public void Configure(EntityTypeBuilder<Grupo> builder)
    {
        builder.ToTable("Grupos");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Nome).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Rota).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasMany(g => g.Participantes)
            .WithOne()
            .HasForeignKey(p => p.GrupoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(g => g.Participantes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
