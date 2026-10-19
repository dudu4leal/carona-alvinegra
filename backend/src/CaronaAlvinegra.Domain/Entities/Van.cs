using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Entities;

public class Van
{
    public const int Capacidade = 15;

    public Guid Id { get; private set; }
    public Rota Rota { get; private set; }
    public string Motorista { get; private set; }
    public Guid ResponsavelId { get; private set; }

    public Van(Rota rota, string motorista, Guid responsavelId)
    {
        if (string.IsNullOrWhiteSpace(motorista))
            throw new DomainException("Motorista da van é obrigatório.");

        Id = Guid.NewGuid();
        Rota = rota;
        Motorista = motorista.Trim();
        ResponsavelId = responsavelId;
    }
}
