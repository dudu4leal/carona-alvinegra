using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Services;

public class JogoService
{
    private readonly IJogoRepository _jogos;

    public JogoService(IJogoRepository jogos)
    {
        _jogos = jogos;
    }

    public async Task<JogoResponse> CriarAsync(JogoCreateRequest request)
    {
        var jogo = new Jogo(
            request.CompeticaoCodigo,
            request.Adversario,
            NormalizarData(request.Data),
            request.HorarioSaida,
            request.PontoEmbarque);

        await _jogos.AddAsync(jogo);
        await _jogos.SaveChangesAsync();

        return ParaResponse(jogo);
    }

    public async Task<List<JogoResponse>> ListarAsync()
    {
        var jogos = await _jogos.GetAllAsync();
        return jogos.Select(ParaResponse).ToList();
    }

    public async Task<JogoResponse?> ObterAsync(Guid id)
    {
        var jogo = await _jogos.GetByIdAsync(id);
        return jogo is null ? null : ParaResponse(jogo);
    }

    public async Task<JogoResponse?> AtualizarAsync(Guid id, JogoUpdateRequest request)
    {
        var jogo = await _jogos.GetByIdAsync(id);

        if (jogo is null)
            return null;

        jogo.Atualizar(
            request.CompeticaoCodigo,
            request.Adversario,
            NormalizarData(request.Data),
            request.HorarioSaida,
            request.PontoEmbarque);

        _jogos.Update(jogo);
        await _jogos.SaveChangesAsync();

        return ParaResponse(jogo);
    }

    // Npgsql mapeia DateTime para "timestamp with time zone", que só aceita UTC.
    private static DateTime NormalizarData(DateTime data) =>
        data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };

    public async Task<bool> ExcluirAsync(Guid id)
    {
        var jogo = await _jogos.GetByIdAsync(id);

        if (jogo is null)
            return false;

        _jogos.Remove(jogo);
        await _jogos.SaveChangesAsync();

        return true;
    }

    private static JogoResponse ParaResponse(Jogo jogo) =>
        new(
            jogo.Id,
            jogo.CompeticaoCodigo,
            jogo.Adversario,
            jogo.Data,
            jogo.HorarioSaida,
            jogo.PontoEmbarque);
}
