using System.Globalization;
using DeveloperStore.Application.Sales.Common;
using DeveloperStore.BDD.Hooks;
using DeveloperStore.BDD.Infrastructure;
using DeveloperStore.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace DeveloperStore.BDD.Steps;

[Binding]
public sealed class UpdateSaleSteps
{
    private readonly SalesApiDriver _driver;
    private readonly BddWebApplicationFactory _factory;

    private SaleDto? _firstSale;
    private SaleDto? _secondSale;

    private string _updatedCustomerName = string.Empty;
    private string _updatedSaleNumber = string.Empty;
    private decimal _expectedTotalAmount;

    private string _rawInvalidId = string.Empty;
    private Guid _nonExistentId;

    public UpdateSaleSteps(SalesApiDriver driver, BddWebApplicationFactory factory)
    {
        _driver = driver;
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static object BuildDefaultUpdateRequest(
        string saleNumber,
        string customerName = "Cliente Atualizado",
        string branchName = "Filial Norte") =>
        new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-UPD-001",
            customerName,
            branchExternalId = "BRANCH-UPD-001",
            branchName,
            items = new[]
            {
                new
                {
                    productExternalId = "PROD-UPD-001",
                    productName = "Produto Atualizado",
                    quantity = 3,
                    unitPrice = 40.00m,
                    discountPercentage = 0.0m
                }
            }
        };

