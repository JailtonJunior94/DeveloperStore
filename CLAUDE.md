# DeveloperStore — Diretrizes para Claude Code

## Idioma nos testes BDD

- Arquivos `.feature` (Gherkin): **Português (pt-BR)**
  - Palavras-chave: `Funcionalidade`, `Cenário`, `Esquema do Cenário`, `Exemplos`, `Dado`/`Dada`/`Dados`, `Quando`, `Então`, `E`, `Mas`
- Código C# (step definitions, fixtures, helpers, hooks): **Inglês**
  - Atributos Reqnroll sempre em inglês: `[Given]`, `[When]`, `[Then]`, `[And]`, `[But]`
  - Classes, métodos, variáveis: inglês

## Padrão de validação nos steps BDD (mandatório)

Toda validação deve seguir o padrão **AAA**:
- `Given` → Arrange (monta o request ou pré-condição)
- `When` → Act (executa a chamada HTTP via `SalesApiDriver`)
- `Then` → Assert (verifica resposta HTTP **e** consulta o banco diretamente via `DefaultContext`)

Consultar o banco diretamente é **obrigatório** para cenários que verificam persistência:
- Criação de venda: buscar a venda no banco com `.Include(s => s.Items)`
- Cancelamento: verificar `Status == Cancelled` e `Items.All(i => i.IsCancelled)`
- Cancelamento de item: verificar `TotalAmount` recalculado excluindo itens cancelados
- Conflito de duplicata: contar registros no banco (`CountAsync`) para garantir unicidade

## Projeto BDD

- Localização: `tests/DeveloperStore.BDD/`
- Framework: Reqnroll 2.x + xUnit
- Banco: TestContainers PostgreSQL (sem dependência de infraestrutura externa)
- Configuração: `reqnroll.json` com `language.feature = "pt-BR"`
