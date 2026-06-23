<!-- governance-schema: 1.0.0 -->
# Regras para Agentes de IA

Este diretorio centraliza regras para uso com agentes de IA em tarefas reais de analise, alteracao e validacao de codigo.

## Objetivo

Use estas instrucoes para manter consistencia, seguranca e qualidade ao trabalhar com codigo, configuracao, validacao e evolucao de sistemas.

## Arquitetura: monolito

O projeto e um monolito backend em camadas, orientado a dominio e exposto por HTTP. A governanca deve privilegiar coesao local, limites de pacote claros, baixo acoplamento entre camadas e crescimento incremental da estrutura sem inflar a solucao com abstracoes desnecessarias.

Stack detectada:

- Backend: `ASP.NET Core Web API`
- Linguagem: `C# 14 / .NET 10`
- Persistencia: `EF Core + PostgreSQL`
- Testes: `xUnit`
- Validacao: `FluentValidation`
- Mediacao: `MediatR`
- Observabilidade: `Serilog`

## Estrutura de Pastas

```text
.
├── .doc
├── .specs
├── src
│   ├── DeveloperStore.Application
│   ├── DeveloperStore.Common
│   ├── DeveloperStore.Domain
│   ├── DeveloperStore.IoC
│   ├── DeveloperStore.ORM
│   └── DeveloperStore.WebApi
├── tests
│   ├── DeveloperStore.Functional
│   ├── DeveloperStore.Integration
│   ├── DeveloperStore.Postgres
│   └── DeveloperStore.Unit
└── README.md
```

## Padrao Arquitetural

Padrao arquitetural adotado:

- `Domain` concentra modelo ubíquo, invariantes, transicoes e eventos.
- `Application` orquestra casos de uso e nao carrega regra de negocio central.
- `ORM` implementa persistencia e mapeamentos EF Core.
- `WebApi` traduz HTTP para casos de uso e devolve contratos de API.
- `IoC` faz wiring explicito.
- `Common` concentra validacao transversal, logging e health checks.

### Estilo Arquitetural

Este repositorio deve evoluir como:

1. `DDD pragmatico`, com aggregate root, VOs e exceptions de dominio.
2. `application services thin`, sem logica central de negocio em handlers.
3. `persistence ignorant enough`, aceitando compromissos controlados de ORM sem deixar o modelo ser guiado pela infraestrutura.
4. `API first at the edge`, com requests/responses claros, status HTTP coerentes e erro semantico uniforme.
5. `production proof`, com evidencia real de comportamento em provider relacional.

### Fluxo de Dependencias

- Dependencias devem apontar de bordas externas para o nucleo do negocio.
- Detalhes de framework, IO e persistencia nao devem vazar para o centro do sistema.
- `WebApi -> Application -> Domain`
- `ORM -> Domain`
- `IoC -> Application/ORM/Common/Domain`
- `Common` pode ser consumido por `Application`, `IoC` e `WebApi` apenas para preocupacoes transversais.

### Fronteiras Mandatorias

1. `Domain` nao referencia `ASP.NET`, `EF Core`, `Serilog` ou detalhes de transporte.
2. `Application` nao implementa regra de negocio central que deveria viver no aggregate.
3. `WebApi` nao calcula desconto, nao recalcula total e nao decide transicoes de estado.
4. `ORM` nao inventa regra de negocio; apenas persiste, consulta e traduz limites do provider.
5. `IoC` nao contem logica de negocio.

## Regras Mandatorias Deste Projeto

### Domain Modeling Made Functional

Para alteracoes no dominio, o livro `Domain Modeling Made Functional: Tackle Software Complexity with Domain-Driven Design and F#` e referencia obrigatoria de modelagem.

Aplicar obrigatoriamente:

1. Modelar conceitos de negocio com tipos explicitos e semanticamente nomeados.
2. Evitar primitive obsession no dominio e, sempre que viavel, na aplicacao.
3. Usar smart constructors ou factories que protejam invariantes.
4. Concentrar transicoes de estado no aggregate root.
5. Tratar regras de negocio como comportamento do dominio, nao como if espalhado em handlers, controllers ou repositories.
6. Representar identidades externas e descricoes desnormalizadas com tipos explicitos.
7. Se for necessario usar primitivo por restricao de framework ou persistencia, manter isso na borda e justificar explicitamente.
8. O estado canônico de aggregates e entities deve preferir tipos semânticos; campos escalares de suporte ao ORM sao excecao e nao devem vazar como API do dominio.
9. `ExternalReference` generico so e aceitavel quando houver justificativa explicita de impossibilidade de especializacao por contexto.
10. Sempre avaliar se o conceito pede `VO`, `enum`, `aggregate`, `domain event` ou `exception` especifica antes de usar um tipo generico.

