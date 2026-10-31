using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaronaAlvinegra.Infrastructure.Repositories;

public class AlocacaoRepository : IAlocacaoRepository
{
    private readonly ApplicationDbContext _context;

    public AlocacaoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Alocacao alocacao)
    {
        await _context.Alocacoes.AddAsync(alocacao);
    }

    public Task<List<Alocacao>> GetByJogoAsync(Guid jogoId) =>
        _context.Alocacoes
            .Include(a => a.Vans)
            .Include(a => a.Spin)
            .Where(a => a.JogoId == jogoId)
            .ToListAsync();

    public async Task RemoveByJogoAsync(Guid jogoId)
    {
        var alocacoes = await _context.Alocacoes
            .Where(a => a.JogoId == jogoId)
            .ToListAsync();

        _context.Alocacoes.RemoveRange(alocacoes);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
