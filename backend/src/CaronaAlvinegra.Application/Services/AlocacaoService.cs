using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;
using CaronaAlvinegra.Domain.Services;

namespace CaronaAlvinegra.Application.Services;

public class AlocacaoService
{
    private readonly IGrupoRepository _grupos;
    private readonly IPagamentoRepository _pagamentos;
    private readonly IJogoRepository _jogos;
    private readonly IAlocacaoRepository _alocacoes;
    private readonly IListaEsperaRepository _listasEspera;
    private readonly Alocador _alocador;

    public AlocacaoService(
        IGrupoRepository grupos,
        IPagamentoRepository pagamentos,
        IJogoRepository jogos,
        IAlocacaoRepository alocacoes,
        IListaEsperaRepository listasEspera,
        Alocador alocador)
    {
        _grupos = grupos;
        _pagamentos = pagamentos;
        _jogos = jogos;
        _alocacoes = alocacoes;
        _listasEspera = listasEspera;
        _alocador = alocador;
    }

    public async Task<List<AlocacaoResponse>?> AlocarJogoAsync(Guid jogoId)
    {
        if (await _jogos.GetByIdAsync(jogoId) is null)
            return null;

        var grupos = await _grupos.GetAllAsync();
        var pagantes = await ObterPagantesAsync(jogoId);
        var (confirmadosPorRota, pendentesPorRota) = Classificar(grupos, pagantes);

        await _alocacoes.RemoveByJogoAsync(jogoId);

        var respostas = new List<AlocacaoResponse>();

        foreach (var rota in RotasPresentes(confirmadosPorRota, pendentesPorRota))
        {
            var confirmados = confirmadosPorRota.GetValueOrDefault(rota) ?? new List<Grupo>();
            var pendentes = pendentesPorRota.GetValueOrDefault(rota) ?? new List<Guid>();
            var listaEspera = await _listasEspera.GetByJogoRotaAsync(jogoId, rota);
            var tamanhoEspera = listaEspera?.ParticipanteIds.Count ?? 0;

            var alocacao = _alocador.Alocar(jogoId, rota, confirmados, tamanhoEspera);
            await _alocacoes.AddAsync(alocacao);
            respostas.Add(ParaResponse(alocacao, pendentes));
        }

        await _alocacoes.SaveChangesAsync();

        return respostas;
    }

    public async Task<List<AlocacaoResponse>?> ObterAsync(Guid jogoId)
    {
        if (await _jogos.GetByIdAsync(jogoId) is null)
            return null;

        var grupos = await _grupos.GetAllAsync();
        var pagantes = await ObterPagantesAsync(jogoId);
        var (_, pendentesPorRota) = Classificar(grupos, pagantes);

        var alocacoes = await _alocacoes.GetByJogoAsync(jogoId);

        return alocacoes
            .Select(alocacao => ParaResponse(
                alocacao,
                pendentesPorRota.GetValueOrDefault(alocacao.Rota) ?? new List<Guid>()))
            .ToList();
    }

    public async Task<VanAlocadaResponse?> DefinirResponsavelVanAsync(
        Guid jogoId, Guid vanId, string motorista, Guid responsavelId)
    {
        var alocacoes = await _alocacoes.GetByJogoAsync(jogoId);

        if (alocacoes.Count == 0)
            return null;

        var alocacao = alocacoes.FirstOrDefault(a => a.Vans.Any(v => v.Id == vanId));

        if (alocacao is null)
            return null;

        var van = alocacao.Vans.First(v => v.Id == vanId);

        if (responsavelId != Guid.Empty)
        {
            var todos = await _grupos.GetAllAsync();
            var grupos = todos.Where(g => van.GrupoIds.Contains(g.Id));
            var participantesDaVan = grupos
                .SelectMany(g => g.Participantes)
                .Select(p => p.Id)
                .ToHashSet();

            if (!participantesDaVan.Contains(responsavelId))
                throw new DomainException("Responsável deve ser participante de um grupo da van.");
        }

        van.DefinirResponsavel(motorista, responsavelId);
        await _alocacoes.SaveChangesAsync();

        return new VanAlocadaResponse(
            van.Id,
            van.Motorista,
            van.ResponsavelId,
            van.GrupoIds.ToList(),
            van.Ocupacao);
    }

    private async Task<HashSet<Guid>> ObterPagantesAsync(Guid jogoId)
    {
        var pagamentos = await _pagamentos.GetByJogoAsync(jogoId);
        return pagamentos.Where(p => p.Pago).Select(p => p.ParticipanteId).ToHashSet();
    }

    private static (
        Dictionary<Rota, List<Grupo>> Confirmados,
        Dictionary<Rota, List<Guid>> Pendentes) Classificar(
        List<Grupo> grupos,
        HashSet<Guid> pagantes)
    {
        var confirmados = new Dictionary<Rota, List<Grupo>>();
        var pendentes = new Dictionary<Rota, List<Guid>>();

        foreach (var grupo in grupos)
        {
            if (grupo.Participantes.Count == 0)
                continue;

            if (grupo.Participantes.All(p => pagantes.Contains(p.Id)))
            {
                if (!confirmados.TryGetValue(grupo.Rota, out var lista))
                {
                    lista = new List<Grupo>();
                    confirmados[grupo.Rota] = lista;
                }

                lista.Add(grupo);
            }
            else
            {
                if (!pendentes.TryGetValue(grupo.Rota, out var lista))
                {
                    lista = new List<Guid>();
                    pendentes[grupo.Rota] = lista;
                }

                lista.Add(grupo.Id);
            }
        }

        return (confirmados, pendentes);
    }

    private static IEnumerable<Rota> RotasPresentes(
        Dictionary<Rota, List<Grupo>> confirmados,
        Dictionary<Rota, List<Guid>> pendentes) =>
        confirmados.Keys.Union(pendentes.Keys).OrderBy(rota => rota);

    private static AlocacaoResponse ParaResponse(Alocacao alocacao, List<Guid> pendentes) =>
        new(
            alocacao.Id,
            alocacao.JogoId,
            alocacao.Rota,
            alocacao.Vans
                .Select(v => new VanAlocadaResponse(
                    v.Id,
                    v.Motorista,
                    v.ResponsavelId,
                    v.GrupoIds.ToList(),
                    v.Ocupacao))
                .ToList(),
            alocacao.Spin is null
                ? null
                : new SpinAlocadoResponse(
                    alocacao.Spin.Id,
                    alocacao.Spin.GrupoIds.ToList(),
                    alocacao.Spin.Ocupacao),
            alocacao.GruposEscalados.ToList(),
            pendentes);
}