### Tipos Primitivos

As seguintes diretrizes sao obrigatorias:

1. Nao introduzir `string`, `int`, `decimal`, `Guid`, `DateTime` ou `DateTimeOffset` crus no dominio para representar conceitos de negocio sem avaliar VO dedicado.
2. `Money`, `Quantity`, `SaleNumber`, ids, nomes, status e referencias externas devem preferir tipos semanticamente ricos.
3. DTOs e requests HTTP podem usar primitivos na borda, mas devem ser convertidos cedo para tipos de dominio ou tipos de aplicacao semanticamente nomeados.
4. Qualquer propriedade `*Value`, `*Id`, `*Name`, `*Amount`, `*Date` ou equivalente exposta como primitivo no dominio deve ser tratada como smell e exigir justificativa explicita.
5. Tipos monetarios e quantitativos devem concentrar arredondamento, comparacao e operacoes; nao espalhar `decimal.Round`, limites e calculos pelo dominio.

### Validacao e API

As seguintes regras sao obrigatorias para endpoints e casos de uso:

1. Validar o mais cedo possivel, antes de executar regra de negocio ou persistencia.
2. Validacao deve ser fail fast por etapa: se a fronteira de validacao falhar, o fluxo deve parar imediatamente.
3. A resposta de erro da API deve ser semantica, consistente e orientada ao consumidor.
4. Erros de validacao devem retornar lista de erros com ao menos `code`, `field` e `message`.
5. Erros de dominio devem ter `code` estavel e status HTTP coerente.
6. Nao retornar mensagens vagas como `something went wrong`.
7. Nao expor stack trace ao cliente.
8. Requests HTTP podem receber primitivos, mas a conversao para tipos semanticos deve ocorrer antes de construir comandos ou entrar na regra de negocio.
9. Comandos da aplicacao nao devem ser meros espelhos do payload HTTP quando o dominio tiver tipos explicitos equivalentes.
10. IDs e query params malformados devem falhar como erro semantico de validacao, nao como `404` de recurso inexistente.
11. Para endpoints de listagem, seguir primeiro o contrato documentado em `.doc/general-api.md` quando houver conflito com implementacao anterior.
12. O payload minimo de erro para APIs deste projeto deve preservar `type`, `error` e `detail`; metadados adicionais sao permitidos apenas sem quebrar esse shape base.
13. `ApiController`, model binding, validators e middleware devem convergir para um unico contrato de erro observavel pelo cliente.
14. Qualquer mudanca em shape de erro, codigos, status HTTP ou semantica de query string exige teste funcional.
15. Falha de validacao na borda nao deve chegar ao repositório nem ao aggregate.
16. Testes de contrato de erro devem inspecionar o payload JSON bruto (ou deserializar com `PropertyNamingPolicy` fixa em camelCase) para garantir que `type`, `error` e `detail` sejam retornados em camelCase. Deserializacao case-insensitive e testes que apenas leem propriedades .NET mascaram inconsistencias de casing no contrato HTTP.
17. Serializacao ad-hoc com `JsonSerializer.Serialize` sem `JsonSerializerOptions` e proibida em middleware, filters ou qualquer ponto que produza o payload de erro da API. O contrato de erro deve usar as mesmas opcoes de serializacao da aplicacao (camelCase por padrao).

### Robustez e Production-Ready

Para novas demandas, assumir como nao negociavel:

1. Foco em robustez antes de conveniencia.
2. Sem falso positivo em validacao, testes ou diagnostico.
3. Logs estruturados para eventos e falhas relevantes.
4. Configuracao externa para conexoes e credenciais.
5. Testes cobrindo caminho feliz, regras e falhas relevantes — incluindo todos os limites de tier (ex: quantidade=3, 9, 20 para regras de desconto).
6. Nenhum merge com workspace quebrado, teste verde por artefato antigo ou dependencia de `bin/obj` preexistente.
7. Readiness real deve verificar infraestrutura critica, especialmente conectividade com banco relacional.
8. Migrations automaticas em startup sao opt-in e justificadas por ambiente; o padrao seguro e desligado.
9. Segredos reais nao podem ser versionados; apenas placeholders ou valores explicitamente locais.
10. Toda alegacao de “pronto para main” exige evidencia objetiva e atual: build limpo, testes relevantes verdes e sem warning critico conhecido.
11. Provider `InMemory` nunca e evidencia suficiente para provar queries, filtros, ordenacao, includes, owned types, constraints ou migrations.
12. Quando houver comportamento sensivel a traducao de LINQ, a prova canônica e o provider relacional real do projeto.
13. Se uma limitacao do provider exigir estrategia hibrida, documentar explicitamente a decisao e preservar corretude antes de otimizar conveniencia.
14. **Ordering obrigatório de eventos de dominio**: a sequencia correta e sempre `SaveChangesAsync` → `DequeueDomainEvents` → `PublishAsync`. Desencadear eventos antes de persistir os dados resulta em eventos permanentemente perdidos se a persistencia falhar. Nunca inverter essa ordem.
15. **Filtros sobre owned entity properties nao traduzem via EF.Functions.Like, Contains, StartsWith, EndsWith no Npgsql**: qualquer metodo de string sobre `sale.Customer.Description` ou `sale.Branch.Description` em predicado LINQ falha com `could not be translated`. A estrategia correta e dois-fases: fase SQL para filtros em colunas proprias da entidade raiz (status, data, saleNumber), fase in-memory para filtros em propriedades de owned entities. Nao tentar resolver via EF.Property aninhado.
16. **WHERE com `saleIds.Contains(sale.Id)` traduz corretamente; `expression.OrElse` com `.Id.Value == guid` nao traduz**: ao filtrar entidades por lista de IDs, usar `.Where(s => ids.Contains(s.Id))` onde `ids` e uma lista de tipos VO. Evitar construcao manual de Expression tree com `Expression.Constant(false)` como base de OrElse.
17. **Codigo morto e proibido**: exceptions, metodos privados, constantes e helpers sem nenhum chamador devem ser removidos imediatamente. A presenca de codigo nao referenciado e sinal de incompleto ou refactor abandonado.
18. **Proibido comentarios em codigo C#**: nenhum comentario de codigo, inline ou de bloco, deve ser adicionado a arquivos `.cs`. A proibicao abrange codigo de producao, testes unitarios, step definitions, fixtures, helpers e qualquer arquivo `.cs` versionado. Se o motivo de uma decisao precisar ser registrado, o lugar e o AGENTS.md ou um ADR, nao o codigo.
19. **Listagens e filtros exigem prova no provider relacional**: listagens, filtros, ordenacao, paginacao, `Include`, `AutoInclude`, `Contains`, `EF.Functions.Like` e queries sobre owned types so sao consideradas validadas quando ha caso de teste executado contra o provider relacional real do projeto (PostgreSQL). Testes com EF InMemory, stubs ou mocks nunca sao evidencia suficiente para esses cenarios.
20. **CI/CD deve executar BDD/PostgreSQL real como gate bloqueante**: o pipeline de merge para `main` deve incluir a suite `DeveloperStore.BDD` (ou testes de integracao equivalentes contra PostgreSQL real). Suites rapidas em memoria (Unit, Integration, Functional) nao sao gate suficiente para funcionalidades sensiveis a persistencia.
21. **Excecoes de dominio sao transporte-agnosticas**: e proibido referenciar `System.Net.HttpStatusCode`, `Microsoft.AspNetCore.*` ou qualquer detalhe de transporte no projeto `Domain`. O mapeamento de excecoes de dominio para status HTTP deve residir em `WebApi` ou `Application`.
22. **Observabilidade de falhas 500**: falhas que resultem em HTTP 500 devem produzir log estruturado com exception, traceId e contexto suficiente para diagnostico. Silencio em log para requisicoes que retornam 500 e considerado bug de observabilidade.

## Modo de trabalho

1. Entender o contexto antes de editar qualquer arquivo.
2. Preferir a menor mudanca segura que resolva a causa raiz.
3. Preservar arquitetura, convencoes e fronteiras ja existentes no contexto analisado.
4. Nao introduzir abstracoes, camadas ou dependencias sem demanda concreta.
5. Atualizar ou adicionar testes quando houver mudanca de comportamento.
6. Rodar validacoes proporcionais a mudanca.
7. Registrar bloqueios e suposicoes explicitamente quando o contexto estiver incompleto.