    private static object BuildCreateRequest(string saleNumber) =>
        new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente Original",
            branchExternalId = "BRANCH-001",
            branchName = "Filial Centro",
            items = new[]
            {
                new
                {
                    productExternalId = "PROD-001",
                    productName = "Produto Alpha",
                    quantity = 2,
                    unitPrice = 50.00m,
                    discountPercentage = 0.0m
                }
            }
        };

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Atualizar venda existente com novos dados retorna 200
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existe uma venda cadastrada para atualização")]
    public async Task DadoQueExisteUmaVendaCadastradaParaAtualizacao()
    {
        var saleNumber = $"SALE-UPD-{Guid.NewGuid():N}"[..20];
        _firstSale = await _driver.CreateSaleAsync(BuildCreateRequest(saleNumber));
    }

    [When(@"eu atualizar a venda com novos dados de cliente e filial")]
    public async Task QuandoEuAtualizarAVendaComNovosDadosDeClienteEFilial()
    {
        _updatedCustomerName = "Cliente Atualizado";
        _updatedSaleNumber = $"SALE-NEW-{Guid.NewGuid():N}"[..20];

        // 3 items × R$40 = R$120
        _expectedTotalAmount = 3 * 40.00m;

        await _driver.SendUpdateSaleAsync(
            _firstSale!.Id,
            BuildDefaultUpdateRequest(_updatedSaleNumber, _updatedCustomerName));
    }

    [Then(@"os dados da venda atualizada devem refletir as novas informações")]
    public void EntaoOsDadosDaVendaAtualizadaDevemRefletirAsNovasInformacoes()
    {
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.Id.Should().Be(_firstSale!.Id);
        _driver.LastSale.SaleNumber.Should().Be(_updatedSaleNumber);
        _driver.LastSale.CustomerName.Should().Be(_updatedCustomerName);
        _driver.LastSale.TotalAmount.Should().Be(_expectedTotalAmount);
    }

    [Then(@"os novos dados devem estar persistidos no banco de dados")]
    public async Task EntaoOsNovosDadosDevemEstarPersistidosNoBancoDeDados()
    {
        await using var db = _factory.CreateTestDbContext();

        var guid = _firstSale!.Id;
        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var saleInDb = allSales.FirstOrDefault(s => s.Id.Value == guid);

        saleInDb.Should().NotBeNull();
        saleInDb!.SaleNumber.Value.Should().Be(_updatedSaleNumber);
        saleInDb.Customer.Description.Should().Be(_updatedCustomerName);
        saleInDb.TotalAmount.Value.Should().Be(_expectedTotalAmount);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Atualizar venda mantendo o mesmo saleNumber retorna 200
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existe uma venda cadastrada para manter o mesmo número")]
    public async Task DadoQueExisteUmaVendaCadastradaParaManteroMesmoNumero()
    {
        var saleNumber = $"SALE-SAME-{Guid.NewGuid():N}"[..20];
        _firstSale = await _driver.CreateSaleAsync(BuildCreateRequest(saleNumber));
    }

    [When(@"eu atualizar a venda mantendo o mesmo saleNumber")]
    public async Task QuandoEuAtualizarAVendaMantendoOMesmoSaleNumber()
    {
        _updatedSaleNumber = _firstSale!.SaleNumber;
        await _driver.SendUpdateSaleAsync(_firstSale.Id, BuildDefaultUpdateRequest(_updatedSaleNumber));
    }

    [Then(@"os dados da venda retornada devem conter o mesmo saleNumber")]
    public void EntaoOsDadosDaVendaRetornadaDevemConterOMesmoSaleNumber()
    {
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.SaleNumber.Should().Be(_updatedSaleNumber);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Atualizar venda com ID inválido retorna 422
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que o ID da venda a ser atualizada é inválido")]
    public void DadoQueOIdDaVendaASerAtualizadaEInvalido()
    {
        _rawInvalidId = "id-invalido-nao-uuid";
    }

    [When(@"eu tentar atualizar a venda com ID inválido")]
    public async Task QuandoEuTentarAtualizarAVendaComIdInvalido()
    {
        var saleNumber = $"SALE-INVID-{Guid.NewGuid():N}"[..20];
        await _driver.SendUpdateSaleAsync(_rawInvalidId, BuildDefaultUpdateRequest(saleNumber));
    }

    [Then(@"o código do erro de atualização deve ser ""(.+)""")]
    public void EntaoOCodigoDoErroDeAtualizacaoDeveSer(string expectedCode)
    {
        _driver.LastError.Should().NotBeNull();
        _driver.LastError!.Errors.Should().NotBeNullOrEmpty();
        _driver.LastError.Errors!.Should().Contain(e => e.Code == expectedCode);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Atualizar venda com ID inexistente retorna 404
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que não existe venda com o ID informado para atualização")]
    public void DadoQueNaoExisteVendaComOIdInformadoParaAtualizacao()
    {
        _nonExistentId = Guid.NewGuid();
    }

    [When(@"eu tentar atualizar a venda inexistente")]
    public async Task QuandoEuTentarAtualizarAVendaInexistente()
    {
        var saleNumber = $"SALE-NX-{Guid.NewGuid():N}"[..20];
        await _driver.SendUpdateSaleAsync(_nonExistentId, BuildDefaultUpdateRequest(saleNumber));
    }

    [Then(@"o tipo do erro de atualização deve ser ""(.+)""")]
    public void EntaoOTipoDoErroDeAtualizacaoDeveSer(string expectedType)
    {
        _driver.LastError.Should().NotBeNull();
        _driver.LastError!.Type.Should().Be(expectedType);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Alterar saleNumber para número já existente retorna 409
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existem duas vendas cadastradas com números distintos")]
    public async Task DadoQueExistemDuasVendasCadastradasComNumeroDistintos()
    {
        var saleNumber1 = $"SALE-DUP1-{Guid.NewGuid():N}"[..20];
        var saleNumber2 = $"SALE-DUP2-{Guid.NewGuid():N}"[..20];

        _firstSale = await _driver.CreateSaleAsync(BuildCreateRequest(saleNumber1));
        _secondSale = await _driver.CreateSaleAsync(BuildCreateRequest(saleNumber2));
    }

    [When(@"eu tentar atualizar a primeira venda com o saleNumber da segunda venda")]
    public async Task QuandoEuTentarAtualizarAPrimeiraVendaComOSaleNumberDaSegundaVenda()
    {
        await _driver.SendUpdateSaleAsync(_firstSale!.Id, BuildDefaultUpdateRequest(_secondSale!.SaleNumber));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Atualizar venda cancelada retorna 409
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existe uma venda cadastrada e ela foi cancelada")]
    public async Task DadoQueExisteUmaVendaCadastradaEElaFoiCancelada()
    {
        var saleNumber = $"SALE-CAN-{Guid.NewGuid():N}"[..20];
        _firstSale = await _driver.CreateSaleAsync(BuildCreateRequest(saleNumber));

        await _driver.SendCancelSaleAsync(_firstSale.Id.ToString());
        _driver.LastResponse!.IsSuccessStatusCode.Should().BeTrue(
            because: "o cancelamento da venda deve ser bem-sucedido antes de tentar atualizá-la");
    }

    [When(@"eu tentar atualizar a venda cancelada")]
    public async Task QuandoEuTentarAtualizarAVendaCancelada()
    {
        var saleNumber = $"SALE-UPDC-{Guid.NewGuid():N}"[..20];
        await _driver.SendUpdateSaleAsync(_firstSale!.Id, BuildDefaultUpdateRequest(saleNumber));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Shared step: verify HTTP status code for update scenarios
    // ──────────────────────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Atualizar quantidade de item recalcula desconto e total
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existe uma venda cadastrada com um item de quantidade (\d+) e preço ([\d.,]+)")]
    public async Task DadoQueExisteUmaVendaCadastradaComUmItemDeQuantidadeEPreco(int quantity, string priceRaw)
    {
        var price = decimal.Parse(priceRaw, CultureInfo.InvariantCulture);
        var saleNumber = $"SALE-QTY-{Guid.NewGuid():N}"[..20];
        _firstSale = await _driver.CreateSaleAsync(new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente Original",
            branchExternalId = "BRANCH-001",
            branchName = "Filial Centro",
            items = new[]
            {
                new { productExternalId = "PROD-001", productName = "Produto Alpha", quantity, unitPrice = price, discountPercentage = 0.0m }
            }
        });
    }

    [When(@"eu atualizar a venda com quantidade (\d+) do mesmo item")]
    public async Task QuandoEuAtualizarAVendaComQuantidadeDoMesmoItem(int quantity)
    {
        _expectedTotalAmount = quantity * 10.00m * (quantity is >= 10 and <= 20 ? 0.8m : quantity is >= 4 and <= 9 ? 0.9m : 1.0m);

        await _driver.SendUpdateSaleAsync(
            _firstSale!.Id,
            new
            {
                saleNumber = _firstSale.SaleNumber,
                soldAt = DateTimeOffset.UtcNow,
                customerExternalId = "CUST-001",
                customerName = "Cliente Original",
                branchExternalId = "BRANCH-001",
                branchName = "Filial Centro",
                items = new[]
                {
                    new { productExternalId = "PROD-001", productName = "Produto Alpha", quantity, unitPrice = 10.00m, discountPercentage = 0.0m }
                }
            });
    }

    [Then(@"o totalAmount da venda retornada deve ser ([\d.,]+)")]
    public void EntaoOTotalAmountDaVendaRetornadaDeveSer(string expectedTotalRaw)
    {
        var expectedTotal = decimal.Parse(expectedTotalRaw, CultureInfo.InvariantCulture);
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.TotalAmount.Should().Be(expectedTotal);
    }

    [Then(@"o desconto do item na resposta deve ser (\d+)%")]
    public void EntaoODescontoDoItemNaRespostaDeveSer(int expectedDiscount)
    {
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.Items.Should().ContainSingle();
        _driver.LastSale.Items.Single().DiscountPercentage.Should().Be(expectedDiscount / 100m);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Atualizar venda adicionando novo item atualiza total
    // ──────────────────────────────────────────────────────────────────────────

    [When(@"eu atualizar a venda incluindo um segundo item de quantidade (\d+) e preço ([\d.,]+)")]
    public async Task QuandoEuAtualizarAVendaIncluindoUmSegundoItem(int quantity, string priceRaw)
    {
        var price = decimal.Parse(priceRaw, CultureInfo.InvariantCulture);
        var secondItemTotal = quantity * price * (quantity is >= 10 and <= 20 ? 0.8m : quantity is >= 4 and <= 9 ? 0.9m : 1.0m);
        _expectedTotalAmount = 1 * 10.00m + secondItemTotal;

        await _driver.SendUpdateSaleAsync(
            _firstSale!.Id,
            new
            {
                saleNumber = _firstSale.SaleNumber,
                soldAt = DateTimeOffset.UtcNow,
                customerExternalId = "CUST-001",
                customerName = "Cliente Original",
                branchExternalId = "BRANCH-001",
                branchName = "Filial Centro",
                items = new[]
                {
                    new { productExternalId = "PROD-001", productName = "Produto Alpha", quantity = 1, unitPrice = 10.00m, discountPercentage = 0.0m },
                    new { productExternalId = "PROD-002", productName = "Produto Beta", quantity, unitPrice = price, discountPercentage = 0.0m }
                }
            });
    }

    [Then(@"a venda no banco deve conter (\d+) itens")]
    public async Task EntaoAVendaNoBancoDeveConterItens(int expectedItemCount)
    {
        await using var db = _factory.CreateTestDbContext();

        var guid = _firstSale!.Id;
        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var saleInDb = allSales.FirstOrDefault(s => s.Id.Value == guid);

        saleInDb.Should().NotBeNull();
        saleInDb!.Items.Should().HaveCount(expectedItemCount);
    }

    [Then(@"a resposta de atualização deve ter status (\d+)")]
    public void EntaoARespostaDeAtualizacaoDeveTerStatus(int expectedStatus)
    {
        _driver.LastResponse.Should().NotBeNull();
        ((int)_driver.LastResponse!.StatusCode).Should().Be(expectedStatus);
    }
}
