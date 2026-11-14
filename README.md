# Chef Express — Plataforma de Gastronomia Sob Demanda

TCC Devs2Blu. Plataforma que conecta clientes a profissionais de gastronomia autônomos (eventos e marmitas semanais): busca com filtros, negociação com proposta, pagamento simulado e avaliação.

| Camada     | Tecnologia                                            | Pasta                      | Status              |
|------------|-------------------------------------------------------|----------------------------|---------------------|
| Back-end   | C# / .NET 10 (API REST) + EF Core 9 (Pomelo)          | [`backend/`](backend/)     | 🟡 fundação pronta  |
| Banco      | MariaDB 11.8                                          | `docker-compose.yml`       | ✅                  |
| Front-end  | Angular                                               | [`frontend/`](frontend/)   | ⏳ a criar          |
| Infra      | Docker Compose + GitHub Actions (K8s planejado)       | [`infra/`](infra/)         | 🟡 parcial          |

---

## Como rodar

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (qualquer 10.0.1xx ou superior)
- [Docker](https://docs.docker.com/get-docker/) + Docker Compose (ou um MariaDB local)

> **Arch Linux:** além do pacote `dotnet-sdk`, instale `aspnet-runtime` **e** `aspnet-targeting-pack`.
> Sem o targeting pack o build falha com `NETSDK1226: Prune Package data not found ... Microsoft.AspNetCore.App`.

### Opção A — tudo no Docker

```bash
docker compose up -d --build
```

- API: http://localhost:5094
- Documentação interativa (Scalar): http://localhost:5094/scalar
- MariaDB: `localhost:3306` (usuário/senha `chefexpress`)

As *migrations* são aplicadas automaticamente quando a API sobe.

### Opção B — banco no Docker, API local (recomendado para desenvolver)

```bash
docker compose up -d db
cd backend
dotnet tool restore                       # instala o dotnet-ef (versão fixada no projeto)
dotnet run --project src/ChefExpress.Api  # abre o /scalar no navegador
```

No ambiente `Development` a API já usa `localhost:3306` e aplica as migrations sozinha
(veja `backend/src/ChefExpress.Api/appsettings.Development.json`).

### Testes

```bash
cd backend
dotnet test
```

---

## Arquitetura do back-end

Usamos **Clean Architecture "enxuta"** (na prática, *Ports & Adapters* / Hexagonal):
as regras de negócio ficam no centro e não conhecem banco, HTTP ou bibliotecas externas.

```
backend/
├── src/
│   ├── ChefExpress.Domain/          # Entidades e regras puras (User, ProfessionalProfile, Skill, AvailableDate)
│   ├── ChefExpress.Application/     # Casos de uso, DTOs, validação e INTERFACES (ports)
│   ├── ChefExpress.Infrastructure/  # Implementações (adapters): EF Core + MariaDB, migrations
│   └── ChefExpress.Api/             # Controllers, CORS, OpenAPI, health checks
└── tests/
    └── ChefExpress.UnitTests/
```

**Regra de dependência:** `Api → Application → Domain` e `Infrastructure → Application`.
O `Domain` não depende de ninguém; o `Application` só conhece interfaces (ex.: `IUnitOfWork`, repositórios).

Por que assim:

- **Testável:** casos de uso podem ser testados sem banco, usando *fakes* das interfaces.
- **Troca de tecnologia sem dor:** um repositório pode ganhar outra implementação (ex.: MongoDB)
  na `Infrastructure` sem mexer em `Application` e `Domain`.
- **Sem cerimônia desnecessária:** nada de MediatR/CQRS; um *service* por módulo é suficiente para o MVP.

### Convenções

- **Código em inglês**, mensagens para o usuário final em **português**.
- Erros esperados (validação, não encontrado, conflito) **não são exceções**: o caso de uso retorna
  `Result<T>` com um `Error` (`Application/Common`), e o controller converte com `this.ToProblem(error)`
  para [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) com o status HTTP certo.
  O campo `code` (ex.: `Profile.NotFound`) é estável e pode ser usado pelo Angular.
- Exceções inesperadas viram `500` em ProblemDetails (`GlobalExceptionHandler`), sem vazar detalhes internos.
- Datas da agenda são **dias fechados** (`DateOnly` → coluna `date`), conforme o Risco 2 do planejamento.
- Configuração sensível vem de **variáveis de ambiente** (`ConnectionStrings__Default`, ...).

### Como adicionar um módulo

1. `Domain`: métodos de negócio na entidade.
2. `Application/<Modulo>/`: DTOs, interface + implementação do *service*, e a interface do repositório em `Abstractions/Persistence`. Registrar em `Application/DependencyInjection.cs`.
3. `Infrastructure/Persistence/Repositories/`: implementação do repositório. Registrar em `Infrastructure/DependencyInjection.cs`.
4. `Api/Controllers/`: controller fino que só chama o service.
5. Se mudou o banco, crie a migration:
   ```bash
   cd backend
   dotnet ef migrations add NomeDaMudanca -p src/ChefExpress.Infrastructure -s src/ChefExpress.Api -o Persistence/Migrations
   ```

---

## Banco de dados (migration `InitialCreate`)

| Tabela                  | Conteúdo                                                                 |
|-------------------------|--------------------------------------------------------------------------|
| `users`                 | Nome, e-mail (único), data de criação                                    |
| `professional_profiles` | 1:1 com usuário — descrição, cidade/UF, bairro (opcional), preço base    |
| `skills`                | Habilidades gastronômicas (lista inicial já inserida pela migration)     |
| `professional_skills`   | N:N entre profissionais e habilidades                                    |
| `available_dates`       | Agenda: um registro por dia livre, com `IsBooked` para o bloqueio após pagamento |

Senha e perfil (Cliente/Profissional) **não** estão na tabela `users` ainda: entram com a tarefa 4 (autenticação), em uma migration própria.
Os índices para a busca com filtros (tarefa 9) — cidade/UF, preço e data — já estão criados.

---

## Endpoints disponíveis

| Método | Rota            | Descrição                            |
|--------|-----------------|--------------------------------------|
| GET    | `/health/live`  | A API está no ar                     |
| GET    | `/health/ready` | A API consegue falar com o MariaDB   |
| GET    | `/scalar`       | Documentação interativa (só em dev)  |

**CORS:** liberado para `http://localhost:4200` (configurável em `Cors:AllowedOrigins`).

---

## Backlog — Semana 1 (Fundação e Acesso)

| # | Tarefa                                      | Responsável   | Status |
|---|---------------------------------------------|---------------|--------|
| 1 | Projeto Back-end (C#) e conexão com MariaDB | Gabriel       | ✅     |
| 2 | Tabelas iniciais via migrations             | Gabriel       | ✅     |
| 3 | Projeto Front-end (Angular)                 | Lucas         | ⏳     |
| 4 | Cadastro e Login no Back-end                | Marco         | ⏳     |
| 5 | Telas de Cadastro e Login (Angular) + integração | Lucas / Marco | ⏳ |

### Notas para a tarefa 4 (autenticação)

- Adicionar à entidade `User` os campos de senha (hash) e perfil (Cliente/Profissional) e gerar uma nova migration.
- Registrar autenticação/autorização em `Api/Extensions/ServiceCollectionExtensions.cs` e os middlewares em `Program.cs` (há um comentário marcando o lugar, depois do `UseCors`).
- Para profissionais, `ProfessionalProfile.CreateFor(user)` cria o perfil vazio que será preenchido na tela "Meu Perfil e Agenda".

## Pontos ainda em aberto (`[A VERIFICAR]` no planejamento)

- Nome oficial do app (provisório: **Chef Express**).
- Percentual e regra da taxa de serviço.
- Lista definitiva de habilidades (a atual está em `SkillConfiguration.cs`).
- Granularidade do filtro de local (cidade/UF já existe; bairro está como campo opcional).
- MongoDB para perfis — por ora tudo no MariaDB (reviews, chat e pedidos são fortemente relacionais).
