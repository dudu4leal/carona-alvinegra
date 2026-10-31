using CaronaAlvinegra.Domain.Entities;
using CaronaAlvinegra.Domain.Enums;
using CaronaAlvinegra.Domain.Services;

namespace CaronaAlvinegra.Domain.Tests;

public class AlocadorTests
{
    private static readonly Guid JogoId = Guid.NewGuid();

    private static Grupo CriarGrupo(int quantidade, string nome)
    {
        var grupo = new Grupo(nome, Rota.Quitandinha);
        for (var i = 0; i < quantidade; i++)
            grupo.AdicionarParticipante(new Participante($"P{i}", $"2499999{i:0000}", grupo.Id));
        return grupo;
    }

    private static Alocacao Alocar(IReadOnlyList<Grupo> grupos, int espera = 0) =>
        new Alocador().Alocar(JogoId, Rota.Quitandinha, grupos, espera);

    [Fact]
    public void Alocar_GrupoDeQuinze_CriaUmaVanCheia()
    {
        var grupo = CriarGrupo(15, "G15");

        var alocacao = Alocar(new[] { grupo });

        var van = Assert.Single(alocacao.Vans);
        Assert.Equal(15, van.Ocupacao);
        Assert.Contains(grupo.Id, van.GrupoIds);
        Assert.Null(alocacao.Spin);
        Assert.Empty(alocacao.GruposEscalados);
    }

    [Fact]
    public void Alocar_OitoESete_CriaUmaVan()
    {
        var oito = CriarGrupo(8, "G8");
        var sete = CriarGrupo(7, "G7");

        var alocacao = Alocar(new[] { oito, sete });

        var van = Assert.Single(alocacao.Vans);
        Assert.Equal(15, van.Ocupacao);
        Assert.Null(alocacao.Spin);
        Assert.Empty(alocacao.GruposEscalados);
        Assert.All(alocacao.Vans, v => Assert.True(v.Ocupacao <= Van.Capacidade));
    }

    [Fact]
    public void Alocar_OitoSeisSeis_CriaVanESpin()
    {
        var oito = CriarGrupo(8, "G8");
        var seis1 = CriarGrupo(6, "G6a");
        var seis2 = CriarGrupo(6, "G6b");

        var alocacao = Alocar(new[] { oito, seis1, seis2 });

        var van = Assert.Single(alocacao.Vans);
        Assert.Equal(14, van.Ocupacao);
        Assert.NotNull(alocacao.Spin);
        Assert.Equal(6, alocacao.Spin!.Ocupacao);
        Assert.Empty(alocacao.GruposEscalados);
        Assert.All(alocacao.Vans, v => Assert.True(v.Ocupacao <= Van.Capacidade));
    }

    [Fact]
    public void Alocar_OitoOitoSeis_CriaVanEEscalaGrupo()
    {
        var oito1 = CriarGrupo(8, "G8a");
        var oito2 = CriarGrupo(8, "G8b");
        var seis = CriarGrupo(6, "G6");

        var alocacao = Alocar(new[] { oito1, oito2, seis });

        var van = Assert.Single(alocacao.Vans);
        Assert.Equal(14, van.Ocupacao);
        Assert.Null(alocacao.Spin);
        var escalado = Assert.Single(alocacao.GruposEscalados);
        Assert.Equal(oito2.Id, escalado);
    }

    [Fact]
    public void Alocar_Seis_CriaSpin()
    {
        var seis = CriarGrupo(6, "G6");

        var alocacao = Alocar(new[] { seis });

        Assert.Empty(alocacao.Vans);
        Assert.NotNull(alocacao.Spin);
        Assert.Equal(6, alocacao.Spin!.Ocupacao);
        Assert.Contains(seis.Id, alocacao.Spin.GrupoIds);
        Assert.Empty(alocacao.GruposEscalados);
    }

    [Fact]
    public void Alocar_SeisComEsperaSeis_PromoveVan()
    {
        var seis = CriarGrupo(6, "G6");

        var alocacao = Alocar(new[] { seis }, espera: 6);

        var van = Assert.Single(alocacao.Vans);
        Assert.Equal(12, van.Ocupacao);
        Assert.Contains(seis.Id, van.GrupoIds);
        Assert.Null(alocacao.Spin);
        Assert.Empty(alocacao.GruposEscalados);
    }

    [Fact]
    public void Alocar_SeisComEsperaCinco_MantemSpin()
    {
        var seis = CriarGrupo(6, "G6");

        var alocacao = Alocar(new[] { seis }, espera: 5);

        Assert.Empty(alocacao.Vans);
        Assert.NotNull(alocacao.Spin);
        Assert.Equal(6, alocacao.Spin!.Ocupacao);
        Assert.Empty(alocacao.GruposEscalados);
    }

    [Fact]
    public void Alocar_Dez_EscalaGrupo()
    {
        var dez = CriarGrupo(10, "G10");

        var alocacao = Alocar(new[] { dez });

        Assert.Empty(alocacao.Vans);
        Assert.Null(alocacao.Spin);
        var escalado = Assert.Single(alocacao.GruposEscalados);
        Assert.Equal(dez.Id, escalado);
    }
}
