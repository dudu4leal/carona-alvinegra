using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Application.Services;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Tests;

public class ListaOficialServiceTests
{
    private const string RodapeEsperado =
        "❌ Não é permiti atraso\n" +
        "❌ Não é permitido o uso de drogas.\n" +
        "💯 Volta será uma hora após o jogo, com tolerância de 10 minutos. Caso não apareça, o responsável da van está autorizado a partir sem você.\n" +
        "✅ O carona não é meu, é nosso então, cuide dele!";

    private sealed class FakeJogoRepository : IJogoRepository
    {
        public List<Jogo> Jogos { get; } = new();

        public Task<Jogo?> GetByIdAsync(Guid id) =>
            Task.FromResult(Jogos.FirstOrDefault(j => j.Id == id));

        public Task<List<Jogo>> GetAllAsync() => Task.FromResult(Jogos.ToList());

        public Task AddAsync(Jogo jogo)
        {
            Jogos.Add(jogo);
            return Task.CompletedTask;
        }

        public void Update(Jogo jogo) { }

        public void Remove(Jogo jogo) => Jogos.Remove(jogo);

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeAlocacaoRepository : IAlocacaoRepository
    {
        public List<Alocacao> Itens { get; } = new();

        public Task AddAsync(Alocacao alocacao)
        {
            Itens.Add(alocacao);
            return Task.CompletedTask;
        }

        public Task<List<Alocacao>> GetByJogoAsync(Guid jogoId) =>
            Task.FromResult(Itens.Where(a => a.JogoId == jogoId).ToList());

        public Task RemoveByJogoAsync(Guid jogoId)
        {
            Itens.RemoveAll(a => a.JogoId == jogoId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeGrupoRepository : IGrupoRepository
    {
        public List<Grupo> Grupos { get; } = new();

        public Task<Grupo?> GetByIdAsync(Guid id) =>
            Task.FromResult(Grupos.FirstOrDefault(g => g.Id == id));

        public Task<List<Grupo>> GetAllAsync() => Task.FromResult(Grupos.ToList());

        public Task AddAsync(Grupo grupo)
        {
            Grupos.Add(grupo);
            return Task.CompletedTask;
        }

        public void Update(Grupo grupo) { }

        public void Remove(Grupo grupo) => Grupos.Remove(grupo);

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private static (
        ListaOficialService Service,
        FakeJogoRepository Jogos,
        FakeAlocacaoRepository Alocacoes,
        FakeGrupoRepository Grupos) CriarServico()
    {
        var jogos = new FakeJogoRepository();
        var alocacoes = new FakeAlocacaoRepository();
        var grupos = new FakeGrupoRepository();

        return (new ListaOficialService(jogos, alocacoes, grupos), jogos, alocacoes, grupos);
    }

    private static Jogo CriarJogo(FakeJogoRepository jogos)
    {
        var jogo = new Jogo(
            "BR-26",
            "Vasco",
            new DateTime(2026, 10, 7),
            new TimeOnly(17, 0),
            "Praça 14 bis");

        jogos.Jogos.Add(jogo);
        return jogo;
    }

    private static Grupo CriarGrupo(params string[] nomes)
    {
        var grupo = new Grupo("Grupo Alvinegro", Rota.Bingen);

        foreach (var nome in nomes)
            grupo.AdicionarParticipante(new Participante(nome, "24999999999", grupo.Id));

        return grupo;
    }

    private static Alocacao CriarAlocacaoComVan(Jogo jogo, Grupo grupo, Guid responsavelId)
    {
        var alocacao = new Alocacao(jogo.Id, Rota.Bingen);
        alocacao.AdicionarVan(new VanAlocada(
            string.Empty,
            responsavelId,
            new[] { grupo.Id },
            grupo.Participantes.Count));

        return alocacao;
    }

    [Fact]
    public async Task GerarAsync_Cabecalho_ContemFormatoOficial()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Daodock", "Nicolle");
        grupos.Grupos.Add(grupo);
        alocacoes.Itens.Add(CriarAlocacaoComVan(jogo, grupo, grupo.Participantes.First().Id));

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.StartsWith(
            "LISTA OFICIAL\n" +
            "BR-26\n" +
            "Data: 7-10, Quarta\n" +
            "Saída, 17:00\n" +
            "Embarque: Praça 14 bis ✈️\n" +
            "Botafogo x Vasco",
            resposta!.Texto);
        Assert.Contains("Van 1 🚐 🚐 ( Bingen)", resposta.Texto);
    }

    [Fact]
    public async Task GerarAsync_Responsavel_AparecePrimeiroComResp()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Nicolle", "Daodock");
        grupos.Grupos.Add(grupo);
        var responsavel = grupo.Participantes.Last().Id;
        alocacoes.Itens.Add(CriarAlocacaoComVan(jogo, grupo, responsavel));

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.Contains("1- ✅ Daodock( Resp)\n2- ✅ Nicolle", resposta!.Texto);
    }

    [Fact]
    public async Task GerarAsync_ResponsavelInexistente_NaoMarcaResp()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Daodock", "Nicolle");
        grupos.Grupos.Add(grupo);
        alocacoes.Itens.Add(CriarAlocacaoComVan(jogo, grupo, Guid.NewGuid()));

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.DoesNotContain("( Resp)", resposta!.Texto);
        Assert.Contains("1- ✅ Daodock\n2- ✅ Nicolle", resposta.Texto);
    }

    [Fact]
    public async Task GerarAsync_RodapeFixo_Presente()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Daodock");
        grupos.Grupos.Add(grupo);
        alocacoes.Itens.Add(CriarAlocacaoComVan(jogo, grupo, grupo.Participantes.First().Id));

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.EndsWith(RodapeEsperado, resposta!.Texto);
    }

