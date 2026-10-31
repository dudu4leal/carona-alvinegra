using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaronaAlvinegra.Api.Controllers;

[ApiController]
[Route("api/participantes")]
[Authorize]
public class ParticipantesController : ControllerBase
{
    private readonly ParticipanteService _service;

    public ParticipantesController(ParticipanteService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ParticipanteResponse>> Criar(ParticipanteCreateRequest request)
    {
        var participante = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = participante.Id }, participante);
    }

    [HttpGet]
    public async Task<ActionResult<List<ParticipanteResponse>>> Listar([FromQuery] Guid? grupoId)
    {
        return Ok(await _service.ListarAsync(grupoId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ParticipanteResponse>> Obter(Guid id)
    {
        var participante = await _service.ObterAsync(id);
        return participante is null ? NotFound() : Ok(participante);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ParticipanteResponse>> Atualizar(Guid id, ParticipanteUpdateRequest request)
    {
        var participante = await _service.AtualizarAsync(id, request);
        return participante is null ? NotFound() : Ok(participante);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        return await _service.ExcluirAsync(id) ? NoContent() : NotFound();
    }
}
