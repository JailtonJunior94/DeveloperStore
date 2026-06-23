using DeveloperStore.Application.Sales.Common;
using DeveloperStore.BDD.Hooks;
using DeveloperStore.BDD.Infrastructure;
using DeveloperStore.Domain.Enums;
using DeveloperStore.ORM;
using DeveloperStore.WebApi.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace DeveloperStore.BDD.Steps;

[Binding]
public sealed class ListSalesSteps
{
    private readonly SalesApiDriver _driver;
    private readonly BddWebApplicationFactory _factory;

    private ApiPagedResponse<SaleSummaryDto>? _lastPagedResponse;
    private string _scenarioPrefix = string.Empty;
    private DateTimeOffset _filterMinDate;
    private DateTimeOffset _filterMaxDate;

    public ListSalesSteps(SalesApiDriver driver, BddWebApplicationFactory factory)
    {
        _driver = driver;
        _factory = factory;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<SaleDto> CreateSaleWithPrefix(
        string prefix,
        string suffix,
        string customerName = "Cliente Padrão",
        string branchName = "Filial Padrão",
        DateTimeOffset? soldAt = null)
    {
        return await _driver.CreateSaleAsync(new
        {
            saleNumber = $"{prefix}-{suffix}",
            soldAt = soldAt ?? DateTimeOffset.UtcNow,
            customerExternalId = $"CUST-{suffix}",
            customerName,
            branchExternalId = $"BRNCH-{suffix}",
            branchName,
            items = new[]
            {
                new
                {
                    productExternalId = $"PROD-{suffix}",
                    productName = $"Produto {suffix}",
                    quantity = 1,
                    unitPrice = 100.00m,
                    discountPercentage = 0.0m
                }
            }
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Listar vendas com paginação padrão
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existem (\d+) vendas cadastradas com prefixo ""([^""]*)""")]
    public async Task DadoQueExistemVendasCadastradasComPrefixo(int count, string prefix)
    {
        _scenarioPrefix = prefix;
        for (var i = 1; i <= count; i++)
        {
            await CreateSaleWithPrefix(prefix, i.ToString("D3"));
        }
    }

    [When(@"eu listar as vendas sem filtros")]
    public async Task QuandoEuListarAsVendasSemFiltros()
    {
        _lastPagedResponse = await _driver.SendListSalesAsync();
    }

    [Then(@"o response deve conter ao menos (\d+) vendas")]
    public void EntaoOResponseDeveConterAoMenosVendas(int minCount)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().HaveCountGreaterThanOrEqualTo(minCount);
    }

    [Then(@"o pageNumber do response deve ser (\d+)")]
    public void EntaoOPageNumberDoResponseDeveSer(int expectedPage)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.PageNumber.Should().Be(expectedPage);
    }

    [Then(@"o pageSize do response deve ser (\d+)")]
    public void EntaoOPageSizeDoResponseDeveSer(int expectedSize)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.PageSize.Should().Be(expectedSize);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Filtrar por número da venda exato
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existem vendas cadastradas com prefixo ""([^""]*)""")]
    public async Task DadoQueExistemVendasCadastradasComPrefixoGenerico(string prefix)
    {
        _scenarioPrefix = prefix;
        await CreateSaleWithPrefix(prefix, "002");
        await CreateSaleWithPrefix(prefix, "003");
    }

    [Given(@"que uma das vendas tem saleNumber ""([^""]*)""")]
    public async Task DadoQueUmaDasVendasTemSaleNumber(string saleNumber)
    {
        await _driver.CreateSaleAsync(new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "CUST-EXACT",
            customerName = "Cliente Exato",
            branchExternalId = "BRNCH-EXACT",
            branchName = "Filial Exata",
            items = new[]
            {
                new
                {
                    productExternalId = "PROD-EXACT",
                    productName = "Produto Exato",
                    quantity = 1,
                    unitPrice = 50.00m,
                    discountPercentage = 0.0m
                }
            }
        });
    }

    [When(@"eu listar as vendas com filtro saleNumber igual a ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComFiltroSaleNumberIgualA(string saleNumber)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync($"?saleNumber={Uri.EscapeDataString(saleNumber)}");
    }

    [Then(@"o response deve conter exatamente (\d+) vendas")]
    public void EntaoOResponseDeveConterExatamenteVendas(int expectedCount)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().HaveCount(expectedCount);
    }

    [Then(@"o response deve conter exatamente (\d+) venda")]
    public void EntaoOResponseDeveConterExatamenteUmaVenda(int expectedCount)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().HaveCount(expectedCount);
    }

