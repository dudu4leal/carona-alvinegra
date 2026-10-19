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
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do participante é obrigatório.");

        if (string.IsNullOrWhiteSpace(whatsApp))
            throw new DomainException("WhatsApp do participante é obrigatório.");

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        WhatsApp = whatsApp.Trim();
        GrupoId = grupoId;
    }
}
