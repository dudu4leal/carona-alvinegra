using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Interfaces;

public interface IParticipanteRepository
{
    Task<Participante?> GetByIdAsync(Guid id);
    Task<List<Participante>> GetAllAsync(Guid? grupoId = null);
    Task AddAsync(Participante participante);
    void Update(Participante participante);
    void Remove(Participante participante);
    Task<int> CountByGrupoAsync(Guid grupoId);
    Task SaveChangesAsync();
}
