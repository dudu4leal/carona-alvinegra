using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaronaAlvinegra.Api.Controllers;

[ApiController]
[Route("api/grupos")]
[Authorize]
public class GruposController : ControllerBase
{
    private readonly GrupoService _service;

    public GruposController(GrupoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<GrupoResponse>> Criar(GrupoCreateRequest request)
    {
        var grupo = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = grupo.Id }, grupo);
    }

    [HttpGet]
    public async Task<ActionResult<List<GrupoResponse>>> Listar()
    {
        return Ok(await _service.ListarAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GrupoResponse>> Obter(Guid id)
    {
        var grupo = await _service.ObterAsync(id);
        return grupo is null ? NotFound() : Ok(grupo);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GrupoResponse>> Atualizar(Guid id, GrupoUpdateRequest request)
    {
        var grupo = await _service.AtualizarAsync(id, request);
        return grupo is null ? NotFound() : Ok(grupo);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var resultado = await _service.ExcluirAsync(id);

        return resultado switch
        {
            ExclusaoResult.Ok => NoContent(),
            ExclusaoResult.NotFound => NotFound(),
            ExclusaoResult.Conflito => Conflict(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
