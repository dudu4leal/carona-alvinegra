using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Interfaces;

public interface IJogoRepository
{
    Task<Jogo?> GetByIdAsync(Guid id);
    Task<List<Jogo>> GetAllAsync();
    Task AddAsync(Jogo jogo);
    void Update(Jogo jogo);
    void Remove(Jogo jogo);
    Task SaveChangesAsync();
}
