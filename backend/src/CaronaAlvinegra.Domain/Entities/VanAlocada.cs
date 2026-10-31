using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Entities;

public class VanAlocada
{
    private readonly List<Guid> _grupoIds = new();

    public Guid Id { get; private set; }
    public string Motorista { get; private set; } = string.Empty;
    public Guid ResponsavelId { get; private set; }
    public IReadOnlyList<Guid> GrupoIds => _grupoIds.AsReadOnly();
    public int Ocupacao { get; private set; }

    private VanAlocada() { }

    public VanAlocada(string motorista, Guid responsavelId, IReadOnlyList<Guid> grupoIds, int ocupacao)
    {
        Id = Guid.NewGuid();
        Motorista = motorista;
        ResponsavelId = responsavelId;
        _grupoIds = grupoIds.ToList();
        Ocupacao = ocupacao;
    }

    public void DefinirResponsavel(string motorista, Guid responsavelId)
    {
        var motoristaNormalizado = (motorista ?? string.Empty).Trim();

        if (motoristaNormalizado.Length > 100)
            throw new DomainException("Motorista deve ter no máximo 100 caracteres.");

        Motorista = motoristaNormalizado;
        ResponsavelId = responsavelId;
    }
}