    [Then(@"a venda retornada deve ter saleNumber ""([^""]*)""")]
    public void EntaoAVendaRetornadaDeveTerSaleNumber(string expectedSaleNumber)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().ContainSingle(s => s.SaleNumber == expectedSaleNumber);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Filtrar por número da venda com wildcard
    // ──────────────────────────────────────────────────────────────────────────

    [When(@"eu listar as vendas com filtro saleNumber usando wildcard ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComFiltroSaleNumberUsandoWildcard(string pattern)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync($"?saleNumber={Uri.EscapeDataString(pattern)}");
    }

    [Then(@"todas as vendas retornadas devem ter saleNumber iniciando com ""([^""]*)""")]
    public void EntaoTodasAsVendasRetornadasDevemTerSaleNumberIniciandoCom(string prefix)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().AllSatisfy(s =>
            s.SaleNumber.Should().StartWith(prefix));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Filtrar por nome do cliente
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existem (\d+) vendas cadastradas com prefixo ""([^""]*)"" para o cliente ""([^""]*)""")]
    public async Task DadoQueExistemVendasCadastradasComPrefixoParaOCliente(int count, string prefix, string customerName)
    {
        _scenarioPrefix = prefix;
        for (var i = 1; i <= count; i++)
        {
            await CreateSaleWithPrefix(prefix, $"C{i:D2}", customerName: customerName);
        }
    }

    [Given(@"que existe (\d+) venda cadastrada com prefixo ""([^""]*)"" para o cliente ""([^""]*)""")]
    public async Task DadoQueExisteVendaCadastradaComPrefixoParaOCliente(int count, string prefix, string customerName)
    {
        for (var i = 1; i <= count; i++)
        {
            await CreateSaleWithPrefix(prefix, $"D{i:D2}", customerName: customerName);
        }
    }

    [When(@"eu listar as vendas com filtro customerName igual a ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComFiltroCustomerNameIgualA(string customerName)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync($"?customerName={Uri.EscapeDataString(customerName)}");
    }

    [Then(@"todas as vendas retornadas devem pertencer ao cliente ""([^""]*)""")]
    public void EntaoTodasAsVendasRetornadasDevemPertencerAoCliente(string customerName)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().AllSatisfy(s =>
            s.CustomerName.Should().Be(customerName));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Filtrar por nome da filial
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existem (\d+) vendas cadastradas com prefixo ""([^""]*)"" para a filial ""([^""]*)""")]
    public async Task DadoQueExistemVendasCadastradasComPrefixoParaAFilial(int count, string prefix, string branchName)
    {
        _scenarioPrefix = prefix;
        for (var i = 1; i <= count; i++)
        {
            await CreateSaleWithPrefix(prefix, $"B{i:D2}", branchName: branchName);
        }
    }

    [Given(@"que existe (\d+) venda cadastrada com prefixo ""([^""]*)"" para a filial ""([^""]*)""")]
    public async Task DadoQueExisteVendaCadastradaComPrefixoParaAFilial(int count, string prefix, string branchName)
    {
        for (var i = 1; i <= count; i++)
        {
            await CreateSaleWithPrefix(prefix, $"E{i:D2}", branchName: branchName);
        }
    }

    [When(@"eu listar as vendas com filtro branchName igual a ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComFiltroBranchNameIgualA(string branchName)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync($"?branchName={Uri.EscapeDataString(branchName)}");
    }

    [Then(@"todas as vendas retornadas devem pertencer à filial ""([^""]*)""")]
    public void EntaoTodasAsVendasRetornadasDevemPertencerAFilial(string branchName)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().AllSatisfy(s =>
            s.BranchName.Should().Be(branchName));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Filtrar por status Cancelled / NotCancelled
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existem 2 vendas cadastradas com prefixo ""([^""]*)"" e a primeira está cancelada")]
    public async Task DadoQueExistemDuasVendasComPrefixoEAPrimeiraEstaCancelada(string prefix)
    {
        _scenarioPrefix = prefix;

        var first = await CreateSaleWithPrefix(prefix, "001");
        await CreateSaleWithPrefix(prefix, "002");

        await _driver.SendCancelSaleAsync(first.Id.ToString());
    }

    [When(@"eu listar as vendas com filtro status igual a ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComFiltroStatusIgualA(string status)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync($"?status={Uri.EscapeDataString(status)}&saleNumber={Uri.EscapeDataString(_scenarioPrefix + "*")}");
    }

    [Then(@"todas as vendas retornadas devem ter status ""([^""]*)""")]
    public void EntaoTodasAsVendasRetornadasDevemTerStatus(string expectedStatus)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().NotBeEmpty();

        var parsed = Enum.Parse<SaleStatus>(expectedStatus, ignoreCase: true);
        _lastPagedResponse.Data.Should().AllSatisfy(s =>
            s.Status.Should().Be(parsed));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Filtrar por intervalo de datas
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que existem 3 vendas com prefixo ""([^""]*)"" com datas distribuídas em janeiro de 2024")]
    public async Task DadoQueExistemVendasComPrefixoComDatasDistribuidasEmJaneiroDe2024(string prefix)
    {
        _scenarioPrefix = prefix;

        // Venda antes do intervalo
        await CreateSaleWithPrefix(prefix, "001", soldAt: new DateTimeOffset(2024, 1, 5, 12, 0, 0, TimeSpan.Zero));

        // Vendas dentro do intervalo (10-20/jan)
        await CreateSaleWithPrefix(prefix, "002", soldAt: new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
        await CreateSaleWithPrefix(prefix, "003", soldAt: new DateTimeOffset(2024, 1, 18, 12, 0, 0, TimeSpan.Zero));
    }

    [When(@"eu listar as vendas com _minSoldAt ""([^""]*)"" e _maxSoldAt ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComMinSoldAtEMaxSoldAt(string minDate, string maxDate)
    {
        _filterMinDate = DateTimeOffset.Parse(minDate);
        _filterMaxDate = DateTimeOffset.Parse(maxDate);

        var encodedMin = Uri.EscapeDataString(_filterMinDate.ToString("O"));
        var encodedMax = Uri.EscapeDataString(_filterMaxDate.ToString("O"));
        var encodedPrefix = Uri.EscapeDataString(_scenarioPrefix + "*");

        var query = string.IsNullOrEmpty(_scenarioPrefix)
            ? $"?_minSoldAt={encodedMin}&_maxSoldAt={encodedMax}"
            : $"?_minSoldAt={encodedMin}&_maxSoldAt={encodedMax}&saleNumber={encodedPrefix}";

        _lastPagedResponse = await _driver.SendListSalesAsync(query);
    }

    [Then(@"apenas vendas dentro do intervalo de datas devem ser retornadas")]
    public void EntaoApenasVendasDentroDoIntervaloDeDatasDevemSerRetornadas()
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().NotBeEmpty();
        _lastPagedResponse.Data.Should().AllSatisfy(s =>
        {
            s.SoldAt.Should().BeOnOrAfter(_filterMinDate);
            s.SoldAt.Should().BeOnOrBefore(_filterMaxDate);
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Ordenar por número da venda ascendente
    // ──────────────────────────────────────────────────────────────────────────

    [When(@"eu listar as vendas com ordenação ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComOrdenacao(string orderParam)
    {
        var encodedPrefix = Uri.EscapeDataString(_scenarioPrefix + "*");
        var query = string.IsNullOrEmpty(_scenarioPrefix)
            ? $"?{orderParam}"
            : $"?{orderParam}&saleNumber={encodedPrefix}";

        _lastPagedResponse = await _driver.SendListSalesAsync(query);
    }

    [Then(@"os saleNumbers das vendas retornadas devem estar em ordem ascendente")]
    public void EntaoOsSaleNumbersDasVendasRetornadasDevemEstarEmOrdemAscendente()
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.Data.Should().NotBeEmpty();

        var saleNumbers = _lastPagedResponse.Data.Select(s => s.SaleNumber).ToList();
        saleNumbers.Should().BeInAscendingOrder();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Paginação com múltiplas páginas
    // ──────────────────────────────────────────────────────────────────────────

    [When(@"eu listar as vendas com _page=(\d+) e _size=(\d+)")]
    public async Task QuandoEuListarAsVendasComPageESize(int page, int size)
    {
        var encodedPrefix = Uri.EscapeDataString(_scenarioPrefix + "*");
        _lastPagedResponse = await _driver.SendListSalesAsync(
            $"?_page={page}&_size={size}&saleNumber={encodedPrefix}");
    }

    [When(@"eu listar as vendas com prefixo ""([^""]*)"" _page=(\d+) e _size=(\d+)")]
    public async Task QuandoEuListarAsVendasComPrefixoPageESize(string prefix, int page, int size)
    {
        _scenarioPrefix = prefix;
        var encodedPrefix = Uri.EscapeDataString(prefix + "*");
        _lastPagedResponse = await _driver.SendListSalesAsync(
            $"?_page={page}&_size={size}&saleNumber={encodedPrefix}");
    }

    [Then(@"o totalCount deve ser pelo menos (\d+)")]
    public void EntaoOTotalCountDeveSerPeloMenos(long minCount)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.TotalCount.Should().BeGreaterThanOrEqualTo(minCount);
    }

    [Then(@"o totalPages deve ser pelo menos (\d+)")]
    public void EntaoOTotalPagesDeveSerPeloMenos(int minPages)
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.TotalPages.Should().BeGreaterThanOrEqualTo(minPages);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Alias legado customer=
    // ──────────────────────────────────────────────────────────────────────────

    [When(@"eu listar as vendas com o parâmetro legado ""customer"" igual a ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComOParametroLegadoCustomerIgualA(string customerName)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync(
            $"?customer={Uri.EscapeDataString(customerName)}&saleNumber={Uri.EscapeDataString(_scenarioPrefix + "*")}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Alias legado branch=
    // ──────────────────────────────────────────────────────────────────────────

    [When(@"eu listar as vendas com o parâmetro legado ""branch"" igual a ""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComOParametroLegadoBranchIgualA(string branchName)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync(
            $"?branch={Uri.EscapeDataString(branchName)}&saleNumber={Uri.EscapeDataString(_scenarioPrefix + "*")}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: Lista vazia quando filtro não tem resultados
    // ──────────────────────────────────────────────────────────────────────────

    [Given(@"que não existem vendas com o saleNumber ""([^""]*)""")]
    public void DadoQueNaoExistemVendasComOSaleNumber(string saleNumber)
    {
        // Arrange — nenhuma ação necessária; banco limpo pelo AfterScenario
    }

    [Then(@"o totalCount deve ser 0")]
    public void EntaoOTotalCountDeveSer0()
    {
        _lastPagedResponse.Should().NotBeNull();
        _lastPagedResponse!.TotalCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Esquema do Cenário: Parâmetros de paginação inválidos
    // ──────────────────────────────────────────────────────────────────────────

    [When(@"eu listar as vendas com ""([^""]*)""=""([^""]*)""")]
    public async Task QuandoEuListarAsVendasComParametroIgualA(string param, string value)
    {
        _lastPagedResponse = await _driver.SendListSalesAsync($"?{param}={value}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cenário: totalCount corresponde ao número de registros no banco
    // ──────────────────────────────────────────────────────────────────────────

    [Then(@"o totalCount corresponde ao número de registros no banco com prefixo ""([^""]*)""")]
    public async Task EntaoOTotalCountCorrespondeAoNumeroDeRegistrosNoBancoComPrefixo(string prefix)
    {
        // Assert HTTP
        _lastPagedResponse.Should().NotBeNull();

        // Assert DB
        await using var db = _factory.CreateTestDbContext();
        var allSales = await db.Sales.AsNoTracking().ToListAsync();
        var dbCount = allSales.Count(s => s.SaleNumber.Value.StartsWith(prefix));
        _lastPagedResponse!.TotalCount.Should().Be(dbCount);
    }
}
