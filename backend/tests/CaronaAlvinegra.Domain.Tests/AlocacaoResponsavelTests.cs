using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Application.Services;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;
using CaronaAlvinegra.Domain.Services;

namespace CaronaAlvinegra.Domain.Tests;

public class AlocacaoResponsavelTests
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
        public Task<Jogo?> GetByIdAsync(Guid id) => Task.FromResult<Jogo?>(null);

        public Task<List<Jogo>> GetAllAsync() => Task.FromResult(new List<Jogo>());

        public Task AddAsync(Jogo jogo) => Task.CompletedTask;

        public void Update(Jogo jogo) { }

        public void Remove(Jogo jogo) { }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakePagamentoRepository : IPagamentoRepository
    {
        public Task<Pagamento?> GetByParticipanteJogoAsync(Guid participanteId, Guid jogoId) =>
            Task.FromResult<Pagamento?>(null);

        public Task<List<Pagamento>> GetByJogoAsync(Guid jogoId) =>
            Task.FromResult(new List<Pagamento>());

        public Task AddAsync(Pagamento pagamento) => Task.CompletedTask;

        public void Update(Pagamento pagamento) { }

        public void Remove(Pagamento pagamento) { }

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
        FakeAlocacaoRepository Alocacoes,
        FakeGrupoRepository Grupos) CriarServico()
    {
        var grupos = new FakeGrupoRepository();
        var alocacoes = new FakeAlocacaoRepository();

        var service = new AlocacaoService(
            grupos,
            new FakePagamentoRepository(),
            new FakeJogoRepository(),
            alocacoes,
            new FakeListaEsperaRepository(),
            new Alocador());

        return (service, alocacoes, grupos);
    }

    private static Grupo CriarGrupo(params string[] nomes)
    {
        var grupo = new Grupo("Grupo Alvinegro", Rota.Bingen);

        foreach (var nome in nomes)
            grupo.AdicionarParticipante(new Participante(nome, "24999999999", grupo.Id));

        return grupo;
    }

    private static (Alocacao Alocacao, VanAlocada Van, Grupo Grupo) CriarCenario(Guid jogoId)
    {
        var grupo = CriarGrupo("Daodock", "Nicolle");
        var alocacao = new Alocacao(jogoId, Rota.Bingen);
        var van = new VanAlocada(
            string.Empty,
            Guid.Empty,
            new[] { grupo.Id },
            grupo.Participantes.Count);

        alocacao.AdicionarVan(van);

        return (alocacao, van, grupo);
    }

    [Fact]
    public async Task DefinirResponsavelVanAsync_Sucesso_AtualizaMotoristaEResponsavel()
    {
        var (service, alocacoes, grupos) = CriarServico();
        var jogoId = Guid.NewGuid();
        var (alocacao, van, grupo) = CriarCenario(jogoId);
        alocacoes.Itens.Add(alocacao);
        grupos.Grupos.Add(grupo);
        var responsavel = grupo.Participantes.First();

        var response = await service.DefinirResponsavelVanAsync(
            jogoId, van.Id, "  Seu Zé  ", responsavel.Id);

        Assert.NotNull(response);
        Assert.Equal("Seu Zé", response!.Motorista);
        Assert.Equal(responsavel.Id, response.ResponsavelId);
        Assert.Equal(grupo.Participantes.Count, response.Ocupacao);
        Assert.Equal(van.GrupoIds, response.GrupoIds);
    }

    [Fact]
    public async Task DefinirResponsavelVanAsync_JogoSemAlocacao_RetornaNull()
    {
        var (service, _, _) = CriarServico();

        Assert.Null(await service.DefinirResponsavelVanAsync(
            Guid.NewGuid(), Guid.NewGuid(), "Seu Zé", Guid.NewGuid()));
    }

    [Fact]
    public async Task DefinirResponsavelVanAsync_VanInexistente_RetornaNull()
    {
        var (service, alocacoes, grupos) = CriarServico();
        var jogoId = Guid.NewGuid();
        var (alocacao, _, grupo) = CriarCenario(jogoId);
        alocacoes.Itens.Add(alocacao);
        grupos.Grupos.Add(grupo);

        Assert.Null(await service.DefinirResponsavelVanAsync(
            jogoId, Guid.NewGuid(), "Seu Zé", Guid.Empty));
    }

    [Fact]
    public async Task DefinirResponsavelVanAsync_ResponsavelDeOutroGrupo_LancaDomainException()
    {
        var (service, alocacoes, grupos) = CriarServico();
        var jogoId = Guid.NewGuid();
        var (alocacao, van, grupo) = CriarCenario(jogoId);
        var outroGrupo = CriarGrupo("Outro");
        alocacoes.Itens.Add(alocacao);
        grupos.Grupos.Add(grupo);
        grupos.Grupos.Add(outroGrupo);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.DefinirResponsavelVanAsync(
                jogoId, van.Id, "Seu Zé", outroGrupo.Participantes.First().Id));

        Assert.Contains("Responsável", ex.Message);
    }

    [Fact]
    public async Task DefinirResponsavelVanAsync_GuidEmpty_LimpaResponsavel()
    {
        var (service, alocacoes, grupos) = CriarServico();
        var jogoId = Guid.NewGuid();
        var (alocacao, van, grupo) = CriarCenario(jogoId);
        alocacoes.Itens.Add(alocacao);
        grupos.Grupos.Add(grupo);
        var responsavel = grupo.Participantes.First();

        await service.DefinirResponsavelVanAsync(jogoId, van.Id, "Seu Zé", responsavel.Id);
        var response = await service.DefinirResponsavelVanAsync(jogoId, van.Id, "Seu Zé", Guid.Empty);

        Assert.NotNull(response);
        Assert.Equal(Guid.Empty, response!.ResponsavelId);
    }

    [Fact]
    public async Task DefinirResponsavelVanAsync_MotoristaAcimaDeCem_LancaDomainException()
    {
        var (service, alocacoes, grupos) = CriarServico();
        var jogoId = Guid.NewGuid();
        var (alocacao, van, grupo) = CriarCenario(jogoId);
        alocacoes.Itens.Add(alocacao);
        grupos.Grupos.Add(grupo);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.DefinirResponsavelVanAsync(
                jogoId, van.Id, new string('a', 101), grupo.Participantes.First().Id));

        Assert.Contains("100", ex.Message);
    }
}
