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
        Id = Guid.NewGuid();
        CompeticaoCodigo = ValidarCompeticaoCodigo(competicaoCodigo);
        Adversario = ValidarAdversario(adversario);
        Data = data;
        HorarioSaida = horarioSaida;
        PontoEmbarque = ValidarPontoEmbarque(pontoEmbarque);
    }

    public void Atualizar(string competicaoCodigo, string adversario, DateTime data, TimeOnly horarioSaida, string pontoEmbarque)
    {
        CompeticaoCodigo = ValidarCompeticaoCodigo(competicaoCodigo);
        Adversario = ValidarAdversario(adversario);
        Data = data;
        HorarioSaida = horarioSaida;
        PontoEmbarque = ValidarPontoEmbarque(pontoEmbarque);
    }

    private static string ValidarCompeticaoCodigo(string competicaoCodigo)
    {
        if (string.IsNullOrWhiteSpace(competicaoCodigo))
            throw new DomainException("Código da competição é obrigatório.");

        return competicaoCodigo.Trim();
    }

    private static string ValidarAdversario(string adversario)
    {
        if (string.IsNullOrWhiteSpace(adversario))
            throw new DomainException("Adversário é obrigatório.");

        return adversario.Trim();
    }

    private static string ValidarPontoEmbarque(string pontoEmbarque)
    {
        if (string.IsNullOrWhiteSpace(pontoEmbarque))
            throw new DomainException("Ponto de embarque é obrigatório.");

        return pontoEmbarque.Trim();
    }
}
