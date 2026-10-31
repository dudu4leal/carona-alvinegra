using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Interfaces;
using CaronaAlvinegra.Domain.Entities;

namespace CaronaAlvinegra.Application.Services;

public class PagamentoService
{
    private readonly IPagamentoRepository _pagamentos;
    private readonly IJogoRepository _jogos;
    private readonly IParticipanteRepository _participantes;

    // Horário de Brasília (UTC-3, sem horário de verão desde 2019).
    private static readonly TimeSpan FusoBrasilia = TimeSpan.FromHours(-3);

    public PagamentoService(
        IPagamentoRepository pagamentos,
        IJogoRepository jogos,
        IParticipanteRepository participantes)
    {
        _pagamentos = pagamentos;
        _jogos = jogos;
        _participantes = participantes;
    }

    public async Task<PagamentoResponse?> MarcarPagoAsync(Guid jogoId, Guid participanteId)
    {
        if (await _jogos.GetByIdAsync(jogoId) is null)
            return null;

        if (await _participantes.GetByIdAsync(participanteId) is null)
            return null;

        var pagamento = await _pagamentos.GetByParticipanteJogoAsync(participanteId, jogoId);

        if (pagamento is null)
        {
            pagamento = new Pagamento(participanteId, jogoId);
            pagamento.MarcarPago();
            await _pagamentos.AddAsync(pagamento);
        }
        else
        {
            pagamento.MarcarPago();
            _pagamentos.Update(pagamento);
        }

        await _pagamentos.SaveChangesAsync();

        return ParaResponse(pagamento);
    }

    public async Task<DesmarcarPagamentoResponse?> DesmarcarPagoAsync(Guid jogoId, Guid participanteId)
    {
        var jogo = await _jogos.GetByIdAsync(jogoId);

        if (jogo is null)
            return null;

        var pagamento = await _pagamentos.GetByParticipanteJogoAsync(participanteId, jogoId);

        if (pagamento is null)
            return new DesmarcarPagamentoResponse(ReembolsoDevido: false);

        pagamento.DesmarcarPago();
        _pagamentos.Update(pagamento);
        await _pagamentos.SaveChangesAsync();

        // "Horário de saída" é horário de Brasília; converte o momento do jogo para UTC.
        var momentoJogoUtc = DateTime.SpecifyKind(
            jogo.Data.Date.Add(jogo.HorarioSaida.ToTimeSpan()).Subtract(FusoBrasilia),
            DateTimeKind.Utc);
        var reembolsoDevido = DateTime.UtcNow <= momentoJogoUtc.AddHours(-48);

        return new DesmarcarPagamentoResponse(reembolsoDevido);
    }

    public async Task<List<PagamentoResponse>> ListarPorJogoAsync(Guid jogoId)
    {
        var pagamentos = await _pagamentos.GetByJogoAsync(jogoId);
        return pagamentos.Select(ParaResponse).ToList();
    }

    private static PagamentoResponse ParaResponse(Pagamento pagamento) =>
        new(pagamento.Id, pagamento.ParticipanteId, pagamento.JogoId, pagamento.Pago);
}
