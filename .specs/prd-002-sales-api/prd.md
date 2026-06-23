# PRD 002 - Sales API

## Resumo

Implementar uma API completa de vendas com regras de desconto, cancelamento, eventos de domínio e modelagem DDD.

## Requisitos Funcionais

- `RF-001`: criar venda com número, data, cliente, filial e itens.
- `RF-002`: consultar venda por identificador.
- `RF-003`: listar vendas com paginação e filtros por número, cliente, filial, status e faixa de data.
- `RF-004`: atualizar venda inteira com recálculo de totais.
- `RF-005`: cancelar venda logicamente.
- `RF-006`: cancelar item individual da venda.
- `RF-007`: persistir total da venda, total do item, desconto percentual e desconto monetário.
- `RF-008`: publicar `SaleCreated`, `SaleModified`, `SaleCancelled` e `ItemCancelled` sem broker.

## Regras de Negócio

- `RN-001`: `1 a 3` itens idênticos não recebem desconto.
- `RN-002`: `4 a 9` itens idênticos recebem `10%`.
- `RN-003`: `10 a 20` itens idênticos recebem `20%`.
- `RN-004`: não é permitido vender mais de `20` unidades do mesmo produto.
- `RN-005`: venda cancelada não pode ser alterada.
