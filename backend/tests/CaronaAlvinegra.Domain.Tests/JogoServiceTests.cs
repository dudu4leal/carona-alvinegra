using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Application.Services;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Tests;

public class JogoServiceTests
{
    private sealed class FakeJogoRepository : IJogoRepository
    {
        public List<Jogo> Jogos { get; } = new();

        public Task<Jogo?> GetByIdAsync(Guid id) =>
            Task.FromResult(Jogos.FirstOrDefault(j => j.Id == id));

        public Task<List<Jogo>> GetAllAsync() =>
            Task.FromResult(Jogos.OrderBy(j => j.Data).ToList());

        public Task AddAsync(Jogo jogo)
        {
            Jogos.Add(jogo);
            return Task.CompletedTask;
        }

        public void Update(Jogo jogo) { }

        public void Remove(Jogo jogo) => Jogos.Remove(jogo);

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private static JogoCreateRequest CriarRequest() =>
        new("BR-26", "Vasco", new DateTime(2026, 10, 7), new TimeOnly(17, 0), "Praça 14 bis");

    [Fact]
    public async Task CriarAsync_AdversarioVazio_LancaDomainException()
    {
        var service = new JogoService(new FakeJogoRepository());
        var request = CriarRequest() with { Adversario = "   " };

        await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(request));
    }

    [Fact]
    public async Task CriarAsync_PersisteJogo()
    {
        var repositorio = new FakeJogoRepository();
        var service = new JogoService(repositorio);

        var response = await service.CriarAsync(CriarRequest());

        Assert.Single(repositorio.Jogos);
        Assert.Equal("BR-26", response.CompeticaoCodigo);
        Assert.Equal("Vasco", response.Adversario);
    }

    [Fact]
    public async Task ObterAsync_RetornaJogoCriado()
    {
        var service = new JogoService(new FakeJogoRepository());
        var criado = await service.CriarAsync(CriarRequest());

        var obtido = await service.ObterAsync(criado.Id);

        Assert.NotNull(obtido);
        Assert.Equal(criado.Id, obtido!.Id);
        Assert.Equal("Praça 14 bis", obtido.PontoEmbarque);
    }

    [Fact]
    public async Task AtualizarAsync_AlteraDadosDoJogo()
    {
        var service = new JogoService(new FakeJogoRepository());
        var criado = await service.CriarAsync(CriarRequest());

        var atualizado = await service.AtualizarAsync(
            criado.Id,
            new JogoUpdateRequest("BR-26", "Flamengo", new DateTime(2026, 10, 8), new TimeOnly(18, 0), "Praça da Liberdade"));

        Assert.NotNull(atualizado);
        Assert.Equal("Flamengo", atualizado!.Adversario);
        Assert.Equal(new DateTime(2026, 10, 8), atualizado.Data);
    }

    [Fact]
    public async Task ExcluirAsync_RemoveJogo()
    {
        var repositorio = new FakeJogoRepository();
        var service = new JogoService(repositorio);
        var criado = await service.CriarAsync(CriarRequest());

        var excluido = await service.ExcluirAsync(criado.Id);

        Assert.True(excluido);
        Assert.Empty(repositorio.Jogos);
    }

    [Fact]
    public async Task CriarAsync_DataSemFuso_NormalizaParaUtc()
    {
        var repositorio = new FakeJogoRepository();
        var service = new JogoService(repositorio);

        await service.CriarAsync(CriarRequest());

        Assert.Equal(DateTimeKind.Utc, repositorio.Jogos.Single().Data.Kind);
    }

    [Fact]
    public async Task AtualizarAsync_DataSemFuso_NormalizaParaUtc()
    {
        var repositorio = new FakeJogoRepository();
        var service = new JogoService(repositorio);
        var criado = await service.CriarAsync(CriarRequest());

        await service.AtualizarAsync(
            criado.Id,
            new JogoUpdateRequest("BR-26", "Flamengo", new DateTime(2026, 10, 8), new TimeOnly(18, 0), "Praça da Liberdade"));

        Assert.Equal(DateTimeKind.Utc, repositorio.Jogos.Single().Data.Kind);
    }

    [Fact]
    public async Task ObterAsync_Inexistente_RetornaNull()
    {
        var service = new JogoService(new FakeJogoRepository());

        Assert.Null(await service.ObterAsync(Guid.NewGuid()));
    }
}
