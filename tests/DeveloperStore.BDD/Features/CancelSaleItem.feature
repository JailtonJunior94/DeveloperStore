#language: pt-BR
Funcionalidade: Cancelar item de venda
  Como consumidor da API de vendas
  Quero cancelar um item individual de uma venda
  Para que o item seja marcado como cancelado e o total da venda seja recalculado

  Cenário: Cancelar um item de venda com dois itens recalcula o TotalAmount
    Dado que existe uma venda ativa com dois itens cadastrada no sistema
    Quando eu cancelar o primeiro item da venda
    Então a resposta deve ter status 200
    E o item cancelado deve estar marcado como cancelado na resposta
    E a venda deve permanecer com Status NotCancelled
    E o TotalAmount da venda no banco deve refletir apenas o item não cancelado

  Cenário: Cancelar o último item ativo torna a venda Cancelled automaticamente
    Dado que existe uma venda ativa com um único item cadastrada no sistema
    Quando eu cancelar o único item da venda
    Então a resposta deve ter status 200
    E o status da venda retornada deve ser "Cancelled"
    E a venda com item cancelado deve existir no banco com todos os itens cancelados

  Cenário: Cancelar item já cancelado é idempotente e retorna 200
    Dado que existe uma venda ativa com um item já cancelado
    Quando eu cancelar novamente o item já cancelado
    Então a resposta deve ter status 200
    E o item cancelado deve estar marcado como cancelado na resposta

  Cenário: Cancelar item em venda cancelada retorna 409
    Dado que existe uma venda totalmente cancelada para cancelamento de item
    Quando eu tentar cancelar um item dessa venda cancelada
    Então a resposta deve ter status 409
    E o tipo do erro deve ser "sale_state_conflict"

  Cenário: Cancelar item com saleId inválido retorna 422
    Dado que o saleId informado para cancelamento de item é inválido e não é um UUID
    Quando eu tentar cancelar um item com o saleId inválido
    Então a resposta deve ter status 422
    E o código do erro de validação deve ser "sale_id_invalid"

  Cenário: Cancelar item com itemId inválido retorna 422
    Dado que existe uma venda ativa cadastrada no sistema para cancelamento de item
    Quando eu tentar cancelar um item com itemId inválido dessa venda
    Então a resposta deve ter status 422
    E o código do erro de validação deve ser "item_id_invalid"

  Cenário: Cancelar item com saleId inexistente retorna 404
    Dado que não existe nenhuma venda com o saleId informado para cancelamento de item
    Quando eu tentar cancelar um item com o saleId inexistente
    Então a resposta deve ter status 404
    E o tipo do erro deve ser "resource_not_found"

  Cenário: Cancelar item com itemId inexistente retorna 404
    Dado que existe uma venda ativa cadastrada no sistema para cancelamento de item
    Quando eu tentar cancelar um item com itemId inexistente dessa venda
    Então a resposta deve ter status 404
    E o tipo do erro deve ser "resource_not_found"