## Regras por Arquitetura

1. Preservar coesao local e dependencia unidirecional entre packages.
2. Evitar helpers transversais que escondam regra de negocio ou IO.
3. Crescer a estrutura apenas quando o codigo atual ja nao comportar a mudanca com clareza.
4. Nao mover regra de negocio para controller, middleware, mapper ou repository.
5. Repository deve expor operacoes de dominio, nao detalhes de SQL.
6. Repositories do dominio nao devem expor primitvos crus para ids, numeros, datas de negocio, filtros ou paginação sem avaliacao explicita de tipos semanticamente ricos.
7. Eventos de dominio devem preferir tipos de dominio ou payloads semanticamente nomeados; `Guid` e `string` crus sao excecao justificada.
8. Foreign keys tecnicas nao devem fazer parte do modelo ubíquo do dominio salvo justificativa arquitetural explicita.
9. Se um VO for adotado no fluxo interno, a migracao deve ser concluida ponta a ponta no mesmo escopo: dominio, aplicacao, ORM, API e testes.
10. `ExternalReference` ou equivalente generico deve ser reavaliado sempre que cliente, filial e produto exigirem semantica distinta em compilacao.
11. Para listagens, preferir `read models/projections` especificos quando isso reduzir fragilidade de traducao sem empobrecer o dominio.
12. Nao acoplar consultas de listagem ao aggregate completo quando uma projecao controlada for suficiente para paginação, ordenação ou filtro.
13. Quando filtros por owned types, value converters ou members calculados degradarem traducao SQL, isolar o problema em estrategia explicita e testada.
14. **Filtros e paginacao de repositorio nao expoem primitivos crus**: `string?`, `int`, `DateTimeOffset?`, `decimal?` e similares nao devem representar conceitos de negocio (`SaleNumber`, `SoldAt`, `Quantity`, etc.) em filtros de repositorio sem justificativa explicita. Se o primitivo for inevitavel por restricao de framework, documentar a decisao.
15. **Listagens nao carregam o universo completo para memoria**: e proibido trazer todo o conjunto de registros para a aplicacao para depois filtrar, ordenar ou paginar. Estrategias hibridas (fase SQL + fase in-memory) devem ser limitadas a subconjuntos controlados, justificadas por limitacao do provider e acompanhadas de analise de risco de performance/volume quando aplicavel.

## Regras Especificas de Persistencia

1. Toda query de repositório deve ser pensada para o provider real, nao apenas para `InMemory`.
2. `Include`, `AutoInclude`, filtros em owned types, `Contains`, ordenacao e paginação devem ser tratados como pontos de risco de traducao.
3. Antes de afirmar que uma query traduz corretamente no provider, validar que ela: (a) compila, (b) gera SQL valido no provider (pode ser verificado via `ToQueryString()` ou execucao real), (c) retorna os dados esperados.
4. Quando necessario, dividir consultas em duas fases:
   - fase relacional para reduzir universo
   - fase em memoria apenas sobre subconjunto controlado e justificado
5. Toda estrategia hibrida deve ser deterministica, testada e proporcional ao risco.
6. Nao usar workaround de provider sem registrar o motivo em comentario curto quando o codigo nao for autoexplicativo.
7. Migrations e snapshot do EF devem acompanhar mudancas de modelo persistido no mesmo commit.
8. **Cuidado com metodos de string sobre projecoes e owned entities no Npgsql**: nao usar `EF.Functions.Like`, `Contains`, `StartsWith`, `EndsWith` sobre propriedades de projecoes EF ou owned entities sem validar a traducao no provider Npgsql. Quando houver duvida, preferir fase relacional sobre colunas da entidade raiz e, se necessario, fase in-memory sobre subconjunto controlado.

## Regras de Documentacao e Escopo

1. `README.md` deve refletir o comportamento real atual da aplicacao, nao o plano antigo nem o template herdado.
2. Sempre que endpoint, filtro, shape de erro, setup ou validacao mudarem, revisar o `README.md`.
3. Nao afirmar aderencia total a `.doc` sem confrontar o estado atual do codigo com os arquivos relevantes.
4. Quando a `.doc` misturar contratos fora do escopo material do repositorio, registrar explicitamente a diferenca entre:
   - aderencia literal ao documento
   - aderencia ao escopo implementado
