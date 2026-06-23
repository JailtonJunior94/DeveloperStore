using System.Globalization;
using System.Net;
using DeveloperStore.BDD.Hooks;
using DeveloperStore.BDD.Infrastructure;
using DeveloperStore.Common.Validation;
using DeveloperStore.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace DeveloperStore.BDD.Steps;

[Binding]
public sealed class CreateSaleSteps
{
    private readonly SalesApiDriver _driver;
    private readonly BddWebApplicationFactory _factory;
    private object? _request;

    public CreateSaleSteps(SalesApiDriver driver, BddWebApplicationFactory factory)
    {
        _driver = driver;
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // Given — Arrange
    // -------------------------------------------------------------------------

    [Given("dados válidos de venda com um item")]
    public void GivenValidSaleWithOneItem()
    {
        _request = new
        {
            saleNumber = $"BDD-CREATE-{Guid.NewGuid().ToString()[..8]}",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new
                {
                    productExternalId = "PROD-001",
                    productName = "Produto BDD",
                    quantity = 2,
                    unitPrice = 50.00m
                }
            }
        };
    }

    [Given("dados válidos de venda com múltiplos itens")]
    public void GivenValidSaleWithMultipleItems()
    {
        _request = new
        {
            saleNumber = $"BDD-MULTI-{Guid.NewGuid().ToString()[..8]}",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-002",
            customerName = "Cliente BDD Multi",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new { productExternalId = "PROD-001", productName = "Produto A", quantity = 3, unitPrice = 10.00m },
                new { productExternalId = "PROD-002", productName = "Produto B", quantity = 2, unitPrice = 25.00m },
                new { productExternalId = "PROD-003", productName = "Produto C", quantity = 1, unitPrice = 5.50m }
            }
        };
    }

    [Given("uma requisição de criação de venda sem o campo {string}")]
    public void GivenRequestMissingField(string campo)
    {
        _request = campo switch
        {
            "saleNumber" => new
            {
                saleNumber = "",
                soldAt = DateTimeOffset.UtcNow,
                customerExternalId = "CUST-001",
                customerName = "Cliente BDD",
                branchExternalId = "BRANCH-001",
                branchName = "Filial BDD",
                items = new[] { new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m } }
            },
            "soldAt" => (object)new
            {
                saleNumber = $"BDD-MISS-{Guid.NewGuid().ToString()[..8]}",
                soldAt = default(DateTimeOffset),
                customerExternalId = "CUST-001",
                customerName = "Cliente BDD",
                branchExternalId = "BRANCH-001",
                branchName = "Filial BDD",
                items = new[] { new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m } }
            },
            "customerExternalId" => new
            {
                saleNumber = $"BDD-MISS-{Guid.NewGuid().ToString()[..8]}",
                soldAt = DateTimeOffset.UtcNow,
                customerExternalId = "",
                customerName = "Cliente BDD",
                branchExternalId = "BRANCH-001",
                branchName = "Filial BDD",
                items = new[] { new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m } }
            },
            "customerName" => new
            {
                saleNumber = $"BDD-MISS-{Guid.NewGuid().ToString()[..8]}",
                soldAt = DateTimeOffset.UtcNow,
                customerExternalId = "CUST-001",
                customerName = "",
                branchExternalId = "BRANCH-001",
                branchName = "Filial BDD",
                items = new[] { new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m } }
            },
            "branchExternalId" => new
            {
                saleNumber = $"BDD-MISS-{Guid.NewGuid().ToString()[..8]}",
                soldAt = DateTimeOffset.UtcNow,
                customerExternalId = "CUST-001",
                customerName = "Cliente BDD",
                branchExternalId = "",
                branchName = "Filial BDD",
                items = new[] { new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m } }
            },
            "branchName" => new
            {
                saleNumber = $"BDD-MISS-{Guid.NewGuid().ToString()[..8]}",
                soldAt = DateTimeOffset.UtcNow,
                customerExternalId = "CUST-001",
                customerName = "Cliente BDD",
                branchExternalId = "BRANCH-001",
                branchName = "",
                items = new[] { new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m } }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(campo), $"Campo não mapeado: {campo}")
        };
    }

    [Given("dados válidos de venda sem itens")]
    public void GivenValidSaleWithNoItems()
    {
        _request = new
        {
            saleNumber = $"BDD-NOITEM-{Guid.NewGuid().ToString()[..8]}",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = Array.Empty<object>()
        };
    }

    [Given("dados válidos de venda com um item de quantidade {int}")]
    public void GivenSaleWithItemQuantity(int quantidade)
    {
        _request = new
        {
            saleNumber = $"BDD-QTY-{Guid.NewGuid().ToString()[..8]}",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = quantidade, unitPrice = 10.00m }
            }
        };
    }

    [Given("dados válidos de venda com um item com preço unitário zero")]
    public void GivenSaleWithZeroUnitPrice()
    {
        _request = new
        {
            saleNumber = $"BDD-PRICE-{Guid.NewGuid().ToString()[..8]}",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 0.00m }
            }
        };
    }

    [Given("uma venda com o número {string} já existe no banco")]
    public async Task GivenSaleNumberAlreadyExists(string saleNumber)
    {
        await _driver.CreateSaleAsync(new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m }
            }
        });
    }

    [Given(@"dados válidos de venda com um item de quantidade (\d+) e preço ([\d.,]+)")]
    public void GivenSaleWithItemQuantityAndPrice(int quantidade, string precoRaw)
    {
        var preco = decimal.Parse(precoRaw, CultureInfo.InvariantCulture);
        _request = new
        {
            saleNumber = $"BDD-TOTAL-{Guid.NewGuid().ToString()[..8]}",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = quantidade, unitPrice = preco }
            }
        };
    }

    [Given("dados de venda com dois itens com o mesmo productExternalId")]
    public void GivenSaleWithDuplicateProductExternalId()
    {
        _request = new
        {
            saleNumber = $"BDD-DUP-PROD-{Guid.NewGuid().ToString()[..8]}",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new { productExternalId = "PROD-SAME", productName = "Produto Duplicado", quantity = 1, unitPrice = 10.00m },
                new { productExternalId = "PROD-SAME", productName = "Produto Duplicado", quantity = 2, unitPrice = 10.00m }
            }
        };
    }

    // -------------------------------------------------------------------------
    // When — Act
    // -------------------------------------------------------------------------

    [When("envio POST para \\/api\\/sales")]
    public async Task WhenPostToApiSales()
    {
        await _driver.SendCreateSaleAsync(_request!);
    }

    [When("envio POST para \\/api\\/sales com o número {string}")]
    public async Task WhenPostToApiSalesWithNumber(string saleNumber)
    {
        _request = new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente BDD",
            branchExternalId = "BRANCH-001",
            branchName = "Filial BDD",
            items = new[]
            {
                new { productExternalId = "PROD-001", productName = "Produto BDD", quantity = 1, unitPrice = 10.00m }
            }
        };
        await _driver.SendCreateSaleAsync(_request);
    }

    // -------------------------------------------------------------------------
    // Then — Assert
    // -------------------------------------------------------------------------

    [Then("recebo status 201 Created")]
    public void ThenStatus201()
    {
        _driver.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Then("recebo status 409 Conflict")]
    public void ThenStatus409()
    {
        _driver.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Then("recebo status 422 Unprocessable Entity")]
    public void ThenStatus422()
    {
        _driver.LastResponse!.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Then("a venda está persistida no banco com os dados corretos")]
    public async Task ThenSalePersistedInDatabase()
    {
        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var guid = _driver.LastSale!.Id;
        var sale = allSales.FirstOrDefault(s => s.Id.Value == guid);

        sale.Should().NotBeNull();
        sale!.Items.Should().HaveCount(1);
    }

    [Then("o totalAmount da resposta é igual à soma dos itens")]
    public async Task ThenTotalAmountEqualsSumOfItems()
    {
        _driver.LastSale.Should().NotBeNull();

        var expectedTotal = _driver.LastSale!.Items.Sum(i => i.TotalAmount);
        _driver.LastSale.TotalAmount.Should().Be(expectedTotal);

        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var guid = _driver.LastSale.Id;
        var sale = allSales.FirstOrDefault(s => s.Id.Value == guid);

        sale.Should().NotBeNull();
        sale!.Items.Should().HaveCount(3);
    }

    [Then("o código de erro {string} está presente na resposta")]
    public void ThenErrorCodeIsPresent(string codigoErro)
    {
        _driver.LastError.Should().NotBeNull();

        var hasErrorInType = _driver.LastError!.Type == codigoErro
                             || _driver.LastError.Error == codigoErro;

        var hasErrorInDetails = _driver.LastError.Errors is not null
                                && _driver.LastError.Errors.Any(e => e.Code == codigoErro);

        (hasErrorInType || hasErrorInDetails)
            .Should().BeTrue(
                because: $"o código de erro '{codigoErro}' deve estar presente em Type, Error ou Errors da resposta");
    }

    [Then("existe apenas uma venda com número {string} no banco")]
    public async Task ThenOnlyOneSaleWithNumberInDatabase(string saleNumber)
    {
        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().ToListAsync();
        var count = allSales.Count(s => s.SaleNumber.Value == saleNumber);

        count.Should().Be(1, because: $"apenas uma venda com o número '{saleNumber}' deve existir no banco");
    }

    [Then(@"o totalAmount do item na resposta é ([\d.,]+)")]
    public void ThenItemTotalAmountIs(string totalEsperadoRaw)
    {
        var totalEsperado = decimal.Parse(totalEsperadoRaw, CultureInfo.InvariantCulture);
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.Items.Should().HaveCount(1);

        var item = _driver.LastSale.Items.Single();
        item.TotalAmount.Should().Be(totalEsperado);
    }
}
