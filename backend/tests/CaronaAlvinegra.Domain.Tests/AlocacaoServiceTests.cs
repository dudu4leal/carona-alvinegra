using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Application.Services;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Services;

namespace CaronaAlvinegra.Domain.Tests;

public class AlocacaoServiceTests
{
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

    private sealed class FakePagamentoRepository : IPagamentoRepository
    {
        public List<Pagamento> Pagamentos { get; } = new();

        public Task<Pagamento?> GetByParticipanteJogoAsync(Guid participanteId, Guid jogoId) =>
            Task.FromResult(Pagamentos.FirstOrDefault(p =>
                p.ParticipanteId == participanteId && p.JogoId == jogoId));

        public Task<List<Pagamento>> GetByJogoAsync(Guid jogoId) =>
            Task.FromResult(Pagamentos.Where(p => p.JogoId == jogoId).ToList());

        public Task AddAsync(Pagamento pagamento)
        {
            Pagamentos.Add(pagamento);
            return Task.CompletedTask;
        }

        public void Update(Pagamento pagamento) { }

        public void Remove(Pagamento pagamento) => Pagamentos.Remove(pagamento);

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

    private sealed class FakeListaEsperaRepository : IListaEsperaRepository
    {
        public Task<ListaEspera?> GetByJogoRotaAsync(Guid jogoId, Rota rota) =>
            Task.FromResult<ListaEspera?>(null);
    }

    private static (
        AlocacaoService Service,
        FakeGrupoRepository Grupos,
        FakePagamentoRepository Pagamentos,
        FakeJogoRepository Jogos,
        FakeAlocacaoRepository Alocacoes) CriarServico()
    {
        var grupos = new FakeGrupoRepository();
        var pagamentos = new FakePagamentoRepository();
        var jogos = new FakeJogoRepository();
        var alocacoes = new FakeAlocacaoRepository();

        var service = new AlocacaoService(
            grupos,
            pagamentos,
            jogos,
            alocacoes,
            new FakeListaEsperaRepository(),
            new Alocador());

        return (service, grupos, pagamentos, jogos, alocacoes);
    }

    private static Jogo CriarJogo(FakeJogoRepository jogos)
    {
        var jogo = new Jogo("BR-26", "Vasco", DateTime.Now.AddDays(7).Date, new TimeOnly(17, 0), "Praça 14 bis");
        jogos.Jogos.Add(jogo);
        return jogo;
    }

    private static Grupo CriarGrupo(int quantidade)
    {
        var grupo = new Grupo($"Grupo {Guid.NewGuid():N}", Rota.Quitandinha);
        for (var i = 0; i < quantidade; i++)
            grupo.AdicionarParticipante(new Participante($"P{i}", $"2499999{i:0000}", grupo.Id));
        return grupo;
    }

    private static void Pagar(
        FakePagamentoRepository pagamentos,
        Jogo jogo,
        IEnumerable<Participante> participantes)
    {
        foreach (var participante in participantes)
        {
            var pagamento = new Pagamento(participante.Id, jogo.Id);
            pagamento.MarcarPago();
            pagamentos.Pagamentos.Add(pagamento);
        }
    }

    [Fact]
    public async Task AlocarJogoAsync_GrupoTotalmentePago_EntraNaAlocacao()
    {
        var (service, grupos, pagamentos, jogos, _) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo(2);
        grupos.Grupos.Add(grupo);
        Pagar(pagamentos, jogo, grupo.Participantes);

        var resultado = await service.AlocarJogoAsync(jogo.Id);

        Assert.NotNull(resultado);
        var alocacao = Assert.Single(resultado!);
        Assert.Equal(Rota.Quitandinha, alocacao.Rota);
        Assert.NotNull(alocacao.Spin);
        Assert.Contains(grupo.Id, alocacao.Spin!.GrupoIds);
        Assert.Empty(alocacao.GruposPendentes);
    }

    [Fact]
    public async Task AlocarJogoAsync_GrupoParcialmentePago_FicaPendente()
    {
        var (service, grupos, pagamentos, jogos, _) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo(2);
        grupos.Grupos.Add(grupo);
        Pagar(pagamentos, jogo, grupo.Participantes.Take(1));

        var resultado = await service.AlocarJogoAsync(jogo.Id);

        Assert.NotNull(resultado);
        var alocacao = Assert.Single(resultado!);
        Assert.Empty(alocacao.Vans);
        Assert.Null(alocacao.Spin);
        Assert.Contains(grupo.Id, alocacao.GruposPendentes);
    }

    [Fact]
    public async Task AlocarJogoAsync_GrupoSemParticipantes_EhIgnorado()
    {
        var (service, grupos, pagamentos, jogos, _) = CriarServico();
        var jogo = CriarJogo(jogos);
        var pago = CriarGrupo(2);
        var vazio = CriarGrupo(0);
        grupos.Grupos.AddRange(new[] { pago, vazio });
        Pagar(pagamentos, jogo, pago.Participantes);

        var resultado = await service.AlocarJogoAsync(jogo.Id);

        Assert.NotNull(resultado);
        var alocacao = Assert.Single(resultado!);
        Assert.DoesNotContain(vazio.Id, alocacao.GruposPendentes);
        Assert.DoesNotContain(vazio.Id, alocacao.Spin!.GrupoIds);
    }

    [Fact]
    public async Task AlocarJogoAsync_SemGruposComParticipantes_RetornaVazioERemoveAlocacoes()
    {
        var (service, grupos, _, jogos, alocacoes) = CriarServico();
        var jogo = CriarJogo(jogos);
        grupos.Grupos.Add(CriarGrupo(0));
        alocacoes.Itens.Add(new Alocacao(jogo.Id, Rota.Quitandinha));

        var resultado = await service.AlocarJogoAsync(jogo.Id);

        Assert.NotNull(resultado);
        Assert.Empty(resultado!);
        Assert.Empty(alocacoes.Itens);
    }

    [Fact]
    public async Task ObterAsync_RecalculaGruposPendentes()
    {
        var (service, grupos, pagamentos, jogos, _) = CriarServico();
        var jogo = CriarJogo(jogos);
        var pago = CriarGrupo(2);
        var pendente = CriarGrupo(2);
        grupos.Grupos.AddRange(new[] { pago, pendente });
        Pagar(pagamentos, jogo, pago.Participantes);

        await service.AlocarJogoAsync(jogo.Id);

        Pagar(pagamentos, jogo, pendente.Participantes);

        var resultado = await service.ObterAsync(jogo.Id);

        Assert.NotNull(resultado);
        var alocacao = Assert.Single(resultado!);
        Assert.Empty(alocacao.GruposPendentes);
    }

    [Fact]
    public async Task AlocarJogoAsync_Reexecutar_NaoDuplicaAlocacoes()
    {
        var (service, grupos, pagamentos, jogos, alocacoes) = CriarServico();
        var jogo = CriarJogo(jogos);
        var grupo = CriarGrupo(2);
        grupos.Grupos.Add(grupo);
        Pagar(pagamentos, jogo, grupo.Participantes);

        await service.AlocarJogoAsync(jogo.Id);
        await service.AlocarJogoAsync(jogo.Id);

        Assert.Single(alocacoes.Itens);
    }

    [Fact]
    public async Task AlocarJogoAsync_JogoInexistente_RetornaNull()
    {
        var (service, _, _, _, _) = CriarServico();

        Assert.Null(await service.AlocarJogoAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterAsync_JogoInexistente_RetornaNull()
    {
        var (service, _, _, _, _) = CriarServico();

        Assert.Null(await service.ObterAsync(Guid.NewGuid()));
    }
}
