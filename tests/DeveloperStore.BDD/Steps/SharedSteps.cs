using DeveloperStore.BDD.Infrastructure;
using FluentAssertions;
using Reqnroll;

namespace DeveloperStore.BDD.Steps;

/// <summary>
/// Generic step definitions shared across all BDD features.
/// </summary>
[Binding]
public sealed class SharedSteps
{
    private readonly SalesApiDriver _driver;

    public SharedSteps(SalesApiDriver driver)
    {
        _driver = driver;
    }

    [Then("a resposta deve ter status {int}")]
    public void ThenRespostaComStatus(int expectedStatus)
    {
        _driver.LastResponse.Should().NotBeNull();
        ((int)_driver.LastResponse!.StatusCode).Should().Be(expectedStatus);
    }

    [Then("o status da venda retornada deve ser {string}")]
    public void ThenStatusDaVendaRetornada(string expectedStatus)
    {
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.Status.ToString().Should().Be(expectedStatus);
    }

    [Then("o tipo do erro deve ser {string}")]
    public void ThenTipoDoErro(string expectedType)
    {
        _driver.LastError.Should().NotBeNull();
        _driver.LastError!.Type.Should().Be(expectedType);
    }

    [Then("o código do erro de validação deve ser {string}")]
    public void ThenCodigoDoErroDeValidacao(string expectedCode)
    {
        _driver.LastError.Should().NotBeNull();
        _driver.LastError!.Errors.Should().NotBeNullOrEmpty();
        _driver.LastError.Errors!.Should().Contain(e => e.Code == expectedCode);
    }
}
