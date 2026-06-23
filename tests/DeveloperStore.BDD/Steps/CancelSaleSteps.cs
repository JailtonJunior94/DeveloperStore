using DeveloperStore.BDD.Hooks;
using DeveloperStore.BDD.Infrastructure;
using DeveloperStore.Domain.Enums;
using DeveloperStore.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace DeveloperStore.BDD.Steps;

[Binding]
public sealed class CancelSaleSteps
{
    private readonly SalesApiDriver _driver;
    private readonly BddWebApplicationFactory _factory;
    private readonly ScenarioContext _scenarioContext;

    private string _rawId = string.Empty;

    public CancelSaleSteps(SalesApiDriver driver, BddWebApplicationFactory factory, ScenarioContext scenarioContext)
    {
        _driver = driver;
        _factory = factory;
        _scenarioContext = scenarioContext;
    }

    // -------------------------------------------------------------------------
    // Given — Arrange
    // -------------------------------------------------------------------------

    [Given("que existe uma venda ativa cadastrada no sistema")]
    public async Task GivenExisteUmaVendaAtivaCadastrada()
    {
        var sale = await _driver.CreateSaleAsync(new
        {
            saleNumber = $"BDD-CS-{Guid.NewGuid():N}".Substring(0, 20),
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[]
            {
                new { productExternalId = "prod-1", productName = "Product 1", quantity = 2, unitPrice = 10m }
            }
        });

        _scenarioContext["SaleId"] = sale.Id;
        _rawId = sale.Id.ToString();
    }

    [Given("que existe uma venda já cancelada cadastrada no sistema")]
    public async Task GivenExisteUmaVendaJaCanceladaCadastrada()
    {
        var sale = await _driver.CreateSaleAsync(new
        {
            saleNumber = $"BDD-CS-{Guid.NewGuid():N}".Substring(0, 20),
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[]
            {
                new { productExternalId = "prod-1", productName = "Product 1", quantity = 1, unitPrice = 5m }
            }
        });

        _scenarioContext["SaleId"] = sale.Id;
        _rawId = sale.Id.ToString();

        // Cancel it so it is already in Cancelled state
        await _driver.SendCancelSaleAsync(_rawId);
    }

    [Given("que o ID de cancelamento informado é inválido e não é um UUID")]
    public void GivenIdInvalidoParaCancelamento()
    {
        _rawId = "id-invalido";
    }

    [Given("que não existe nenhuma venda com o ID informado para cancelamento")]
    public void GivenVendaInexistenteParaCancelamento()
    {
        _rawId = Guid.NewGuid().ToString();
    }

    // -------------------------------------------------------------------------
    // When — Act
    // -------------------------------------------------------------------------

    [When("eu cancelar a venda pelo ID cadastrado")]
    public async Task WhenCancelarVendaPeloIdCadastrado()
    {
        await _driver.SendCancelSaleAsync(_rawId);
    }

    [When("eu cancelar a venda pelo ID inválido")]
    public async Task WhenCancelarVendaPeloIdInvalido()
    {
        await _driver.SendCancelSaleAsync(_rawId);
    }

    [When("eu cancelar a venda pelo ID inexistente")]
    public async Task WhenCancelarVendaPeloIdInexistente()
    {
        await _driver.SendCancelSaleAsync(_rawId);
    }

    // -------------------------------------------------------------------------
    // Then — Assert (DB)
    // -------------------------------------------------------------------------

    [Then("a venda cancelada deve existir no banco com todos os itens cancelados")]
    public async Task ThenVendaCanceladaNosBancoComTodosItens()
    {
        var saleId = (Guid)_scenarioContext["SaleId"];

        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var dbSale = allSales.First(s => s.Id.Value == saleId);

        dbSale.Status.Should().Be(SaleStatus.Cancelled);
        dbSale.Items.Should().NotBeEmpty();
        dbSale.Items.Should().AllSatisfy(i => i.IsCancelled.Should().BeTrue());
    }

    [Then("o TotalAmount da venda cancelada no banco deve ser zero")]
    public async Task ThenTotalAmountDaVendaCanceladaNoBancoDeveSerZero()
    {
        var saleId = (Guid)_scenarioContext["SaleId"];

        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var dbSale = allSales.First(s => s.Id.Value == saleId);

        dbSale.TotalAmount.Value.Should().Be(0m);
    }
}
