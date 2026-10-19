using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Exceptions;

namespace CaronaAlvinegra.Domain.Tests;

public class GrupoTests
{
    private static Grupo CriarGrupo() => new("Família Alvinegra", Rota.Quitandinha);

    private static Participante CriarParticipante(Grupo grupo, int numero) =>
        new($"Torcedor {numero}", $"2499999{numero:0000}", grupo.Id);

    [Fact]
    public void AdicionarParticipante_AceitaAteQuinzeParticipantes()
    {
        var grupo = CriarGrupo();

        for (var i = 1; i <= Grupo.CapacidadeMaxima; i++)
            grupo.AdicionarParticipante(CriarParticipante(grupo, i));

        Assert.Equal(Grupo.CapacidadeMaxima, grupo.Participantes.Count);
    }

    [Fact]
    public void AdicionarParticipante_DecimoSextoLancaDomainException()
    {
        var grupo = CriarGrupo();

        for (var i = 1; i <= Grupo.CapacidadeMaxima; i++)
            grupo.AdicionarParticipante(CriarParticipante(grupo, i));

        var ex = Assert.Throws<DomainException>(() => grupo.AdicionarParticipante(CriarParticipante(grupo, 16)));
        Assert.Contains("capacidade máxima", ex.Message);
    }

    [Fact]
    public void RemoverParticipante_Inexistente_NaoLanca()
    {
        var grupo = CriarGrupo();

        var exception = Record.Exception(() => grupo.RemoverParticipante(Guid.NewGuid()));

        Assert.Null(exception);
    }

    [Fact]
    public void Participantes_EhSomenteLeitura()
    {
        var grupo = CriarGrupo();
        grupo.AdicionarParticipante(CriarParticipante(grupo, 1));

        var participantes = grupo.Participantes as IList<Participante>;

        Assert.True(participantes is null || participantes.IsReadOnly);
    }

    [Fact]
    public void Participante_RejeitaNomeVazio()
    {
        Assert.Throws<DomainException>(() => new Participante("   ", "24999999999", Guid.NewGuid()));
    }

    [Fact]
    public void Participante_RejeitaWhatsAppVazio()
    {
        Assert.Throws<DomainException>(() => new Participante("Jones", "", Guid.NewGuid()));
    }

    [Fact]
    public void Jogo_RejeitaAdversarioVazio()
    {
        Assert.Throws<DomainException>(() =>
            new Jogo("BR-26", " ", new DateTime(2026, 10, 20), new TimeOnly(18, 30), "Praça da Liberdade"));
    }

    [Fact]
    public void Pagamento_IniciaNaoPago_EAlternaEstado()
    {
        var pagamento = new Pagamento(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(pagamento.Pago);
        pagamento.MarcarPago();
        Assert.True(pagamento.Pago);
        pagamento.DesmarcarPago();
        Assert.False(pagamento.Pago);
    }

    [Fact]
    public void ListaEspera_AdicionarParticipante_EhIdempotente()
    {
        var lista = new ListaEspera(Guid.NewGuid(), Rota.Bingen);
        var participanteId = Guid.NewGuid();

        lista.AdicionarParticipante(participanteId);
        lista.AdicionarParticipante(participanteId);

        Assert.Single(lista.ParticipanteIds);
    }
}
