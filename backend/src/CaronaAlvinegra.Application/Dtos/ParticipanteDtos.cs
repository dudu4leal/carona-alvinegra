namespace CaronaAlvinegra.Application.Dtos;

public record ParticipanteCreateRequest(string Nome, string WhatsApp, Guid GrupoId);
public record ParticipanteUpdateRequest(string Nome, string WhatsApp, Guid GrupoId);
public record ParticipanteResponse(Guid Id, string Nome, string WhatsApp, Guid GrupoId);
