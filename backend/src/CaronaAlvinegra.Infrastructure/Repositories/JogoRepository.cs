using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaronaAlvinegra.Infrastructure.Repositories;

public class JogoRepository : IJogoRepository
{
    private readonly ApplicationDbContext _context;

    public JogoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Jogo?> GetByIdAsync(Guid id) =>
        _context.Jogos.FirstOrDefaultAsync(j => j.Id == id);

    public Task<List<Jogo>> GetAllAsync() =>
        _context.Jogos.OrderBy(j => j.Data).ToListAsync();

    public async Task AddAsync(Jogo jogo)
    {
        await _context.Jogos.AddAsync(jogo);
    }

    public void Update(Jogo jogo) => _context.Jogos.Update(jogo);

    public void Remove(Jogo jogo) => _context.Jogos.Remove(jogo);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
