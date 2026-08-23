# 🍷 ERP Adega

**Sistema de gestão profissional para adegas e comércio de bebidas.**

Controle completo de estoque (FEFO, lotes, validade, embalagens), compras, vendas/PDV, financeiro, fiscal, reservas, inventário, auditoria e multiempresa.

---

## Estrutura do Projeto

```
erp-adega/
│
├── backend/                          ← .NET 8 / C# — Clean Architecture + DDD
│   ├── src/
│   │   ├── ERP.Adega.Domain/        → Entidades, regras de negócio, invariantes
│   │   ├── ERP.Adega.Application/   → Commands, Queries (CQRS), DTOs, validações
│   │   ├── ERP.Adega.Infrastructure/→ EF Core, PostgreSQL, JWT, repositórios
│   │   └── ERP.Adega.API/           → Controllers REST, middleware, Swagger
│   ├── tests/
│   │   └── ERP.Adega.Domain.Tests/  → Testes unitários do domínio
│   └── ERP.Adega.sln
│
├── frontend/                         ← React 18 + TypeScript + Vite
│   └── erp-adega-web/
│       ├── src/
│       │   ├── components/           → Design System (Button, Input, Badge, Sidebar)
│       │   ├── pages/                → Login, Dashboard, Produtos, Estoque...
│       │   ├── services/             → API client (Axios + JWT)
│       │   ├── store/                → Estado global (Zustand)
│       │   ├── types/                → TypeScript types (espelham DTOs do backend)
│       │   └── styles/               → Tokens do Design System + CSS global
│       ├── package.json
│       └── vite.config.ts
│
├── docs/                             → Documentação do projeto
│   └── escopo-geral.pdf
│
└── README.md                         ← Você está aqui
```

---

## Pré-requisitos

| Ferramenta    | Versão  | Para quê                  |
|---------------|---------|---------------------------|
| .NET SDK      | 8.0+    | Backend                   |
| PostgreSQL    | 16+     | Banco de dados            |
| Node.js       | 20+     | Frontend                  |
| npm           | 10+     | Gerenciador de pacotes    |

---

## Setup Rápido

### 1. Banco de Dados

```sql
CREATE DATABASE erp_adega;
```

### 2. Backend

```bash
cd backend
dotnet restore

# Configurar (editar se necessário)
# backend/src/ERP.Adega.API/appsettings.json
#   → ConnectionString do PostgreSQL
#   → Secret do JWT (mínimo 32 caracteres)

cd src/ERP.Adega.API
dotnet run

# API: https://localhost:5001
# Swagger: https://localhost:5001/swagger
```

### 3. Frontend

```bash
cd frontend/erp-adega-web
npm install
npm run dev

# App: http://localhost:5173
# Proxy automático /api → backend
```

### 4. Testes

```bash
cd backend
dotnet test
```

---

## Stack Técnica

**Backend:**  .NET 8, C# 12, EF Core 8, PostgreSQL 16, MediatR, FluentValidation, JWT Bearer

**Frontend:** React 18, TypeScript, Vite, Zustand, Axios, Lucide Icons, CSS Modules

**Padrões:**  Clean Architecture, DDD, CQRS, Repository, Unit of Work, Result Pattern

---

## Regras de Negócio Críticas

| ID     | Regra                                                  |
|--------|--------------------------------------------------------|
| RN-001 | Estoque não pode ficar negativo                        |
| RN-002 | Físico, reservado e disponível são conceitos distintos |
| RN-003 | Toda alteração de estoque gera movimentação rastreável |
| RN-004 | Venda utiliza apenas estoque disponível                |
| RN-005 | Unidade base é a unidade comercializável               |
| RN-006 | Produto fechado não pode ser fracionado                |
| RN-007 | Quantidade por embalagem é configurável por produto    |
| RN-008 | Validade é vinculada ao lote                           |
| RN-009 | FEFO prioriza lote com vencimento mais próximo         |
| RN-011 | Operações críticas geram auditoria                     |
| RN-013 | Reserva reduz estoque disponível                       |

---

## Roadmap

- [x] **Fase 1** — Fundação (domínio, EF Core, JWT, API, Design System, frontend base)
- [ ] **Fase 2** — Motor de Estoque (FEFO completo, alertas, movimentações)
- [ ] **Fase 3** — Vendas e PDV (código de barras, fardo + unidade, pagamentos)
- [ ] **Fase 4** — Compras (pedidos, aprovação, recebimento, conferência)
- [ ] **Fase 5** — Financeiro e Caixa (contas, taxas, conciliação)
- [ ] **Fase 6** — Complementos (reservas, inventário, transferências, relatórios, fiscal)

---

## Plataformas

| Plataforma | Stack                  | Status        |
|------------|------------------------|---------------|
| Web        | .NET 8 + React         | Em construção |
| Desktop    | Mesma API              | Planejado     |
| Mobile     | SQLite local + API     | Planejado     |

Todas as plataformas compartilham a mesma API, paleta de cores, tipografia e terminologia.

---

*Documento base: ERP Adega — Escopo Geral e Requisitos (19/08/2026)*
