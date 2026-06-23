#language: pt-BR
Funcionalidade: Criar Venda
  Como consumidor da API de vendas
  Quero criar novas vendas
  Para registrar transações comerciais no sistema

  Cenário: Criar venda válida com um item
    Dado dados válidos de venda com um item
    Quando envio POST para /api/sales
    Então recebo status 201 Created
    E a venda está persistida no banco com os dados corretos

  Cenário: Criar venda com múltiplos itens
    Dado dados válidos de venda com múltiplos itens
    Quando envio POST para /api/sales
    Então recebo status 201 Created
    E o totalAmount da resposta é igual à soma dos itens

  Esquema do Cenário: Campo obrigatório ausente retorna 422
    Dado uma requisição de criação de venda sem o campo "<campo>"
    Quando envio POST para /api/sales
    Então recebo status 422 Unprocessable Entity
    E o código de erro "<codigo_erro>" está presente na resposta

    Exemplos:
      | campo              | codigo_erro                    |
      | saleNumber         | sale_number_required           |
      | soldAt             | sold_at_required               |
      | customerExternalId | customer_external_id_required  |
      | customerName       | customer_name_required         |
      | branchExternalId   | branch_external_id_required    |
      | branchName         | branch_name_required           |

  Cenário: Criar venda sem itens retorna 422
    Dado dados válidos de venda sem itens
    Quando envio POST para /api/sales
    Então recebo status 422 Unprocessable Entity
    E o código de erro "items_required" está presente na resposta

  Esquema do Cenário: Quantidade de item inválida retorna 422
    Dado dados válidos de venda com um item de quantidade <quantidade>
    Quando envio POST para /api/sales
    Então recebo status 422 Unprocessable Entity
    E o código de erro "<codigo_erro>" está presente na resposta

    Exemplos:
      | quantidade | codigo_erro              |
      | 0          | quantity_must_be_positive |
      | 21         | quantity_limit_exceeded   |

  Cenário: Preço unitário zero retorna 422
    Dado dados válidos de venda com um item com preço unitário zero
    Quando envio POST para /api/sales
    Então recebo status 422 Unprocessable Entity
    E o código de erro "unit_price_must_be_positive" está presente na resposta

  Cenário: Número de venda duplicado retorna 409
    Dado uma venda com o número "BDD-DUP-001" já existe no banco
    Quando envio POST para /api/sales com o número "BDD-DUP-001"
    Então recebo status 409 Conflict
    E o código de erro "sale_number_conflict" está presente na resposta
    E existe apenas uma venda com número "BDD-DUP-001" no banco

  Esquema do Cenário: Total do item calculado corretamente
    Dado dados válidos de venda com um item de quantidade <quantidade> e preço <preco>
    Quando envio POST para /api/sales
    Então recebo status 201 Created
    E o totalAmount do item na resposta é <total_esperado>

    Exemplos:
      | quantidade | preco | total_esperado |
      | 1          | 10.00 | 10.00          |
      | 3          | 10.00 | 30.00          |
      | 4          | 10.00 | 36.00          |
      | 5          | 4.50  | 20.25          |
      | 9          | 10.00 | 81.00          |
      | 10         | 10.00 | 80.00          |
      | 20         | 99.99 | 1599.84        |

  Cenário: Produto duplicado na mesma venda retorna 422
    Dado dados de venda com dois itens com o mesmo productExternalId
    Quando envio POST para /api/sales
    Então recebo status 422 Unprocessable Entity
    E o código de erro "duplicate_product_in_sale" está presente na resposta
