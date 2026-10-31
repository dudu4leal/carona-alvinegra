using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CaronaAlvinegra.Infrastructure.Repositories;

public class ListaEsperaRepository : IListaEsperaRepository
{
    private readonly ApplicationDbContext _context;

    public ListaEsperaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ListaEspera?> GetByJogoRotaAsync(Guid jogoId, Rota rota) =>
        _context.ListasEspera.FirstOrDefaultAsync(l => l.JogoId == jogoId && l.Rota == rota);
}
