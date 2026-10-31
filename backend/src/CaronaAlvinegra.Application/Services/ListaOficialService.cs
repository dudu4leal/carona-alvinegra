using System.Globalization;
using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Application.Services;

public class ListaOficialService
{
    private const string EmojiAviao = "✈️";
    private const string EmojiVan = "🚐 🚐";
    private const string EmojiCheck = "✅";

    private const string Rodape =
        "❌ Não é permiti atraso\n" +
        "❌ Não é permitido o uso de drogas.\n" +
        "💯 Volta será uma hora após o jogo, com tolerância de 10 minutos. Caso não apareça, o responsável da van está autorizado a partir sem você.\n" +
        EmojiCheck + " O carona não é meu, é nosso então, cuide dele!";

    private static readonly string[] DiasDaSemana =
    {
        "Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"
    };

    private readonly IJogoRepository _jogos;
    private readonly IAlocacaoRepository _alocacoes;
    private readonly IGrupoRepository _grupos;

    public ListaOficialService(
        IJogoRepository jogos,
        IAlocacaoRepository alocacoes,
        IGrupoRepository grupos)
    {
        _jogos = jogos;
        _alocacoes = alocacoes;
        _grupos = grupos;
    }

    public async Task<ListaOficialResponse?> GerarAsync(Guid jogoId)
    {
        var jogo = await _jogos.GetByIdAsync(jogoId);

        if (jogo is null)
            return null;

        var alocacoes = await _alocacoes.GetByJogoAsync(jogoId);

        if (alocacoes.Count == 0)
            throw new DomainException(
                "Nenhuma alocação encontrada para o jogo. Rode a alocação antes de gerar a lista.");

        var grupos = (await _grupos.GetAllAsync()).ToDictionary(g => g.Id);
        var blocos = new List<string> { MontarCabecalho(jogo) };
        var numeroVan = 0;

        foreach (var alocacao in alocacoes.OrderBy(a => a.Rota))
        {
            foreach (var van in alocacao.Vans)
            {
                var bloco = MontarBlocoVan(numeroVan + 1, alocacao.Rota, van, grupos);

                if (bloco is null)
                    continue;

                numeroVan++;
                blocos.Add(bloco);
            }

            if (alocacao.Spin is not null)
            {
                var blocoSpin = MontarBlocoSpin(alocacao.Spin, grupos);

                if (blocoSpin is not null)
                    blocos.Add(blocoSpin);
            }
        }

        if (blocos.Count == 1)
            throw new DomainException("Nenhum participante alocado para gerar a lista.");

        blocos.Add(Rodape);

        return new ListaOficialResponse(string.Join("\n\n", blocos));
    }

    private static string MontarCabecalho(Jogo jogo)
    {
        var data = jogo.Data.ToString("d-M", CultureInfo.InvariantCulture);
        var diaDaSemana = DiasDaSemana[(int)jogo.Data.DayOfWeek];
        var horario = jogo.HorarioSaida.ToString("HH:mm", CultureInfo.InvariantCulture);

        return
            "LISTA OFICIAL\n" +
            $"{jogo.CompeticaoCodigo}\n" +
            $"Data: {data}, {diaDaSemana}\n" +
            $"Saída, {horario}\n" +
            $"Embarque: {jogo.PontoEmbarque} {EmojiAviao}\n" +
            $"Botafogo x {jogo.Adversario}";
    }

    private static string? MontarBlocoVan(
        int numero,
        Rota rota,
        VanAlocada van,
        IReadOnlyDictionary<Guid, Grupo> grupos)
    {
        var participantes = ResolverParticipantes(van.GrupoIds, grupos);

        if (participantes.Count == 0)
            return null;

        var responsavel = participantes.FirstOrDefault(p => p.Id == van.ResponsavelId);

        if (responsavel is not null)
            participantes = new[] { responsavel }
                .Concat(participantes.Where(p => p.Id != responsavel.Id))
                .ToList();

        var linhas = new List<string> { $"Van {numero} {EmojiVan} ( {rota})" };
        var limite = Math.Min(participantes.Count, Van.Capacidade);

        for (var i = 0; i < limite; i++)
        {
            var sufixo = participantes[i].Id == van.ResponsavelId ? "( Resp)" : string.Empty;
            linhas.Add($"{i + 1}- {EmojiCheck} {participantes[i].Nome}{sufixo}");
        }

        return string.Join("\n", linhas);
    }

    private static string? MontarBlocoSpin(
        SpinAlocado spin,
        IReadOnlyDictionary<Guid, Grupo> grupos)
    {
        var participantes = ResolverParticipantes(spin.GrupoIds, grupos);

        if (participantes.Count == 0)
            return null;

        var linhas = new List<string> { "Spin" };
        var limite = Math.Min(participantes.Count, Spin.Capacidade);

        for (var i = 0; i < limite; i++)
            linhas.Add($"{i + 1}- {EmojiCheck} {participantes[i].Nome}");

        return string.Join("\n", linhas);
    }

    private static List<Participante> ResolverParticipantes(
        IReadOnlyList<Guid> grupoIds,
        IReadOnlyDictionary<Guid, Grupo> grupos)
    {
        var participantes = new List<Participante>();

        foreach (var grupoId in grupoIds)
        {
            if (grupos.TryGetValue(grupoId, out var grupo))
                participantes.AddRange(grupo.Participantes);
        }

        return participantes;
    }
}
