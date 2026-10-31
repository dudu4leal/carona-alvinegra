using CaronaAlvinegra.Domain.Enums;

namespace CaronaAlvinegra.Application.Dtos;

public record VanAlocadaResponse(
    Guid Id,
    string Motorista,
    Guid ResponsavelId,
    List<Guid> GrupoIds,
    int Ocupacao);

public record SpinAlocadoResponse(
    Guid Id,
    List<Guid> GrupoIds,
    int Ocupacao);

public record AlocacaoResponse(
    Guid Id,
    Guid JogoId,
    Rota Rota,
    List<VanAlocadaResponse> Vans,
    SpinAlocadoResponse? Spin,
    List<Guid> GruposEscalados,
    List<Guid> GruposPendentes);

public record DefinirResponsavelVanRequest(string Motorista, Guid ResponsavelId);
