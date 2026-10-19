using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Entities;

public class Grupo
{
    public const int CapacidadeMaxima = 15;

    private readonly List<Participante> _participantes = new();

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Rota Rota { get; private set; }
    public IReadOnlyCollection<Participante> Participantes => _participantes.AsReadOnly();

    public Grupo(string nome, Rota rota)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do grupo é obrigatório.");

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Rota = rota;
    }

    public void AdicionarParticipante(Participante participante)
    {
        if (_participantes.Count >= CapacidadeMaxima)
            throw new DomainException($"Grupo já atingiu a capacidade máxima de {CapacidadeMaxima} participantes.");

        _participantes.Add(participante);
    }

    public void RemoverParticipante(Guid participanteId)
    {
        _participantes.RemoveAll(p => p.Id == participanteId);
    }
}
