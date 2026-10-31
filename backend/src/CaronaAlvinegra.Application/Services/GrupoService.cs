using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Services;

public enum ExclusaoResult
{
    Ok,
    NotFound,
    Conflito
}

public class GrupoService
{
    private readonly IGrupoRepository _grupos;
    private readonly IParticipanteRepository _participantes;

    public GrupoService(IGrupoRepository grupos, IParticipanteRepository participantes)
    {
        _grupos = grupos;
        _participantes = participantes;
    }

    public async Task<GrupoResponse> CriarAsync(GrupoCreateRequest request)
    {
        var grupo = new Grupo(request.Nome, request.Rota);

        await _grupos.AddAsync(grupo);
        await _grupos.SaveChangesAsync();

        return ParaResponse(grupo);
    }

    public async Task<List<GrupoResponse>> ListarAsync()
    {
        var grupos = await _grupos.GetAllAsync();
        return grupos.Select(ParaResponse).ToList();
    }

    public async Task<GrupoResponse?> ObterAsync(Guid id)
    {
        var grupo = await _grupos.GetByIdAsync(id);
        return grupo is null ? null : ParaResponse(grupo);
    }

    public async Task<GrupoResponse?> AtualizarAsync(Guid id, GrupoUpdateRequest request)
    {
        var grupo = await _grupos.GetByIdAsync(id);

        if (grupo is null)
            return null;

        grupo.Atualizar(request.Nome, request.Rota);
        _grupos.Update(grupo);
        await _grupos.SaveChangesAsync();

        return ParaResponse(grupo);
    }

    public async Task<ExclusaoResult> ExcluirAsync(Guid id)
    {
        var grupo = await _grupos.GetByIdAsync(id);

        if (grupo is null)
            return ExclusaoResult.NotFound;

        if (await _participantes.CountByGrupoAsync(id) > 0)
            return ExclusaoResult.Conflito;

        _grupos.Remove(grupo);
        await _grupos.SaveChangesAsync();

        return ExclusaoResult.Ok;
    }

    private static GrupoResponse ParaResponse(Grupo grupo) =>
        new(
            grupo.Id,
            grupo.Nome,
            grupo.Rota,
            grupo.Participantes
                .Select(p => new ParticipanteResponse(p.Id, p.Nome, p.WhatsApp, p.GrupoId))
                .ToList());
}
