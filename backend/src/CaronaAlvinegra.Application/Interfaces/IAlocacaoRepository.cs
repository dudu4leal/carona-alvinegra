using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Interfaces;

public interface IAlocacaoRepository
{
    Task AddAsync(Alocacao alocacao);
    Task<List<Alocacao>> GetByJogoAsync(Guid jogoId);
    Task RemoveByJogoAsync(Guid jogoId);
    Task SaveChangesAsync();
}