5. Se houver conflito entre instrucoes do usuario e a `.doc`, explicitar a decisao e a perda assumida.
6. Mensagens de commit, PR e README devem evitar inflar conclusoes; declarar apenas o que foi efetivamente provado.

## Regras Anti-Alucinacao

1. Nao declarar “production-ready”, “main-ready”, “100% aderente”, “sem falso positivo” ou equivalente sem evidencia executada na sessao atual.
2. Nao inferir que um comportamento funciona em PostgreSQL porque funcionou em `InMemory`.
3. Nao inferir que a documentacao esta correta porque o nome parece coerente; confrontar com codigo e testes.
4. Nao inferir que o escopo inclui tudo em `.doc`; verificar se o repositorio, README, controllers e testes realmente cobrem aquilo.
5. Nao usar resultado de comando antigo como prova apos refactor estrutural, alteracao de contrato ou mudanca de provider.
6. Quando uma afirmacao depender de interpretacao, separar explicitamente:
   - fato observado
   - inferencia
   - risco residual
7. Quando houver mais de uma leitura plausivel de requisito, registrar a ambiguidade antes de concluir aderencia total.
8. **Nao inferir que eventos de dominio foram entregues** apenas porque o handler retornou sem erro; verificar se `DequeueDomainEvents` e chamado **apos** `SaveChangesAsync` e nao antes.
9. **Nao inferir cobertura de regra de negocio por ter ao menos um caso feliz**: limites de tier (ex: 3, 4, 9, 10, 20, 21 para descontos por quantidade) exigem casos de teste explicitos para cada fronteira, nao apenas um valor por tier.
10. **Nao tratar warning de codigo nao referenciado como ruido**: codigo sem chamadores e sintoma de feature incompleta, refactor abandonado ou excecao de dominio sem fio. Investigar antes de ignorar.
11. **Nao declarar funcionalidade de listagem/filtro pronta apenas com testes rapidos**: `DeveloperStore.Unit` e `DeveloperStore.Functional` verdes nao provam que listagens, filtros, ordenacao ou paginacao funcionam em PostgreSQL. A prova canonica e `DeveloperStore.BDD` ou `DeveloperStore.Postgres` verde.
12. **Nao tratar `.doc` como especificacao viva quando contradiz o codigo/README atual**: se `.doc` refletir template legado (ex: stack, frameworks, escopo fora do repositorio), o codigo/README atual prevalece para o escopo implementado e a divergencia deve ser declarada explicitamente.
13. **Nao ignorar sinais de governanca quebrada**: chamadas de DI duplicadas (ex.: `AddControllers`, `AddHealthChecks` registrados em mais de um lugar), comentarios em `.cs` e codigo nao referenciado sao sintomas de incompletude e devem ser tratados, nao mascarados como estilo ou ruido.

## Regras por Linguagem

Para tarefas que alteram codigo, carregar a skill:

- `.agents/skills/agent-governance/SKILL.md`

Para tarefas de correcao de bugs com remediacao e teste de regressao, carregar tambem:

- `.agents/skills/bugfix/SKILL.md`

## Referencias

Cada skill lista suas proprias referencias em `references/` com gatilhos de carregamento no respectivo `SKILL.md`. Nao duplicar a listagem aqui — consultar o SKILL.md da skill ativa para saber quais referencias carregar e em que condicao.

## Notas por Ferramenta

- **Claude Code**: skills pre-carregadas via `.claude/skills/`, hooks via `.claude/hooks/`, agents delegados via `.claude/agents/`.
- **Gemini CLI**: commands em `.gemini/commands/*.toml` apontam para skills canonicas. Sem hooks ou agents nativos — o modelo deve seguir as instrucoes procedurais do SKILL.md carregado.
- **Codex**: le `AGENTS.md` como instrucao de sessao. Entradas em `.codex/config.toml` sao metadados para `upgrade.sh`, nao spec oficial do Codex CLI. O agente deve seguir as instrucoes de `AGENTS.md` para descobrir e carregar skills.
- **Copilot**: `.github/copilot-instructions.md` como instrucao principal. `.github/agents/` sao wrappers. Sem hooks nativos — compliance depende do modelo seguir as instrucoes.

### Matrix de Enforcement

