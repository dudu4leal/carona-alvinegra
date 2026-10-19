using CaronaAlvinegra.Domain.Enums;

namespace CaronaAlvinegra.Domain.Entities;

public class Spin
{
    public const int Capacidade = 6;

    public Guid Id { get; private set; }
    public Rota Rota { get; private set; }

    public Spin(Rota rota)
    {
        Id = Guid.NewGuid();
        Rota = rota;
    }
}
