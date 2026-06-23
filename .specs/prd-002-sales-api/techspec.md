# Tech Spec 002 - Sales API

## Decisões

- `RF-001`: usar `Sale` como aggregate root e `SaleItem` como entidade interna.
- `RF-001`: aplicar abordagem de `Domain Modeling Made Functional` com smart constructors e value objects (`SaleNumber`, `ExternalReference`).
- `RF-002`: persistir cliente, filial e produto por external id + descrição desnormalizada.
- `RF-003`: implementar queries via repositório EF Core com filtros server-side e paginação.
- `RF-004`: recalcular totais sempre no domínio, nunca aceitar total do request.
- `RF-005`: `DELETE /api/sales/{id}` representa cancelamento lógico.
- `RF-006`: `POST /api/sales/{saleId}/items/{itemId}/cancel` recalcula o total da venda.
- `RF-008`: publicar eventos via `IDomainEventPublisher` com implementação de logging estruturado.
- `RN-001`: regras de desconto ficam encapsuladas em `SaleItem`.
- `RN-005`: alterações em venda cancelada disparam erro de domínio.