    [Fact]
    public async Task GerarAsync_SemSpin_OmiteBloco()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Daodock");
        grupos.Grupos.Add(grupo);
        alocacoes.Itens.Add(CriarAlocacaoComVan(jogo, grupo, Guid.Empty));

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.DoesNotContain("Spin", resposta!.Texto);
    }

    [Fact]
    public async Task GerarAsync_ComSpin_IncluiBloco()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Jones");
        grupos.Grupos.Add(grupo);
        var alocacao = new Alocacao(jogo.Id, Rota.Bingen);
        alocacao.DefinirSpin(new SpinAlocado(new[] { grupo.Id }, 1));
        alocacoes.Itens.Add(alocacao);

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.Contains("Spin\n1- ✅ Jones", resposta!.Texto);
    }

    [Fact]
    public async Task GerarAsync_JogoInexistente_RetornaNull()
    {
        var (service, _, _, _) = CriarServico();

        Assert.Null(await service.GerarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GerarAsync_SemAlocacao_LancaDomainException()
    {
        var (service, jogos, _, _) = CriarServico();
        var jogo = CriarJogo(jogos);

        await Assert.ThrowsAsync<DomainException>(() => service.GerarAsync(jogo.Id));
    }

    [Fact]
    public async Task GerarAsync_SemParticipantesAlocados_LancaDomainException()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Daodock");
        grupos.Grupos.Add(grupo);
        var alocacao = new Alocacao(jogo.Id, Rota.Quitandinha);
        alocacao.Escalar(grupo.Id);
        alocacoes.Itens.Add(alocacao);

        await Assert.ThrowsAsync<DomainException>(() => service.GerarAsync(jogo.Id));
    }

    [Fact]
    public async Task GerarAsync_VanComGrupoExcluido_OmiteBloco()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo("Daodock");
        grupos.Grupos.Add(grupo);

        var alocacao = new Alocacao(jogo.Id, Rota.Bingen);
        alocacao.AdicionarVan(new VanAlocada(
            string.Empty,
            grupo.Participantes.First().Id,
            new[] { grupo.Id },
            1));
        alocacao.AdicionarVan(new VanAlocada(
            string.Empty,
            Guid.Empty,
            new[] { Guid.NewGuid() },
            10));
        alocacoes.Itens.Add(alocacao);

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.Contains("Van 1 🚐 🚐 ( Bingen)", resposta!.Texto);
        Assert.DoesNotContain("Van 2", resposta.Texto);
    }

    [Fact]
    public async Task GerarAsync_VanAcimaDeQuinze_LimitaQuinzeLinhas()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo1 = CriarGrupo(Enumerable.Range(1, 8).Select(n => $"A{n}").ToArray());
        var grupo2 = CriarGrupo(Enumerable.Range(1, 8).Select(n => $"B{n}").ToArray());
        grupos.Grupos.AddRange(new[] { grupo1, grupo2 });

        var alocacao = new Alocacao(jogo.Id, Rota.Bingen);
        alocacao.AdicionarVan(new VanAlocada(
            string.Empty,
            Guid.Empty,
            new[] { grupo1.Id, grupo2.Id },
            16));
        alocacoes.Itens.Add(alocacao);

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.Contains("15- ✅ B7", resposta!.Texto);
        Assert.DoesNotContain("16- ✅", resposta.Texto);
    }

    [Fact]
    public async Task GerarAsync_SpinAcimaDeSeis_LimitaSeisLinhas()
    {
        var (service, jogos, alocacoes, grupos) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo(Enumerable.Range(1, 7).Select(n => $"S{n}").ToArray());
        grupos.Grupos.Add(grupo);

        var alocacao = new Alocacao(jogo.Id, Rota.Bingen);
        alocacao.DefinirSpin(new SpinAlocado(new[] { grupo.Id }, 7));
        alocacoes.Itens.Add(alocacao);

        var resposta = await service.GerarAsync(jogo.Id);

        Assert.NotNull(resposta);
        Assert.Contains("6- ✅ S6", resposta!.Texto);
        Assert.DoesNotContain("7- ✅", resposta.Texto);
    }
}
