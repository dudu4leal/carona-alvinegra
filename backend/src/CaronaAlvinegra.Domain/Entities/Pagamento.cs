namespace CaronaAlvinegra.Domain.Entities;

public class Pagamento
{
    public Guid Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public Guid JogoId { get; private set; }
    public bool Pago { get; private set; }

    public Pagamento(Guid participanteId, Guid jogoId)
    {
        Id = Guid.NewGuid();
        ParticipanteId = participanteId;
        JogoId = jogoId;
        Pago = false;
    }

    public void MarcarPago() => Pago = true;

    public void DesmarcarPago() => Pago = false;
}
