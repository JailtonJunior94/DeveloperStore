# Postman

Arquivos:

- `DeveloperStore.postman_collection.json`
- `DeveloperStore.postman_environment.json`

Sequencia exata de uso:

1. Inicie a API localmente com `dotnet run --project src/DeveloperStore.WebApi`.
2. Importe `docs/postman/DeveloperStore.postman_collection.json` no Postman.
3. Importe `docs/postman/DeveloperStore.postman_environment.json` no Postman.
4. Selecione o environment `DeveloperStore Local`.
5. Ajuste `base_url` se sua API nao estiver em `http://localhost:5119`.
6. Execute `01 - Health Summary`, `02 - Health Live` e `03 - Health Ready` para validar disponibilidade da API e do banco.
7. Execute `04 - Swagger JSON`, `05 - Swagger Shortcut` e `06 - Swagger UI` para validar os endpoints de bootstrap expostos pela aplicacao.
8. Execute `07 - Create Sale`. O script `post-response` salva automaticamente `sale_id`, `sale_item_id`, `sale_status` e `created_sale_location`.
9. Execute `08 - Get Sale By Id`. O script reaproveita `sale_id` e atualiza `sale_item_id` com o primeiro item retornado.
10. Execute `09 - List Sales` para consultar a venda criada usando os filtros canonicos.
11. Execute `10 - List Sales Legacy Alias` para validar os aliases `customer` e `branch`.
12. Execute `11 - Update Sale`. O script usa `sale_id`, envia `updated_sale_number` e refresca `sale_item_id`.
13. Execute `12 - Cancel Sale Item`. O script usa `sale_id` + `sale_item_id` e valida `isCancelled = true`.
14. Execute `13 - Cancel Sale`. O script usa `sale_id` e valida `status = Cancelled`.
15. Execute `14 - Get Cancelled Sale` para confirmar o estado final persistido.

Variaveis relevantes:

- `base_url`: URL base da API.
- `api_version`: usada no endpoint `/swagger/{{api_version}}/swagger.json`.
- `sale_id`: preenchida automaticamente apos `07 - Create Sale`.
- `sale_item_id`: preenchida automaticamente apos `07 - Create Sale` e atualizada em `08 - Get Sale By Id` e `11 - Update Sale`.
- `sale_number` e `updated_sale_number`: geradas automaticamente no `pre-request`.
- `token`: placeholder reservado no environment; a API atual nao exige autenticacao.

Como o encadeamento funciona:

1. O `pre-request` da colecao inicializa variaveis padrao, datas e identificadores de exemplo.
2. O `pre-request` de `07 - Create Sale` gera um conjunto novo de ids externos e numeros para evitar colisao por `saleNumber`.
3. O `post-response` de `07 - Create Sale` extrai `data.id` e `data.items[0].id`.
4. `08 - Get Sale By Id`, `11 - Update Sale`, `12 - Cancel Sale Item`, `13 - Cancel Sale` e `14 - Get Cancelled Sale` consomem essas variaveis sem edicao manual.

Observacoes:

- O endpoint `GET /api/sales` aceita filtros canonicos (`customerName`, `branchName`) e aliases legados (`customer`, `branch`), por isso a colecao tem duas requests distintas para a mesma rota.
- Os endpoints de Swagger UI retornam HTML e foram incluidos porque sao expostos pelo bootstrap da aplicacao.
