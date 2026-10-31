using CaronaAlvinegra.Domain.Enums;

namespace CaronaAlvinegra.Application.Dtos;

public record GrupoCreateRequest(string Nome, Rota Rota);
public record GrupoUpdateRequest(string Nome, Rota Rota);
public record GrupoResponse(Guid Id, string Nome, Rota Rota, List<ParticipanteResponse> Participantes);
