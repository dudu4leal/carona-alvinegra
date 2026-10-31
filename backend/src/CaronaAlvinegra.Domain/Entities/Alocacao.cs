using CaronaAlvinegra.Domain.Enums;

namespace CaronaAlvinegra.Domain.Entities;

public class Alocacao
{
    private readonly List<VanAlocada> _vans = new();
    private readonly List<Guid> _gruposEscalados = new();

    public Guid Id { get; private set; }
    public Guid JogoId { get; private set; }
    public Rota Rota { get; private set; }
    public IReadOnlyList<VanAlocada> Vans => _vans.AsReadOnly();
    public SpinAlocado? Spin { get; private set; }
    public IReadOnlyList<Guid> GruposEscalados => _gruposEscalados.AsReadOnly();

    public Alocacao(Guid jogoId, Rota rota)
    {
        Id = Guid.NewGuid();
        JogoId = jogoId;
        Rota = rota;
    }

    public void AdicionarVan(VanAlocada van) => _vans.Add(van);

    public void DefinirSpin(SpinAlocado spin) => Spin = spin;

    public void Escalar(Guid grupoId) => _gruposEscalados.Add(grupoId);
}
