# 🍷 Pagnan Adega Control

Aplicação **web** desenvolvida para apoiar a operação de **adegas e comércios de bebidas**, reunindo ponto de venda, controle de estoque, compras, financeiro, clientes e relatórios em uma única plataforma.

O projeto foi construído com frontend em **React + TypeScript** e uma API em **.NET 8**, seguindo arquitetura em camadas, separação de responsabilidades, princípios SOLID e persistência local de dados.

---

## 💻 Funcionalidades

### Autenticação e Acesso
* Login com autenticação baseada em JWT
* Renovação de sessão por refresh token
* Controle de acesso por perfil e permissões
* Proteção de rotas no frontend

### Produtos e Estoque
* Cadastro e edição de produtos e categorias
* Código de barras e embalagens
* Controle de preço de custo, preço de venda e estoque mínimo
* Gestão de lotes e movimentações de estoque
* Inventário e transferências entre filiais

### Vendas e Clientes
* Ponto de venda (PDV)
* Consulta de vendas e devoluções
* Cadastro e gestão de clientes
* Reservas e notificações relacionadas

### Compras e Fornecedores
* Cadastro de fornecedores
* Registro e acompanhamento de pedidos de compra
* Recebimento de mercadorias e atualização de estoque

### Financeiro e Gestão
* Controle de caixa, contas a pagar e contas a receber
* Dashboard operacional
* Relatórios gerenciais
* Auditoria de operações
* Configurações da aplicação

---

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas, com frontend separado da API:

```text
AdegaControl_Web
│
├── backend
│   ├── src
│   │   ├── ERP.Adega.Domain           (regras de negócio e entidades)
│   │   ├── ERP.Adega.Application      (casos de uso, DTOs e validações)
│   │   ├── ERP.Adega.Infrastructure   (persistência, identidade e repositórios)
│   │   └── ERP.Adega.API              (API REST, autenticação e endpoints)
│   │
│   └── tests
│       └── ERP.Adega.Domain.Tests     (testes de unidade)
│
└── frontend
    └── erp-adega-web                  (aplicação React)
```

### Responsabilidades

#### Domain
Contém o núcleo da aplicação, sem dependências de infraestrutura:
* Entidades de domínio
* Enums e objetos de valor
* Interfaces de repositório
* Regras e exceções de negócio

#### Application
Orquestra os casos de uso da aplicação:
* Commands e queries com MediatR
* DTOs
* Serviços de aplicação
* Validações com FluentValidation
* Comportamento de auditoria

#### Infrastructure
Implementa recursos externos e persistência:
* Entity Framework Core
* SQLite
* Repositórios e Unit of Work
* Serviços de identidade, JWT e hashing BCrypt
* Seed inicial de dados

#### API
Expõe os recursos do sistema via API REST:
* Controllers para os módulos da aplicação
* Autenticação e autorização JWT
* Swagger para documentação e testes da API
* CORS e middleware global de erros

#### Frontend
Responsável pela interface web:
* React, TypeScript e Vite
* React Router para navegação
* Zustand para estado de autenticação
* Axios para comunicação com a API
* Componentes e estilos modulares

---

## 🛠️ Tecnologias Utilizadas

### Backend
* .NET 8
* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQLite
* MediatR
* FluentValidation
* AutoMapper
* JWT Bearer Authentication
* BCrypt.Net
* Swagger / OpenAPI
* xUnit

### Frontend
* React 18
* TypeScript
* Vite
* React Router
* Zustand
* Axios
* Lucide React
* React Hot Toast
* date-fns

---

## 📂 Principais Recursos

### PDV e Vendas
Registro de vendas, consulta de histórico e suporte a devoluções.

### Estoque
Controle de produtos, lotes, estoque mínimo, inventário, movimentações e transferências entre filiais.

### Compras
Gestão de fornecedores, pedidos de compra e recebimento de mercadorias.

### Financeiro
Organização de caixa, contas a pagar e contas a receber.

### Segurança e Auditoria
Autenticação por JWT, permissões por perfil e registro de operações relevantes.

---

## 🚀 Como Executar

### Pré-requisitos
* .NET 8 SDK
* Node.js 18+ e npm
* Git (opcional, para clonagem)

### Clonar o Projeto

```bash
git clone https://github.com/SEU-USUARIO/PagnanAdegaControl.git
cd PagnanAdegaControl
```

### Executar o Backend

Em um terminal, acesse a pasta `backend` e execute:

```bash
dotnet restore
dotnet run --project src/ERP.Adega.API
```

A API será iniciada em `http://localhost:5000`. A documentação Swagger estará disponível em:

```text
http://localhost:5000/swagger
```

### Executar o Frontend

Em outro terminal, acesse a pasta `frontend/erp-adega-web` e execute:

```bash
npm install
npm run dev
```

Abra `http://localhost:5173` no navegador. Durante o desenvolvimento, o Vite encaminha as chamadas `/api` para o backend em `http://localhost:5000`.

### Executar os Testes

```bash
dotnet test backend/tests/ERP.Adega.Domain.Tests
```

### Credenciais de Demonstração

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@adega.com` | `admin123` |

> Na primeira execução, o banco SQLite é criado automaticamente e recebe dados de demonstração, incluindo empresa, filial, produtos, estoque, fornecedores, perfil e usuário administrador.

---

## 🗄️ Banco de Dados

O sistema utiliza **SQLite** para armazenamento local. O arquivo `erp_adega.db` é criado automaticamente na execução da API, conforme a connection string configurada em `appsettings.json`.

O processo de seed inicial carrega:

* Empresa e filial de demonstração
* Categorias de bebidas
* Fornecedores de exemplo
* Produtos, lotes e estoque inicial
* Perfil de administrador e credenciais de acesso

Em produção, altere a chave JWT padrão e as origens permitidas de CORS no arquivo de configuração da API.

---

## 📈 Roadmap

* [ ] Migrations do Entity Framework Core
* [ ] Cobertura ampliada de testes automatizados
* [ ] Integração com meios de pagamento
* [ ] Exportação avançada de relatórios
* [ ] Dashboard com indicadores e gráficos mais detalhados
* [ ] Integração com banco de dados gerenciado em produção

---

## 👨‍💻 Autor

**Felipe Pagnan**

Software Engineer especializado em desenvolvimento .NET, arquitetura de software e aplicações multiplataforma.

LinkedIn:  
https://www.linkedin.com/in/felipe-pagnan/

---

## 📄 Licença

Este projeto está sob a licença Pagnan.  
Sinta-se à vontade para estudar, utilizar e contribuir com melhorias.
