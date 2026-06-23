#language: pt-BR
Funcionalidade: Cancelar venda
  Como consumidor da API de vendas
  Quero cancelar uma venda pelo seu identificador
  Para que a venda e todos os seus itens sejam marcados como cancelados

  Cenário: Cancelar venda ativa retorna 200 e cancela todos os itens
    Dado que existe uma venda ativa cadastrada no sistema
    Quando eu cancelar a venda pelo ID cadastrado
    Então a resposta deve ter status 200
    E o status da venda retornada deve ser "Cancelled"
    E a venda cancelada deve existir no banco com todos os itens cancelados
    E o TotalAmount da venda cancelada no banco deve ser zero

  Cenário: Cancelar venda já cancelada é idempotente e retorna 200
    Dado que existe uma venda já cancelada cadastrada no sistema
    Quando eu cancelar a venda pelo ID cadastrado
    Então a resposta deve ter status 200
    E o status da venda retornada deve ser "Cancelled"

  Cenário: Cancelar venda com ID inválido retorna 422
    Dado que o ID de cancelamento informado é inválido e não é um UUID
    Quando eu cancelar a venda pelo ID inválido
    Então a resposta deve ter status 422
    E o código do erro de validação deve ser "sale_id_invalid"

  Cenário: Cancelar venda com ID inexistente retorna 404
    Dado que não existe nenhuma venda com o ID informado para cancelamento
    Quando eu cancelar a venda pelo ID inexistente
    Então a resposta deve ter status 404
    E o tipo do erro deve ser "resource_not_found"

  Cenário: TotalAmount é zero no banco após cancelamento de venda ativa
    Dado que existe uma venda ativa cadastrada no sistema
    Quando eu cancelar a venda pelo ID cadastrado
    Então a resposta deve ter status 200
    E o TotalAmount da venda cancelada no banco deve ser zero
