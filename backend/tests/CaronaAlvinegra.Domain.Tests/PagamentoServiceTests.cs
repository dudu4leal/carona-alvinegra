using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Application.Services;
using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Domain.Tests;

public class PagamentoServiceTests
{
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

    private sealed class FakeParticipanteRepository : IParticipanteRepository
    {
        public List<Participante> Participantes { get; } = new();

        public Task<Participante?> GetByIdAsync(Guid id) =>
            Task.FromResult(Participantes.FirstOrDefault(p => p.Id == id));

        public Task<List<Participante>> GetAllAsync(Guid? grupoId = null) =>
            Task.FromResult(grupoId.HasValue
                ? Participantes.Where(p => p.GrupoId == grupoId.Value).ToList()
                : Participantes.ToList());

        public Task AddAsync(Participante participante)
        {
            Participantes.Add(participante);
            return Task.CompletedTask;
        }

        public void Update(Participante participante) { }

        public void Remove(Participante participante) => Participantes.Remove(participante);

        public Task<int> CountByGrupoAsync(Guid grupoId) =>
            Task.FromResult(Participantes.Count(p => p.GrupoId == grupoId));

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private static (
        PagamentoService Service,
        FakePagamentoRepository Pagamentos,
        FakeJogoRepository Jogos,
        FakeParticipanteRepository Participantes) CriarServico()
    {
        var pagamentos = new FakePagamentoRepository();
        var jogos = new FakeJogoRepository();
        var participantes = new FakeParticipanteRepository();
        return (new PagamentoService(pagamentos, jogos, participantes), pagamentos, jogos, participantes);
    }

    private static Jogo CriarJogo(FakeJogoRepository jogos, DateTime momento)
    {
        var jogo = new Jogo("BR-26", "Vasco", momento.Date, TimeOnly.FromDateTime(momento), "Praça 14 bis");
        jogos.Jogos.Add(jogo);
        return jogo;
    }

    private static Participante CriarParticipante(FakeParticipanteRepository participantes)
    {
        var participante = new Participante("Jones", "24999999999", Guid.NewGuid());
        participantes.Participantes.Add(participante);
        return participante;
    }

    [Fact]
    public async Task MarcarPagoAsync_CriaPagamentoPago()
    {
        var (service, pagamentos, jogos, participantes) = CriarServico();
        var jogo = CriarJogo(jogos, DateTime.Now.AddDays(7));
        var participante = CriarParticipante(participantes);

        var response = await service.MarcarPagoAsync(jogo.Id, participante.Id);

        Assert.NotNull(response);
        Assert.True(response!.Pago);
        Assert.Single(pagamentos.Pagamentos);
    }

    [Fact]
    public async Task MarcarPagoAsync_QuandoJaPago_EhIdempotente()
    {
        var (service, pagamentos, jogos, participantes) = CriarServico();
        var jogo = CriarJogo(jogos, DateTime.Now.AddDays(7));
        var participante = CriarParticipante(participantes);

        await service.MarcarPagoAsync(jogo.Id, participante.Id);
        var segundo = await service.MarcarPagoAsync(jogo.Id, participante.Id);

        Assert.NotNull(segundo);
        Assert.True(segundo!.Pago);
        Assert.Single(pagamentos.Pagamentos);
    }

    [Fact]
    public async Task MarcarPagoAsync_JogoInexistente_RetornaNull()
    {
        var (service, _, _, participantes) = CriarServico();
        var participante = CriarParticipante(participantes);

        Assert.Null(await service.MarcarPagoAsync(Guid.NewGuid(), participante.Id));
    }

    [Fact]
    public async Task MarcarPagoAsync_ParticipanteInexistente_RetornaNull()
    {
        var (service, _, jogos, _) = CriarServico();
        var jogo = CriarJogo(jogos, DateTime.Now.AddDays(7));

        Assert.Null(await service.MarcarPagoAsync(jogo.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task DesmarcarPagoAsync_MaisDeQuarentaEOitoHoras_ReembolsoDevido()
    {
        var (service, pagamentos, jogos, participantes) = CriarServico();
        var jogo = CriarJogo(jogos, DateTime.Now.AddHours(72));
        var participante = CriarParticipante(participantes);
        await service.MarcarPagoAsync(jogo.Id, participante.Id);

        var response = await service.DesmarcarPagoAsync(jogo.Id, participante.Id);

        Assert.NotNull(response);
        Assert.True(response!.ReembolsoDevido);
        Assert.False(pagamentos.Pagamentos.Single().Pago);
    }

    [Fact]
    public async Task DesmarcarPagoAsync_MenosDeQuarentaEOitoHoras_SemReembolso()
    {
        var (service, pagamentos, jogos, participantes) = CriarServico();
        var jogo = CriarJogo(jogos, DateTime.Now.AddHours(24));
        var participante = CriarParticipante(participantes);
        await service.MarcarPagoAsync(jogo.Id, participante.Id);

        var response = await service.DesmarcarPagoAsync(jogo.Id, participante.Id);

        Assert.NotNull(response);
        Assert.False(response!.ReembolsoDevido);
        Assert.False(pagamentos.Pagamentos.Single().Pago);
    }

    [Fact]
    public async Task DesmarcarPagoAsync_QuandoJaNaoPago_MantemFalse()
    {
        var (service, pagamentos, jogos, participantes) = CriarServico();
        var jogo = CriarJogo(jogos, DateTime.Now.AddHours(72));
        var participante = CriarParticipante(participantes);
        await service.MarcarPagoAsync(jogo.Id, participante.Id);
        await service.DesmarcarPagoAsync(jogo.Id, participante.Id);

        var response = await service.DesmarcarPagoAsync(jogo.Id, participante.Id);

        Assert.NotNull(response);
        Assert.Single(pagamentos.Pagamentos);
        Assert.False(pagamentos.Pagamentos.Single().Pago);
    }

    [Fact]
    public async Task DesmarcarPagoAsync_SemPagamento_RetornaSemReembolsoESemCriar()
    {
        var (service, pagamentos, jogos, participantes) = CriarServico();
        var jogo = CriarJogo(jogos, DateTime.Now.AddHours(72));
        var participante = CriarParticipante(participantes);

        var response = await service.DesmarcarPagoAsync(jogo.Id, participante.Id);

        Assert.NotNull(response);
        Assert.False(response!.ReembolsoDevido);
        Assert.Empty(pagamentos.Pagamentos);
    }

    [Fact]
    public async Task DesmarcarPagoAsync_JogoInexistente_RetornaNull()
    {
        var (service, _, _, _) = CriarServico();

        Assert.Null(await service.DesmarcarPagoAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
