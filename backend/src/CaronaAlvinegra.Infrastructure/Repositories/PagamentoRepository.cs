using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaronaAlvinegra.Infrastructure.Repositories;

public class PagamentoRepository : IPagamentoRepository
{
    private readonly ApplicationDbContext _context;

    public PagamentoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Pagamento?> GetByParticipanteJogoAsync(Guid participanteId, Guid jogoId) =>
        _context.Pagamentos.FirstOrDefaultAsync(p =>
            p.ParticipanteId == participanteId && p.JogoId == jogoId);

    public Task<List<Pagamento>> GetByJogoAsync(Guid jogoId) =>
        _context.Pagamentos.Where(p => p.JogoId == jogoId).ToListAsync();

    public async Task AddAsync(Pagamento pagamento)
    {
        await _context.Pagamentos.AddAsync(pagamento);
    }

    public void Update(Pagamento pagamento) => _context.Pagamentos.Update(pagamento);

    public void Remove(Pagamento pagamento) => _context.Pagamentos.Remove(pagamento);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
