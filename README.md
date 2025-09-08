# PagueVeloz Transaction Processor

Sistema de processamento de transações financeiras desenvolvido com .NET 9, seguindo os princípios de Clean Architecture, DDD e padrões enterprise.

## 📋 Índice

- [Arquitetura](#-arquitetura)
- [Tecnologias](#-tecnologias)
- [Pré-requisitos](#-pré-requisitos)
- [Instalação](#-instalação)
- [Configuração](#-configuração)
- [Executando o Projeto](#-executando-o-projeto)
- [API Documentation](#-api-documentation)
- [Testes](#-testes)
- [Docker](#-docker)
- [Estrutura do Projeto](#-estrutura-do-projeto)

## Arquitetura

O projeto segue os princípios de **Clean Architecture** com as seguintes camadas:

- **Domain**: Entidades, Value Objects, Interfaces e Eventos de Domínio
- **Application**: Commands, Handlers, DTOs, Validações e Serviços
- **Infrastructure**: Implementação de Repositórios, Entity Framework, Event Publishing
- **API**: Controllers, Middleware, Configuração e Endpoints REST

### Padrões Implementados

- **CQRS** com MediatR para separação de comandos e queries
- **Repository Pattern** para abstração de acesso a dados
- **Unit of Work** para gerenciamento de transações
- **Domain Events** para comunicação assíncrona
- **Retry Pattern** com Polly para resiliência
- **Idempotência** via reference_id único

## Tecnologias

- **.NET 9.0**
- **C# 13**
- **Entity Framework Core 9**
- **SQL Server**
- **MediatR** - CQRS Pattern
- **AutoMapper** - Object Mapping
- **FluentValidation** - Validação de Dados
- **Polly** - Resiliência e Retry
- **MassTransit** - Message Bus
- **Serilog** - Logging Estruturado
- **Swagger/OpenAPI** - Documentação de API
- **xUnit** - Testes Unitários
- **FluentAssertions** - Assertions em Testes
- **Docker** - Containerização

## Pré-requisitos

- Visual Studio 2022 (17.8+) ou VS Code
- .NET 9 SDK
- SQL Server (LocalDB, Express ou Developer)
- Docker Desktop
- Git

## Instalação

### 1. Clonar o Repositório

```bash
git clone https://github.com/schimitegusta/PagueVelozTransactionProcessor.git
cd pagueveloz-transaction-processor
```

### 2. Restaurar Pacotes

```bash
dotnet restore
```

### 3. Configurar Banco de Dados

Este projeto utiliza SQL Server via Docker para facilitar o desenvolvimento e garantir consistência entre ambientes:

Passo 1: Criar e executar o container SQL Server
```
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=suaSenha123" -p 1433:1433 --name sqlserver-pagueveloz -d mcr.microsoft.com/mssql/server:2022-latest
```

Passo 2: Verificar se o container está rodando
```
bashdocker ps
```
Deve mostrar o container 'sqlserver-pagueveloz' com status 'Up'

Passo 3: Configurar a Connection String

Edite o arquivo appsettings.json na pasta src/PagueVeloz.API:
```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PagueVelozDB;User Id=sa;Password=suaSenha123;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

Passo 4: Criar o banco de dados e executar Migrations
Via CLI do .NET
```
cd src/PagueVeloz.API
dotnet ef database update
```

## Executando o Projeto

### Via Visual Studio

1. Abra a solução `PagueVeloz.TransactionProcessor.sln`
2. Defina `PagueVeloz.API` como projeto de inicialização
3. Pressione F5 para executar

## Documentação da API

### Endpoints Principais

#### 1. Criar Conta

```json
POST /api/accounts
Content-Type: application/json

{
  "client_id": "d35eaadd-eae2-45a6-aac3-2096fa1eeb92",
  "initial_balance": 100000,
  "credit_limit": 50000,
  "currency": "BRL"
}
```
Para testes é importante informar um GIUD real para o parâmetro client_id, pode utilizar alguns dos seguintes:
```
d35eaadd-eae2-45a6-aac3-2096fa1eeb92
5f94e4b2-c780-4988-b6fd-3ff754977ddf
af5000e9-c84d-4ced-b479-f7b951390de1
05339459-7ac0-4ece-aac7-9edb2747bb44
```

**Response:**
```json
{
  "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
  "client_id": "d35eaadd-eae2-45a6-aac3-2096fa1eeb92",
  "balance": 100000,
  "reserved_balance": 0,
  "available_balance": 100000,
  "credit_limit": 50000,
  "currency": "BRL",
  "status": "active",
  "created_at": "2025-01-07T20:00:00Z"
}
```

#### 2. Processar Transação

```json
POST /api/transactions
Content-Type: application/json

{
  "operation": "credit",
  "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
  "amount": 10000,
  "currency": "BRL",
  "reference_id": "TXN-001",
  "metadata": {
    "description": "Depósito inicial"
  }
}
```

**Response:**
```json
{
  "transaction_id": "txn-uuid",
  "status": "success",
  "balance": 110000,
  "reserved_balance": 0,
  "available_balance": 110000,
  "timestamp": "2025-01-07T20:05:00Z",
  "error_message": null
}
```

### Tipos de Operações

| Operação | Descrição | Validações |
|----------|-----------|------------|
| `credit` | Adiciona valor à conta | Nenhuma |
| `debit` | Remove valor da conta | Saldo disponível + limite |
| `reserve` | Reserva valor para captura futura | Saldo disponível |
| `capture` | Confirma uma reserva | Saldo reservado |
| `reversal` | Reverte uma transação anterior | Transação original existe |
| `transfer` | Transfere entre contas | Conta destino existe |

### Exemplos de Uso

#### Operação de Crédito
```json
POST /api/transactions
Content-Type: application/json

{
    "operation": "credit",
    "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
    "amount": 50000,
    "currency": "BRL",
    "reference_id": "DEP-001"
}
```

#### Operação de Débito
```json
POST /api/transactions
Content-Type: application/json
{
    "operation": "debit",
    "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
    "amount": 20000,
    "currency": "BRL",
    "reference_id": "PAY-001"
}
```

#### Operação de Reversão
```json
POST /api/transactions
Content-Type: application/json

{
  "operation": "reversal",
  "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
  "reference_id": "RES-001",
  "metadata": {
    "description": "reversal",
    "original_reference_id" : "PAY-001"
    }
}
```

#### Reserva e Captura
```json
# Reservar
POST /api/transactions
Content-Type: application/json
{
    "operation": "reserve",
    "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
    "amount": 30000,
    "currency": "BRL",
    "reference_id": "RES-001"
}

# Capturar
POST /api/transactions
Content-Type: application/json
{
    "operation": "capture",
    "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
    "amount": 30000,
    "currency": "BRL",
    "reference_id": "CAP-001"
}
```

#### Transferência
```json
POST /api/transactions
Content-Type: application/json

{
  "operation": "transfer",
  "account_id": "1a28cf32-8540-4913-8dc8-4f2362883abc",
  "amount": 200,
  "currency": "BRL",
  "reference_id": "TRA-001",
  "metadata": {
    "description": "transfer"
    },
  "target_account_id": "6916D862-C579-4B05-9E15-BA27E88AE108"
}
```

#### Processamento em Lote
```json
POST /api/transactions
Content-Type: application/json
[
    {
      "operation": "credit",
      "account_id": "b4787c2e-50f5-4591-8dd9-fac453e88dbe",
      "amount": 10000,
      "currency": "BRL",
      "reference_id": "BATCH-001"
    },
    {
      "operation": "debit",
      "account_id": "ACC-001",
      "amount": 5000,
      "currency": "BRL",
      "reference_id": "BATCH-002"
    }
]
```

## Testes

### Executar Todos os Testes

```bash
dotnet test
```

## Docker

### Dockerfile

## Estrutura do Projeto

```
PagueVeloz.TransactionProcessor/
├── src/
│   ├── PagueVeloz.Domain/          # Camada de Domínio
│   │   ├── Entities/               # Entidades do domínio
│   │   ├── Enums/                  # Enumerações
│   │   ├── Events/                 # Eventos de domínio
│   │   ├── Interfaces/             # Contratos/Interfaces
│   │   └── ValueObjects/           # Objetos de valor
│   │
│   ├── PagueVeloz.Application/     # Camada de Aplicação
│   │   ├── Commands/               # Comandos CQRS
│   │   ├── Handlers/               # Command handlers
│   │   ├── DTOs/                   # Data Transfer Objects
│   │   ├── Services/               # Serviços de aplicação
│   │   └── Validators/             # Validadores
│   │
│   ├── PagueVeloz.Infrastructure/  # Camada de Infraestrutura
│   │   ├── Data/                   # DbContext e configurações
│   │   ├── Repositories/           # Implementação dos repositórios
│   │   ├── Services/               # Serviços de infraestrutura
│   │   └── Migrations/             # Migrations do EF Core
│   │
│   └── PagueVeloz.API/             # Camada de Apresentação
│       ├── Controllers/            # Controllers da API
│       ├── Middleware/             # Middlewares customizados
│       ├── Extensions/             # Extension methods
│       └── Program.cs              # Entry point
│
├── tests/
│   ├── PagueVeloz.Tests.Unit/      # Testes unitários
│   └── PagueVeloz.Tests.Integration/ # Testes de integração
│
├── docker-compose.yml
├── Dockerfile
└── README.md
```

## Segurança

- **Validação de Entrada**: Todas as entradas são validadas usando FluentValidation
- **SQL Injection**: Prevenido através do uso de Entity Framework Core
- **Concorrência**: Implementado controle otimista com RowVersion
- **Rate Limiting**: Limite de 100 requisições por minuto
- **Idempotência**: Garantida através de reference_id único

### Métricas
- Transações processadas por operação
- Tempo de processamento
- Taxa de erros
- Disponibilidade do sistema

## Decisões Técnicas

### Clean Architecture
- Separação clara de responsabilidades
- Testabilidade aumentada
- Independência de frameworks
- Facilita manutenção e evolução

### Por que CQRS com MediatR?
- Desacoplamento entre camadas
- Facilita implementação de cross-cutting concerns
- Melhor organização do código
- Preparado para Event Sourcing futuro

### Por que Entity Framework Core?
- ORM maduro e bem suportado
- Migrations automáticas
- LINQ para queries type-safe
- Suporte nativo para SQL Server

### Por que Polly?
- Implementação robusta de retry patterns
- Circuit breaker para falhas em cascata
- Timeout policies
- Fallback strategies