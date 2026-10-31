using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaronaAlvinegra.Api.Controllers;

[ApiController]
[Route("api/jogos")]
[Authorize]
public class JogosController : ControllerBase
{
    private readonly JogoService _service;

    public JogosController(JogoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<JogoResponse>> Criar(JogoCreateRequest request)
    {
        var jogo = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = jogo.Id }, jogo);
    }

    [HttpGet]
    public async Task<ActionResult<List<JogoResponse>>> Listar()
    {
        return Ok(await _service.ListarAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JogoResponse>> Obter(Guid id)
    {
        var jogo = await _service.ObterAsync(id);
        return jogo is null ? NotFound() : Ok(jogo);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JogoResponse>> Atualizar(Guid id, JogoUpdateRequest request)
    {
        var jogo = await _service.AtualizarAsync(id, request);
        return jogo is null ? NotFound() : Ok(jogo);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        return await _service.ExcluirAsync(id) ? NoContent() : NotFound();
    }
}
