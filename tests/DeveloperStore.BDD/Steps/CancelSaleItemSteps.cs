using DeveloperStore.BDD.Hooks;
using DeveloperStore.BDD.Infrastructure;
using DeveloperStore.Domain.Enums;
using DeveloperStore.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace DeveloperStore.BDD.Steps;

[Binding]
public sealed class CancelSaleItemSteps
{
    private readonly SalesApiDriver _driver;
    private readonly BddWebApplicationFactory _factory;
    private readonly ScenarioContext _scenarioContext;

    private Guid _saleId;
    private string _rawSaleId = string.Empty;
    private string _rawItemId = string.Empty;

    public CancelSaleItemSteps(SalesApiDriver driver, BddWebApplicationFactory factory, ScenarioContext scenarioContext)
    {
        _driver = driver;
        _factory = factory;
        _scenarioContext = scenarioContext;
    }

    // -------------------------------------------------------------------------
    // Given — Arrange
    // -------------------------------------------------------------------------

    [Given("que existe uma venda ativa com dois itens cadastrada no sistema")]
    public async Task GivenVendaComDoisItens()
    {
        var sale = await _driver.CreateSaleAsync(new
        {
            saleNumber = $"BDD-CI-{Guid.NewGuid():N}".Substring(0, 20),
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[]
            {
                new { productExternalId = "prod-1", productName = "Product 1", quantity = 2, unitPrice = 10m },
                new { productExternalId = "prod-2", productName = "Product 2", quantity = 3, unitPrice = 5m }
            }
        });

        _saleId = sale.Id;
        _scenarioContext["SaleId"] = sale.Id;
        _rawSaleId = sale.Id.ToString();
        _rawItemId = sale.Items.First().Id.ToString();
    }

    [Given("que existe uma venda ativa com um único item cadastrada no sistema")]
    public async Task GivenVendaComUmUnicoItem()
    {
        var sale = await _driver.CreateSaleAsync(new
        {
            saleNumber = $"BDD-CI-{Guid.NewGuid():N}".Substring(0, 20),
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[]
            {
                new { productExternalId = "prod-1", productName = "Product 1", quantity = 1, unitPrice = 20m }
            }
        });

        _saleId = sale.Id;
        _scenarioContext["SaleId"] = sale.Id;
        _rawSaleId = sale.Id.ToString();
        _rawItemId = sale.Items.First().Id.ToString();
    }

    [Given("que existe uma venda ativa com um item já cancelado")]
    public async Task GivenVendaComItemJaCancelado()
    {
        var sale = await _driver.CreateSaleAsync(new
        {
            saleNumber = $"BDD-CI-{Guid.NewGuid():N}".Substring(0, 20),
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[]
            {
                new { productExternalId = "prod-1", productName = "Product 1", quantity = 2, unitPrice = 10m },
                new { productExternalId = "prod-2", productName = "Product 2", quantity = 1, unitPrice = 5m }
            }
        });

        _saleId = sale.Id;
        _scenarioContext["SaleId"] = sale.Id;
        _rawSaleId = sale.Id.ToString();

        // Cancel the first item so it is already in cancelled state
        var firstItem = sale.Items.First();
        _rawItemId = firstItem.Id.ToString();
        await _driver.SendCancelItemAsync(_rawSaleId, _rawItemId);
    }

    [Given("que existe uma venda totalmente cancelada para cancelamento de item")]
    public async Task GivenVendaTotalmenteCancelada()
    {
        var sale = await _driver.CreateSaleAsync(new
        {
            saleNumber = $"BDD-CI-{Guid.NewGuid():N}".Substring(0, 20),
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[]
            {
                new { productExternalId = "prod-1", productName = "Product 1", quantity = 1, unitPrice = 10m }
            }
        });

        _saleId = sale.Id;
        _scenarioContext["SaleId"] = sale.Id;
        _rawSaleId = sale.Id.ToString();
        _rawItemId = sale.Items.First().Id.ToString();

        // Cancel the whole sale first
        await _driver.SendCancelSaleAsync(_rawSaleId);
    }

    [Given("que o saleId informado para cancelamento de item é inválido e não é um UUID")]
    public void GivenSaleIdInvalidoParaCancelamentoDeItem()
    {
        _rawSaleId = "saleId-invalido";
        _rawItemId = Guid.NewGuid().ToString();
    }

