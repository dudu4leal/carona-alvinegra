using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;

namespace CaronaAlvinegra.Domain.Services;

public class Alocador
{
    private const int OcupacaoMinimaVan = 12;

    public Alocacao Alocar(Guid jogoId, Rota rota, IReadOnlyList<Grupo> grupos, int tamanhoListaEspera = 0)
    {
        var alocacao = new Alocacao(jogoId, rota);

        var ordenados = grupos.OrderByDescending(g => g.Participantes.Count).ToList();

        var vans = new List<(List<Grupo> Grupos, int Ocupacao)>();
        foreach (var grupo in ordenados)
        {
            var tamanho = grupo.Participantes.Count;
            var indice = vans.FindIndex(v => v.Ocupacao + tamanho <= Van.Capacidade);

            if (indice < 0)
            {
                vans.Add((new List<Grupo>(), 0));
                indice = vans.Count - 1;
            }

            var (gruposDaVan, ocupacao) = vans[indice];
            gruposDaVan.Add(grupo);
            vans[indice] = (gruposDaVan, ocupacao + tamanho);
        }

        var sobra = new List<Grupo>();
        foreach (var (gruposDaVan, ocupacao) in vans)
        {
            if (ocupacao >= OcupacaoMinimaVan)
            {
                alocacao.AdicionarVan(new VanAlocada(
                    string.Empty,
                    Guid.Empty,
                    gruposDaVan.Select(g => g.Id).ToList(),
                    ocupacao));
            }
            else
            {
                sobra.AddRange(gruposDaVan);
            }
        }

        var ocupacaoSobra = sobra.Sum(g => g.Participantes.Count);
        SpinAlocado? spin = null;

        if (sobra.Count > 0 && ocupacaoSobra <= Spin.Capacidade)
        {
            spin = new SpinAlocado(sobra.Select(g => g.Id).ToList(), ocupacaoSobra);
        }
        else if (ocupacaoSobra >= Spin.Capacidade + 1)
        {
            foreach (var grupo in sobra)
                alocacao.Escalar(grupo.Id);
        }

        if (spin is not null)
        {
            var ocupacaoPromovida = spin.Ocupacao + tamanhoListaEspera;

            if (ocupacaoPromovida >= OcupacaoMinimaVan)
            {
                alocacao.AdicionarVan(new VanAlocada(
                    string.Empty,
                    Guid.Empty,
                    spin.GrupoIds.ToList(),
                    ocupacaoPromovida));
            }
            else
            {
                alocacao.DefinirSpin(spin);
            }
        }

        return alocacao;
    }
}
