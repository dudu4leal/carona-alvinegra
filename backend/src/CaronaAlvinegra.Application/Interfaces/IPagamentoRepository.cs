using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Interfaces;

public interface IPagamentoRepository
{
    Task<Pagamento?> GetByParticipanteJogoAsync(Guid participanteId, Guid jogoId);
    Task<List<Pagamento>> GetByJogoAsync(Guid jogoId);
    Task AddAsync(Pagamento pagamento);
    void Update(Pagamento pagamento);
    void Remove(Pagamento pagamento);
    Task SaveChangesAsync();
}