    [Given("que existe uma venda ativa cadastrada no sistema para cancelamento de item")]
    public async Task GivenVendaAtivaParaCancelamentoDeItem()
    {
        var sale = await _driver.CreateSaleAsync(new
        {
            saleNumber = $"BDD-CI-{Guid.NewGuid():N}".Substring(0, 20),
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[]
            {
                new { productExternalId = "prod-1", productName = "Product 1", quantity = 1, unitPrice = 10m }
            }
        });

        _saleId = sale.Id;
        _scenarioContext["SaleId"] = sale.Id;
        _rawSaleId = sale.Id.ToString();
        // _rawItemId intentionally left empty — set per When step as needed
    }

    [Given("que não existe nenhuma venda com o saleId informado para cancelamento de item")]
    public void GivenSaleIdInexistenteParaCancelamentoDeItem()
    {
        _rawSaleId = Guid.NewGuid().ToString();
        _rawItemId = Guid.NewGuid().ToString();
    }

    // -------------------------------------------------------------------------
    // When — Act
    // -------------------------------------------------------------------------

    [When("eu cancelar o primeiro item da venda")]
    public async Task WhenCancelarPrimeiroItemDaVenda()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, _rawItemId);
    }

    [When("eu cancelar o único item da venda")]
    public async Task WhenCancelarUnicoItemDaVenda()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, _rawItemId);
    }

    [When("eu cancelar novamente o item já cancelado")]
    public async Task WhenCancelarNovamenteItemJaCancelado()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, _rawItemId);
    }

    [When("eu tentar cancelar um item dessa venda cancelada")]
    public async Task WhenTentarCancelarItemEmVendaCancelada()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, _rawItemId);
    }

    [When("eu tentar cancelar um item com o saleId inválido")]
    public async Task WhenTentarCancelarItemComSaleIdInvalido()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, _rawItemId);
    }

    [When("eu tentar cancelar um item com itemId inválido dessa venda")]
    public async Task WhenTentarCancelarItemComItemIdInvalido()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, "itemId-invalido");
    }

    [When("eu tentar cancelar um item com o saleId inexistente")]
    public async Task WhenTentarCancelarItemComSaleIdInexistente()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, _rawItemId);
    }

    [When("eu tentar cancelar um item com itemId inexistente dessa venda")]
    public async Task WhenTentarCancelarItemComItemIdInexistente()
    {
        await _driver.SendCancelItemAsync(_rawSaleId, Guid.NewGuid().ToString());
    }

    // -------------------------------------------------------------------------
    // Then — Assert (HTTP)
    // -------------------------------------------------------------------------

    [Then("o item cancelado deve estar marcado como cancelado na resposta")]
    public void ThenItemCanceladoNaResposta()
    {
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.Items.Should().Contain(i => i.IsCancelled);
    }

    [Then("a venda deve permanecer com Status NotCancelled")]
    public void ThenVendaPermaneceNotCancelled()
    {
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.Status.Should().Be(SaleStatus.NotCancelled);
    }

    // -------------------------------------------------------------------------
    // Then — Assert (DB)
    // -------------------------------------------------------------------------

    [Then("o TotalAmount da venda no banco deve refletir apenas o item não cancelado")]
    public async Task ThenTotalAmountNosBancoRefleteSomenteItemNaoCancelado()
    {
        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var dbSale = allSales.First(s => s.Id.Value == _saleId);

        var activeItems = dbSale.Items.Where(i => !i.IsCancelled).ToList();
        activeItems.Should().NotBeEmpty();

        var expectedTotal = activeItems.Sum(i => i.TotalAmount.Value);
        dbSale.TotalAmount.Value.Should().Be(expectedTotal);

        // HTTP response must agree
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.TotalAmount.Should().Be(expectedTotal);
    }

    [Then("a venda com item cancelado deve existir no banco com todos os itens cancelados")]
    public async Task ThenVendaComItemCanceladoNoBancoComTodosItens()
    {
        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var dbSale = allSales.First(s => s.Id.Value == _saleId);

        dbSale.Status.Should().Be(SaleStatus.Cancelled);
        dbSale.Items.Should().NotBeEmpty();
        dbSale.Items.Should().AllSatisfy(i => i.IsCancelled.Should().BeTrue());
    }
}
