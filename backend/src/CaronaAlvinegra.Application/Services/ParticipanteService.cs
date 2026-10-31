using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Application.Services;

public class ParticipanteService
{
    private readonly IParticipanteRepository _participantes;
    private readonly IGrupoRepository _grupos;

    public ParticipanteService(IParticipanteRepository participantes, IGrupoRepository grupos)
    {
        _participantes = participantes;
        _grupos = grupos;
    }

    public async Task<ParticipanteResponse> CriarAsync(ParticipanteCreateRequest request)
    {
        await ValidarGrupoComCapacidadeAsync(request.GrupoId);

        var participante = new Participante(request.Nome, request.WhatsApp, request.GrupoId);

        await _participantes.AddAsync(participante);
        await _participantes.SaveChangesAsync();

        return ParaResponse(participante);
    }

    public async Task<List<ParticipanteResponse>> ListarAsync(Guid? grupoId = null)
    {
        var participantes = await _participantes.GetAllAsync(grupoId);
        return participantes.Select(ParaResponse).ToList();
    }

    public async Task<ParticipanteResponse?> ObterAsync(Guid id)
    {
        var participante = await _participantes.GetByIdAsync(id);
        return participante is null ? null : ParaResponse(participante);
    }

    public async Task<ParticipanteResponse?> AtualizarAsync(Guid id, ParticipanteUpdateRequest request)
    {
        var participante = await _participantes.GetByIdAsync(id);

        if (participante is null)
            return null;

        if (participante.GrupoId != request.GrupoId)
            await ValidarGrupoComCapacidadeAsync(request.GrupoId);

        participante.Atualizar(request.Nome, request.WhatsApp, request.GrupoId);
        _participantes.Update(participante);
        await _participantes.SaveChangesAsync();

        return ParaResponse(participante);
    }

    public async Task<bool> ExcluirAsync(Guid id)
    {
        var participante = await _participantes.GetByIdAsync(id);

        if (participante is null)
            return false;

        _participantes.Remove(participante);
        await _participantes.SaveChangesAsync();

        return true;
    }

    private async Task ValidarGrupoComCapacidadeAsync(Guid grupoId)
    {
        if (await _grupos.GetByIdAsync(grupoId) is null)
            throw new DomainException("Grupo não encontrado.");

        if (await _participantes.CountByGrupoAsync(grupoId) >= Grupo.CapacidadeMaxima)
            throw new DomainException(
                $"Grupo já atingiu a capacidade máxima de {Grupo.CapacidadeMaxima} participantes.");
    }

    private static ParticipanteResponse ParaResponse(Participante participante) =>
        new(participante.Id, participante.Nome, participante.WhatsApp, participante.GrupoId);
}