| Capacidade | Claude Code | Gemini CLI | Codex | Copilot |
|---|---|---|---|---|
| Carga base automatica | hook PreToolUse | procedural | procedural | procedural |
| Protecao de governanca | hook PostToolUse | procedural | procedural | procedural |
| Skills pre-carregadas | sim (symlinks) | sim (commands) | nao | sim (agents) |
| Enforcement programatico | sim (hooks) | nao | nao | nao |
| Validacao de evidencias | script | procedural | procedural | procedural |

Ferramentas sem enforcement programatico dependem do modelo seguir instrucoes procedurais. A compliance nessas ferramentas e best-effort.

## Economia de Contexto

Carregar o minimo necessario para a tarefa reduz custo de tokens em 35-50%:

| Complexidade | Criterio | O que carregar |
|---|---|---|
| `trivial` | Rename, typo, import, formatacao | Apenas AGENTS.md |
| `standard` | Bug fix, novo metodo, refactor local | AGENTS.md + TL;DR das references afetadas |
| `complex` | Nova feature, interface publica, migracao | AGENTS.md + referencias completas |

- Classificar a complexidade **antes** de carregar qualquer referencia.
- Quando a reference tiver bloco `<!-- TL;DR ... -->`, preferir o TL;DR ao documento completo em tarefas standard.
- Override explicito via `--complexity=<nivel>` prevalece sobre classificacao automatica.

## Validacao

Antes de concluir uma alteracao:

Seguir Etapa 4 de `.agents/skills/agent-governance/SKILL.md` como base canonica.

Adicionalmente neste projeto:

1. Sempre rodar `dotnet build` para alteracoes de codigo.
2. Rodar ao menos a suite de testes diretamente afetada.
3. Se alterar PRDs em `.specs`, rodar `ai-spec sync-spec-hash` e `ai-spec check-spec-drift`.
4. Se alterar contrato HTTP, validar payloads de erro e sucesso.
5. Nao considerar valido teste executado com `--no-build` como evidencia unica apos refactor estrutural.
6. Se alterar persistencia, validar tambem comportamento com provider relacional real quando houver impacto de traducao, ordenacao, filtros, constraints ou migrations.
7. Se alterar health checks, configuracao ou bootstrap, validar readiness/liveness e politica de migracao explicitamente.
8. Se alterar `README.md` ou contrato de uso, validar se a documentacao continua consistente com endpoints, defaults e exemplos reais.
9. Ao concluir, preferir reportar tambem riscos residuais conhecidos, especialmente quando houver divergencia nao resolvida com `.doc`.
10. Antes de concluir alteracao em listagem, filtro, paginacao, ordenacao ou contrato de erro, executar `make verify-full` (ou equivalente) e garantir que `DeveloperStore.BDD` e `DeveloperStore.Postgres` estejam verdes. Se nao for possivel executa-los, registrar o impedimento e nao declarar a funcionalidade pronta.
11. Se alterar middleware, filter ou shape de erro, validar o payload JSON bruto para erros 500, 422 e 404, garantindo camelCase e consistencia entre todos os caminhos de erro da API.

## Restricoes

1. Nao inventar contexto ausente.
2. Nao assumir versao de linguagem, framework ou runtime sem verificar.
3. Nao alterar comportamento publico sem deixar isso explicito.
4. Nao usar exemplos como copia cega; adaptar ao contexto real.
5. Nao flexibilizar regras de modelagem para “agilizar” quando isso degradar o dominio.
6. Nao declarar aderencia total ao `.doc` quando houver conflito conhecido nao resolvido entre stack, contrato HTTP ou frameworks listados.
7. Quando houver conflito entre instrucoes de usuario e `.doc`, registrar explicitamente o conflito e a decisao tomada.
8. Nao apagar ou mascarar sinais de risco tecnico no texto final apenas para parecer pronto.
9. Nao tratar template legado como prova de requisito implementado; considerar apenas codigo, testes e documentacao atuais.

### Controle de profundidade de invocacao

- Skills que invocam outros skills (execute-task, refactor) devem verificar profundidade via `scripts/lib/check-invocation-depth.sh`.
- Limite padrao: 2 niveis. Configuravel via `AI_INVOCATION_MAX`.
- Variaveis de ambiente: `AI_INVOCATION_DEPTH` (corrente), `AI_INVOCATION_MAX` (limite).
