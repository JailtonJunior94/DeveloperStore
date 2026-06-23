using DeveloperStore.Application.Sales.Common;
using DeveloperStore.BDD.Hooks;
using DeveloperStore.BDD.Infrastructure;
using DeveloperStore.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace DeveloperStore.BDD.Steps;

[Binding]
public sealed class GetSaleSteps
{
    private readonly SalesApiDriver _driver;
    private readonly BddWebApplicationFactory _factory;

    private SaleDto? _createdSale;
    private Guid _targetId;
    private string _rawId = string.Empty;

    public GetSaleSteps(SalesApiDriver driver, BddWebApplicationFactory factory)
    {
        _driver = driver;
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Buscar venda existente pelo ID retorna 200 com dados corretos
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existe uma venda cadastrada no sistema")]
    public async Task DadoQueExisteUmaVendaCadastradaNoSistema()
    {
        var saleNumber = $"SALE-GET-{Guid.NewGuid():N}"[..20];

        _createdSale = await _driver.CreateSaleAsync(new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-001",
            customerName = "Cliente Teste",
            branchExternalId = "BRANCH-001",
            branchName = "Filial Centro",
            items = new[]
            {
                new
                {
                    productExternalId = "PROD-001",
                    productName = "Produto Alpha",
                    quantity = 2,
                    unitPrice = 50.00m
                }
            }
        });

        _targetId = _createdSale.Id;
    }

    [When(@"eu buscar a venda pelo ID cadastrado")]
    public async Task QuandoEuBuscarAVendaPeloIdCadastrado()
    {
        await _driver.SendGetSaleAsync(_targetId.ToString());
    }

    [Then(@"os dados da venda retornados devem corresponder à venda cadastrada")]
    public void EntaoOsDadosDaVendaRetornadosDevemCorresponderAVendaCadastrada()
    {
        _driver.LastSale.Should().NotBeNull();
        _driver.LastSale!.Id.Should().Be(_createdSale!.Id);
        _driver.LastSale.SaleNumber.Should().Be(_createdSale.SaleNumber);
        _driver.LastSale.CustomerName.Should().Be(_createdSale.CustomerName);
        _driver.LastSale.BranchName.Should().Be(_createdSale.BranchName);
        _driver.LastSale.TotalAmount.Should().Be(_createdSale.TotalAmount);
        _driver.LastSale.Status.Should().Be(_createdSale.Status);
        _driver.LastSale.Items.Should().HaveCount(_createdSale.Items.Count);
    }

    [Then(@"a venda deve existir no banco de dados com os dados corretos")]
    public async Task EntaoAVendaDeveExistirNoBancoDeDadosComOsDadosCorretos()
    {
        await using var db = _factory.CreateTestDbContext();

        var allSales = await db.Sales.AsNoTracking().Include(s => s.Items).ToListAsync();
        var saleInDb = allSales.FirstOrDefault(s => s.Id.Value == _targetId);

        saleInDb.Should().NotBeNull();
        saleInDb!.SaleNumber.Value.Should().Be(_createdSale!.SaleNumber);
        saleInDb.Customer.Description.Should().Be(_createdSale.CustomerName);
        saleInDb.Branch.Description.Should().Be(_createdSale.BranchName);
        saleInDb.Items.Should().HaveCount(_createdSale.Items.Count);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Buscar venda com ID inexistente retorna 404
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que não existe nenhuma venda com o ID informado")]
    public void DadoQueNaoExisteNenhumaVendaComOIdInformado()
    {
        _targetId = Guid.NewGuid();
    }

    [When(@"eu buscar a venda pelo ID inexistente")]
    public async Task QuandoEuBuscarAVendaPeloIdInexistente()
    {
        await _driver.SendGetSaleAsync(_targetId.ToString());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Buscar venda com ID inválido retorna 422
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que o ID informado é inválido e não é um UUID")]
    public void DadoQueOIdInformadoEInvalidoENaoEUmUuid()
    {
        _rawId = "id-invalido-nao-uuid";
    }

    [When(@"eu buscar a venda pelo ID inválido")]
    public async Task QuandoEuBuscarAVendaPeloIdInvalido()
    {
        await _driver.SendGetSaleAsync(_rawId);
    }
}
