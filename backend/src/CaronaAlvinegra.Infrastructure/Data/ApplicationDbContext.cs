using CaronaAlvinegra.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CaronaAlvinegra.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Participante> Participantes => Set<Participante>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Van> Vans => Set<Van>();
    public DbSet<Spin> Spins => Set<Spin>();
    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<ListaEspera> ListasEspera => Set<ListaEspera>();
    public DbSet<Alocacao> Alocacoes => Set<Alocacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
