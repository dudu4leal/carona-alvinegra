using System.IdentityModel.Tokens.Jwt;
using CaronaAlvinegra.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CaronaAlvinegra.Domain.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CriarServico() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-teste-com-mais-de-32-caracteres",
                ["Jwt:Issuer"] = "CaronaAlvinegra",
                ["Jwt:Audience"] = "CaronaAlvinegra",
                ["Jwt:ExpirationMinutes"] = "480"
            })
            .Build());

    [Fact]
    public void GenerateToken_RetornaTokenNaoVazioComClaimsSubEEmail()
    {
        var user = new IdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "jones@teste.local",
            Email = "jones@teste.local"
        };

        var token = CriarServico().GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, claim =>
            claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id);
        Assert.Contains(jwt.Claims, claim =>
            claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
    }

    [Fact]
    public void GenerateToken_SemChaveLancaInvalidOperationException()
    {
        var servico = new JwtTokenService(new ConfigurationBuilder().Build());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            servico.GenerateToken(new IdentityUser { Id = Guid.NewGuid().ToString() }));

        Assert.Contains("Jwt:Key", ex.Message);
    }
}
