using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Entities;

public class Participante
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string WhatsApp { get; private set; }
    public Guid GrupoId { get; private set; }

    public Participante(string nome, string whatsApp, Guid grupoId)
    {
        Id = Guid.NewGuid();
        Nome = ValidarNome(nome);
        WhatsApp = ValidarWhatsApp(whatsApp);
        GrupoId = grupoId;
    }

    public void Atualizar(string nome, string whatsApp, Guid grupoId)
    {
        Nome = ValidarNome(nome);
        WhatsApp = ValidarWhatsApp(whatsApp);
        GrupoId = grupoId;
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do participante é obrigatório.");

        return nome.Trim();
    }

    private static string ValidarWhatsApp(string whatsApp)
    {
        if (string.IsNullOrWhiteSpace(whatsApp))
            throw new DomainException("WhatsApp do participante é obrigatório.");

        return whatsApp.Trim();
    }
}
