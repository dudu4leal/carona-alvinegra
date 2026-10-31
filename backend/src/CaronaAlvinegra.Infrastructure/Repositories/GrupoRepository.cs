using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaronaAlvinegra.Infrastructure.Repositories;

public class GrupoRepository : IGrupoRepository
{
    private readonly ApplicationDbContext _context;

    public GrupoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Grupo?> GetByIdAsync(Guid id) =>
        _context.Grupos
            .Include(g => g.Participantes)
            .FirstOrDefaultAsync(g => g.Id == id);

    public Task<List<Grupo>> GetAllAsync() =>
        _context.Grupos
            .Include(g => g.Participantes)
            .ToListAsync();

    public async Task AddAsync(Grupo grupo)
    {
        await _context.Grupos.AddAsync(grupo);
    }

    public void Update(Grupo grupo) => _context.Grupos.Update(grupo);

    public void Remove(Grupo grupo) => _context.Grupos.Remove(grupo);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
