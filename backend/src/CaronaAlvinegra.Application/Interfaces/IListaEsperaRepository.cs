using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;

namespace CaronaAlvinegra.Application.Interfaces;

public interface IListaEsperaRepository
{
    Task<ListaEspera?> GetByJogoRotaAsync(Guid jogoId, Rota rota);
}
