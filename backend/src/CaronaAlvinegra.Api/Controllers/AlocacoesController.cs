using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaronaAlvinegra.Api.Controllers;

[ApiController]
[Route("api/jogos/{jogoId}")]
[Authorize]
public class AlocacoesController : ControllerBase
{
    private readonly AlocacaoService _service;

    public AlocacoesController(AlocacaoService service)
    {
        _service = service;
    }

    [HttpPost("alocar")]
    public async Task<ActionResult<List<AlocacaoResponse>>> Alocar(Guid jogoId)
    {
        var resultado = await _service.AlocarJogoAsync(jogoId);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpGet("alocacao")]
    public async Task<ActionResult<List<AlocacaoResponse>>> Obter(Guid jogoId)
    {
        var resultado = await _service.ObterAsync(jogoId);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPut("alocacao/vans/{vanId}")]
    public async Task<ActionResult<VanAlocadaResponse>> DefinirResponsavelVan(
        Guid jogoId, Guid vanId, [FromBody] DefinirResponsavelVanRequest request)
    {
        var response = await _service.DefinirResponsavelVanAsync(
            jogoId, vanId, request.Motorista, request.ResponsavelId);

        return response is null ? NotFound() : Ok(response);
    }
}
