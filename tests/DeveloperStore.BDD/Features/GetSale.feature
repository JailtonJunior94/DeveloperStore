#language: pt-BR
Funcionalidade: Buscar venda por ID
  Como consumidor da API de vendas
  Quero buscar uma venda pelo seu identificador
  Para obter os detalhes completos da venda

  Cenário: Buscar venda existente pelo ID retorna 200 com dados corretos
    Dado que existe uma venda cadastrada no sistema
    Quando eu buscar a venda pelo ID cadastrado
    Então a resposta deve ter status 200
    E os dados da venda retornados devem corresponder à venda cadastrada
    E a venda deve existir no banco de dados com os dados corretos

  Cenário: Buscar venda com ID inexistente retorna 404
    Dado que não existe nenhuma venda com o ID informado
    Quando eu buscar a venda pelo ID inexistente
    Então a resposta deve ter status 404
    E o tipo do erro deve ser "resource_not_found"

  Cenário: Buscar venda com ID inválido retorna 422
    Dado que o ID informado é inválido e não é um UUID
    Quando eu buscar a venda pelo ID inválido
    Então a resposta deve ter status 422
    E o código do erro de validação deve ser "sale_id_invalid"
