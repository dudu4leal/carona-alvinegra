namespace CaronaAlvinegra.Application.Dtos;

public record PagamentoResponse(Guid Id, Guid ParticipanteId, Guid JogoId, bool Pago);
public record DesmarcarPagamentoResponse(bool ReembolsoDevido);
