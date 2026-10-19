using CaronaAlvinegra.Domain.Enums;

namespace CaronaAlvinegra.Domain.Entities;

public class ListaEspera
{
    private readonly List<Guid> _participanteIds = new();

    public Guid Id { get; private set; }
    public Guid JogoId { get; private set; }
    public Rota Rota { get; private set; }
    public IReadOnlyCollection<Guid> ParticipanteIds => _participanteIds.AsReadOnly();

    public ListaEspera(Guid jogoId, Rota rota)
    {
        Id = Guid.NewGuid();
        JogoId = jogoId;
        Rota = rota;
    }

    public void AdicionarParticipante(Guid participanteId)
    {
        if (!_participanteIds.Contains(participanteId))
            _participanteIds.Add(participanteId);
    }

    public void RemoverParticipante(Guid participanteId)
    {
        _participanteIds.Remove(participanteId);
    }
}
