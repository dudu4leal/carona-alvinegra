using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Interfaces;

public interface IGrupoRepository
{
    Task<Grupo?> GetByIdAsync(Guid id);
    Task<List<Grupo>> GetAllAsync();
    Task AddAsync(Grupo grupo);
    void Update(Grupo grupo);
    void Remove(Grupo grupo);
    Task SaveChangesAsync();
}
