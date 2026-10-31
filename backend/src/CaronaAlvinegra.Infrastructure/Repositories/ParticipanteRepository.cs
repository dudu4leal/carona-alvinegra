using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaronaAlvinegra.Infrastructure.Repositories;

public class ParticipanteRepository : IParticipanteRepository
{
    private readonly ApplicationDbContext _context;

    public ParticipanteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Participante?> GetByIdAsync(Guid id) =>
        _context.Participantes.FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<Participante>> GetAllAsync(Guid? grupoId = null)
    {
        var query = _context.Participantes.AsQueryable();

        if (grupoId.HasValue)
            query = query.Where(p => p.GrupoId == grupoId.Value);

        return query.ToListAsync();
    }

    public async Task AddAsync(Participante participante)
    {
        await _context.Participantes.AddAsync(participante);
    }

    public void Update(Participante participante) => _context.Participantes.Update(participante);

    public void Remove(Participante participante) => _context.Participantes.Remove(participante);

    public Task<int> CountByGrupoAsync(Guid grupoId) =>
        _context.Participantes.CountAsync(p => p.GrupoId == grupoId);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
