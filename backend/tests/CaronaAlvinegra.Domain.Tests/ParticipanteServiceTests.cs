using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Application.Services;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Tests;

public class ParticipanteServiceTests
{
    private sealed class FakeGrupoRepository : IGrupoRepository
    {
        public List<Grupo> Grupos { get; } = new();

        public Task<Grupo?> GetByIdAsync(Guid id) =>
            Task.FromResult(Grupos.FirstOrDefault(g => g.Id == id));

        public Task<List<Grupo>> GetAllAsync() =>
            Task.FromResult(Grupos.ToList());

        public Task AddAsync(Grupo grupo)
        {
            Grupos.Add(grupo);
            return Task.CompletedTask;
        }

        public void Update(Grupo grupo) { }

        public void Remove(Grupo grupo) => Grupos.Remove(grupo);

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

    private static (ParticipanteService Service, FakeGrupoRepository Grupos, FakeParticipanteRepository Participantes)
        CriarServico()
    {
        var grupos = new FakeGrupoRepository();
        var participantes = new FakeParticipanteRepository();
        return (new ParticipanteService(participantes, grupos), grupos, participantes);
    }

    private static Grupo CriarGrupo(FakeGrupoRepository grupos, string nome = "Família Alvinegra")
    {
        var grupo = new Grupo(nome, Rota.Quitandinha);
        grupos.Grupos.Add(grupo);
        return grupo;
    }

    private static async Task PreencherGrupo(ParticipanteService service, Guid grupoId, int quantidade)
    {
        for (var i = 1; i <= quantidade; i++)
            await service.CriarAsync(new ParticipanteCreateRequest($"Torcedor {i}", $"2499999{i:0000}", grupoId));
    }

    [Fact]
    public async Task CriarAsync_AceitaAteQuinzeParticipantes()
    {
        var (service, grupos, participantes) = CriarServico();
        var grupo = CriarGrupo(grupos);

        await PreencherGrupo(service, grupo.Id, Grupo.CapacidadeMaxima);

        Assert.Equal(Grupo.CapacidadeMaxima, participantes.Participantes.Count);
    }

    [Fact]
    public async Task CriarAsync_DecimoSextoLancaDomainException()
    {
        var (service, grupos, _) = CriarServico();
        var grupo = CriarGrupo(grupos);
        await PreencherGrupo(service, grupo.Id, Grupo.CapacidadeMaxima);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(new ParticipanteCreateRequest("Excedente", "24999999999", grupo.Id)));

        Assert.Contains("capacidade máxima", ex.Message);
    }

    [Fact]
    public async Task AtualizarAsync_MoverParaGrupoCheioLancaDomainException()
    {
        var (service, grupos, participantes) = CriarServico();
        var origem = CriarGrupo(grupos, "Origem");
        var destino = CriarGrupo(grupos, "Destino");
        await PreencherGrupo(service, origem.Id, 1);
        await PreencherGrupo(service, destino.Id, Grupo.CapacidadeMaxima);

        var participante = participantes.Participantes.First(p => p.GrupoId == origem.Id);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.AtualizarAsync(
                participante.Id,
                new ParticipanteUpdateRequest(participante.Nome, participante.WhatsApp, destino.Id)));

        Assert.Contains("capacidade máxima", ex.Message);
    }

    [Fact]
    public async Task CriarAsync_GrupoInexistenteLancaDomainException()
    {
        var (service, _, _) = CriarServico();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(new ParticipanteCreateRequest("Jones", "24999999999", Guid.NewGuid())));

        Assert.Contains("Grupo não encontrado", ex.Message);
    }

    [Fact]
    public async Task AtualizarAsync_MesmoGrupoCheio_NaoLanca()
    {
        var (service, grupos, participantes) = CriarServico();
        var grupo = CriarGrupo(grupos);
        await PreencherGrupo(service, grupo.Id, Grupo.CapacidadeMaxima);

        var participante = participantes.Participantes.First();

        var atualizado = await service.AtualizarAsync(
            participante.Id,
            new ParticipanteUpdateRequest("Nome Atualizado", participante.WhatsApp, grupo.Id));

        Assert.NotNull(atualizado);
        Assert.Equal("Nome Atualizado", atualizado!.Nome);
    }

    [Fact]
    public async Task ExcluirAsync_GrupoComParticipantes_RetornaConflito()
    {
        var grupos = new FakeGrupoRepository();
        var participantes = new FakeParticipanteRepository();
        var grupo = CriarGrupo(grupos);
        participantes.Participantes.Add(new Participante("Jones", "24999999999", grupo.Id));
        var service = new GrupoService(grupos, participantes);

        var resultado = await service.ExcluirAsync(grupo.Id);

        Assert.Equal(ExclusaoResult.Conflito, resultado);
    }
}
