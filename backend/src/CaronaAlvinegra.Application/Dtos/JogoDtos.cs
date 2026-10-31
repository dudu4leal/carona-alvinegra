namespace CaronaAlvinegra.Application.Dtos;

public record JogoCreateRequest(
    string CompeticaoCodigo,
    string Adversario,
    DateTime Data,
    TimeOnly HorarioSaida,
    string PontoEmbarque);

public record JogoUpdateRequest(
    string CompeticaoCodigo,
    string Adversario,
    DateTime Data,
    TimeOnly HorarioSaida,
    string PontoEmbarque);

public record JogoResponse(
    Guid Id,
    string CompeticaoCodigo,
    string Adversario,
    DateTime Data,
    TimeOnly HorarioSaida,
    string PontoEmbarque);
