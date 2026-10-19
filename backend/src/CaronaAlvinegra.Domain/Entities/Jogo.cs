using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Entities;

public class Jogo
{
    public Guid Id { get; private set; }
    public string CompeticaoCodigo { get; private set; }
    public string Adversario { get; private set; }
    public DateTime Data { get; private set; }
    public TimeOnly HorarioSaida { get; private set; }
    public string PontoEmbarque { get; private set; }

    public Jogo(string competicaoCodigo, string adversario, DateTime data, TimeOnly horarioSaida, string pontoEmbarque)
    {
        if (string.IsNullOrWhiteSpace(competicaoCodigo))
            throw new DomainException("Código da competição é obrigatório.");

        if (string.IsNullOrWhiteSpace(adversario))
            throw new DomainException("Adversário é obrigatório.");

        if (string.IsNullOrWhiteSpace(pontoEmbarque))
            throw new DomainException("Ponto de embarque é obrigatório.");

        Id = Guid.NewGuid();
        CompeticaoCodigo = competicaoCodigo.Trim();
        Adversario = adversario.Trim();
        Data = data;
        HorarioSaida = horarioSaida;
        PontoEmbarque = pontoEmbarque.Trim();
    }
}
