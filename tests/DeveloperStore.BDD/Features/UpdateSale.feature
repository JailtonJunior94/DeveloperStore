#language: pt-BR
Funcionalidade: Atualizar venda
  Como consumidor da API de vendas
  Quero atualizar os dados de uma venda existente
  Para manter as informações de vendas sempre corretas

  Cenário: Atualizar venda existente com novos dados retorna 200 com dados atualizados
    Dado que existe uma venda cadastrada para atualização
    Quando eu atualizar a venda com novos dados de cliente e filial
    Então a resposta de atualização deve ter status 200
    E os dados da venda atualizada devem refletir as novas informações
    E os novos dados devem estar persistidos no banco de dados

  Cenário: Atualizar venda mantendo o mesmo saleNumber retorna 200
    Dado que existe uma venda cadastrada para manter o mesmo número
    Quando eu atualizar a venda mantendo o mesmo saleNumber
    Então a resposta de atualização deve ter status 200
    E os dados da venda retornada devem conter o mesmo saleNumber

  Cenário: Atualizar venda com ID inválido retorna 422
    Dado que o ID da venda a ser atualizada é inválido
    Quando eu tentar atualizar a venda com ID inválido
    Então a resposta de atualização deve ter status 422
    E o código do erro de atualização deve ser "sale_id_invalid"

  Cenário: Atualizar venda com ID inexistente retorna 404
    Dado que não existe venda com o ID informado para atualização
    Quando eu tentar atualizar a venda inexistente
    Então a resposta de atualização deve ter status 404
    E o tipo do erro de atualização deve ser "resource_not_found"

  Cenário: Alterar saleNumber para número já existente retorna 409
    Dado que existem duas vendas cadastradas com números distintos
    Quando eu tentar atualizar a primeira venda com o saleNumber da segunda venda
    Então a resposta de atualização deve ter status 409
    E o tipo do erro de atualização deve ser "sale_number_conflict"

  Cenário: Atualizar venda cancelada retorna 409
    Dado que existe uma venda cadastrada e ela foi cancelada
    Quando eu tentar atualizar a venda cancelada
    Então a resposta de atualização deve ter status 409
    E o tipo do erro de atualização deve ser "sale_state_conflict"

  Cenário: Atualizar quantidade de item recalcula desconto e total
    Dado que existe uma venda cadastrada com um item de quantidade 2 e preço 10.00
    Quando eu atualizar a venda com quantidade 10 do mesmo item
    Então a resposta de atualização deve ter status 200
    E o totalAmount da venda retornada deve ser 80.00
    E o desconto do item na resposta deve ser 20%

  Cenário: Atualizar venda adicionando novo item atualiza total
    Dado que existe uma venda cadastrada com um item de quantidade 1 e preço 10.00
    Quando eu atualizar a venda incluindo um segundo item de quantidade 5 e preço 20.00
    Então a resposta de atualização deve ter status 200
    E o totalAmount da venda retornada deve ser 100.00
    E a venda no banco deve conter 2 itens
