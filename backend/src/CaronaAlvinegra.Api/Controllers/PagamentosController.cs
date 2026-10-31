using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaronaAlvinegra.Api.Controllers;

[ApiController]
[Route("api/jogos/{jogoId}/pagamentos")]
[Authorize]
public class PagamentosController : ControllerBase
{
    private readonly PagamentoService _service;

    public PagamentosController(PagamentoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<PagamentoResponse>>> Listar(Guid jogoId)
    {
        return Ok(await _service.ListarPorJogoAsync(jogoId));
    }

    [HttpPost("{participanteId}/marcar-pago")]
    public async Task<ActionResult<PagamentoResponse>> MarcarPago(Guid jogoId, Guid participanteId)
    {
        var pagamento = await _service.MarcarPagoAsync(jogoId, participanteId);
        return pagamento is null ? NotFound() : Ok(pagamento);
    }

    [HttpPost("{participanteId}/desmarcar-pago")]
    public async Task<ActionResult<DesmarcarPagamentoResponse>> DesmarcarPago(Guid jogoId, Guid participanteId)
    {
        var resultado = await _service.DesmarcarPagoAsync(jogoId, participanteId);
        return resultado is null ? NotFound() : Ok(resultado);
    }
}
