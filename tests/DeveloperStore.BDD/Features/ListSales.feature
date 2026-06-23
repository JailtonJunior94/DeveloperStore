#language: pt-BR
Funcionalidade: Listar vendas
  Como consumidor da API de vendas
  Quero listar as vendas com suporte a filtros, ordenação e paginação
  Para consultar e monitorar as vendas do sistema

  Cenário: Listar vendas com paginação padrão retorna 200 com dados corretos
    Dado que existem 3 vendas cadastradas com prefixo "LIST-PAD"
    Quando eu listar as vendas sem filtros
    Então a resposta deve ter status 200
    E o response deve conter ao menos 3 vendas
    E o pageNumber do response deve ser 1
    E o pageSize do response deve ser 10

  Cenário: Filtrar por número da venda exato retorna apenas a venda correspondente
    Dado que existem vendas cadastradas com prefixo "LIST-EXACT"
    E que uma das vendas tem saleNumber "LIST-EXACT-001"
    Quando eu listar as vendas com filtro saleNumber igual a "LIST-EXACT-001"
    Então a resposta deve ter status 200
    E o response deve conter exatamente 1 venda
    E a venda retornada deve ter saleNumber "LIST-EXACT-001"

  Cenário: Filtrar por número da venda com wildcard retorna múltiplas vendas
    Dado que existem 3 vendas cadastradas com prefixo "LIST-WILD"
    Quando eu listar as vendas com filtro saleNumber usando wildcard "LIST-WILD*"
    Então a resposta deve ter status 200
    E o response deve conter exatamente 3 vendas
    E todas as vendas retornadas devem ter saleNumber iniciando com "LIST-WILD"

  Cenário: Filtrar por nome do cliente retorna apenas vendas do cliente
    Dado que existem 2 vendas cadastradas com prefixo "LIST-CUST" para o cliente "Alice Santos"
    E que existe 1 venda cadastrada com prefixo "LIST-CUST" para o cliente "Bob Lima"
    Quando eu listar as vendas com filtro customerName igual a "Alice Santos"
    Então a resposta deve ter status 200
    E o response deve conter exatamente 2 vendas
    E todas as vendas retornadas devem pertencer ao cliente "Alice Santos"

  Cenário: Filtrar por nome da filial retorna apenas vendas da filial
    Dado que existem 2 vendas cadastradas com prefixo "LIST-BRNCH" para a filial "Filial Central"
    E que existe 1 venda cadastrada com prefixo "LIST-BRNCH" para a filial "Filial Norte"
    Quando eu listar as vendas com filtro branchName igual a "Filial Central"
    Então a resposta deve ter status 200
    E o response deve conter exatamente 2 vendas
    E todas as vendas retornadas devem pertencer à filial "Filial Central"

  Cenário: Filtrar por status Cancelled retorna apenas vendas canceladas
    Dado que existem 2 vendas cadastradas com prefixo "LIST-STS" e a primeira está cancelada
    Quando eu listar as vendas com filtro status igual a "Cancelled"
    Então a resposta deve ter status 200
    E todas as vendas retornadas devem ter status "Cancelled"

  Cenário: Filtrar por status NotCancelled retorna apenas vendas ativas
    Dado que existem 2 vendas cadastradas com prefixo "LIST-STSN" e a primeira está cancelada
    Quando eu listar as vendas com filtro status igual a "NotCancelled"
    Então a resposta deve ter status 200
    E todas as vendas retornadas devem ter status "NotCancelled"

  Cenário: Filtrar por intervalo de datas retorna apenas vendas no período
    Dado que existem 3 vendas com prefixo "LIST-DT" com datas distribuídas em janeiro de 2024
    Quando eu listar as vendas com _minSoldAt "2024-01-10T00:00:00Z" e _maxSoldAt "2024-01-20T23:59:59Z"
    Então a resposta deve ter status 200
    E apenas vendas dentro do intervalo de datas devem ser retornadas

  Cenário: Ordenar por número da venda ascendente retorna vendas na ordem correta
    Dado que existem 3 vendas cadastradas com prefixo "LIST-ORD"
    Quando eu listar as vendas com ordenação "_order=saleNumber asc"
    Então a resposta deve ter status 200
    E os saleNumbers das vendas retornadas devem estar em ordem ascendente

  Cenário: Paginação com múltiplas páginas - página 1
    Dado que existem 5 vendas cadastradas com prefixo "LIST-PG"
    Quando eu listar as vendas com _page=1 e _size=3
    Então a resposta deve ter status 200
    E o pageNumber do response deve ser 1
    E o pageSize do response deve ser 3
    E o totalCount deve ser pelo menos 5
    E o totalPages deve ser pelo menos 2

  Cenário: Paginação com múltiplas páginas - página 2
    Dado que existem 5 vendas cadastradas com prefixo "LIST-PG2"
    Quando eu listar as vendas com prefixo "LIST-PG2" _page=2 e _size=3
    Então a resposta deve ter status 200
    E o pageNumber do response deve ser 2

  Cenário: Suporte ao alias legado customer= filtra por nome do cliente
    Dado que existem 2 vendas cadastradas com prefixo "LIST-ALI" para o cliente "Carlos Neto"
    Quando eu listar as vendas com o parâmetro legado "customer" igual a "Carlos Neto"
    Então a resposta deve ter status 200
    E o response deve conter ao menos 2 vendas
    E todas as vendas retornadas devem pertencer ao cliente "Carlos Neto"

  Cenário: Suporte ao alias legado branch= filtra por nome da filial
    Dado que existem 2 vendas cadastradas com prefixo "LIST-ALB" para a filial "Filial Sul"
    Quando eu listar as vendas com o parâmetro legado "branch" igual a "Filial Sul"
    Então a resposta deve ter status 200
    E o response deve conter ao menos 2 vendas
    E todas as vendas retornadas devem pertencer à filial "Filial Sul"

  Cenário: Filtro sem resultados retorna lista vazia com 200
    Dado que não existem vendas com o saleNumber "LIST-EMPTY-999"
    Quando eu listar as vendas com filtro saleNumber igual a "LIST-EMPTY-999"
    Então a resposta deve ter status 200
    E o response deve conter exatamente 0 vendas
    E o totalCount deve ser 0

  Esquema do Cenário: Parâmetros de paginação inválidos retornam 422 com código correto
    Quando eu listar as vendas com "<parametro>"="<valor>"
    Então a resposta deve ter status 422
    E o código do erro de validação deve ser "<codigo>"

    Exemplos:
      | parametro   | valor | codigo               |
      | _page       | 0     | page_number_invalid  |
      | _size       | 101   | page_size_invalid    |

  Cenário: Ordenação por campo inválido retorna 422 com código order_invalid
    Quando eu listar as vendas com ordenação "_order=campoInexistente asc"
    Então a resposta deve ter status 422
    E o código do erro de validação deve ser "order_invalid"

  Cenário: Intervalo de datas invertido retorna 422 com código sold_at_range_invalid
    Quando eu listar as vendas com _minSoldAt "2024-06-30T00:00:00Z" e _maxSoldAt "2024-01-01T00:00:00Z"
    Então a resposta deve ter status 422
    E o código do erro de validação deve ser "sold_at_range_invalid"

  Cenário: O totalCount no response corresponde ao número de registros no banco
    Dado que existem 3 vendas cadastradas com prefixo "LIST-CNT"
    Quando eu listar as vendas com filtro saleNumber usando wildcard "LIST-CNT*"
    Então a resposta deve ter status 200
    E o totalCount corresponde ao número de registros no banco com prefixo "LIST-CNT"
