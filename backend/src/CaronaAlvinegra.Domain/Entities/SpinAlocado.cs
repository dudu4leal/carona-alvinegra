namespace CaronaAlvinegra.Domain.Entities;

public class SpinAlocado
{
    private readonly List<Guid> _grupoIds = new();

    public Guid Id { get; private set; }
    public IReadOnlyList<Guid> GrupoIds => _grupoIds.AsReadOnly();
    public int Ocupacao { get; private set; }

    private SpinAlocado() { }

    public SpinAlocado(IReadOnlyList<Guid> grupoIds, int ocupacao)
    {
        Id = Guid.NewGuid();
        _grupoIds = grupoIds.ToList();
        Ocupacao = ocupacao;
    }
}
