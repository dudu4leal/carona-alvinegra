using CaronaAlvinegra.Application.Dtos;
using CaronaAlvinegra.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaronaAlvinegra.Api.Controllers;

[ApiController]
[Route("api/jogos/{jogoId}/lista")]
[Authorize]
public class ListaController : ControllerBase
{
    private readonly ListaOficialService _service;

    public ListaController(ListaOficialService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ListaOficialResponse>> Gerar(Guid jogoId)
    {
        var lista = await _service.GerarAsync(jogoId);

        return lista is null ? NotFound() : Ok(lista);
    }
}
