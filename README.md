# ERP Suite

Projeto full stack de ERP com API em C#/.NET, SQL Server, JWT e frontend Angular.

## Modulos incluidos

- Login, cadastro de usuario e perfis com JWT.
- Dashboard operacional com indicadores de materiais, clientes, fornecedores, pedidos e estoque baixo.
- Cadastro e consulta de materiais com categoria, unidade, fornecedor, custo, preco e estoque minimo.
- Estoque com saldos, movimentacoes manuais, recebimento de compras e expedicao de vendas.
- Cadastro de clientes e fornecedores.
- Pedidos de compra com recebimento no almoxarifado.
- Pedidos de venda com baixa de estoque na expedicao.
- Financeiro com contas a pagar, contas a receber, vencimentos, baixa e resumo de fluxo.
- Gestao de usuarios para perfil Admin.
- Auditoria de acoes do usuario com registro de data, usuario, endpoint, entidade, status e detalhes seguros.
- Mensageria com RabbitMQ para publicar eventos de estoque, compras e vendas.

## Estrutura

```text
erp-suite/
  backend/Erp.Api/
    Controllers/     Endpoints REST
    Data/            DbContext, inicializacao do banco e seed
    Dtos/            Contratos de entrada e saida da API
    Messaging/       RabbitMQ, eventos de integracao e consumidor de estoque baixo
    Models/          Entidades de dominio separadas por arquivo
    Services/        JWT, seguranca, financeiro e regras auxiliares
  frontend/          Aplicacao Angular
  docker-compose.yml Opcao para SQL Server e RabbitMQ em container
```

## Rodar localmente

1. Confirme que o SQL Server local esteja rodando.

O projeto esta configurado para usar a instancia padrao instalada na maquina:

```text
Server=localhost;Database=ErpSuiteDb;User Id=sa;Password=1234;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Essa string usa a instancia local padrao do SQL Server com login SQL. Se o SQL Server estiver configurado para porta diferente ou instancia nomeada, ajuste o `Server`.

```text
Server=localhost\SQLEXPRESS;Database=ErpSuiteDb;User Id=sa;Password=1234;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true
```

O banco `ErpSuiteDb` e criado automaticamente quando `Database:AutoCreate` esta `true`.
Os dados iniciais sao inseridos quando `Database:Seed` esta `true`.
Para esta demonstracao, a API usa criacao automatica via Entity Framework. Em producao, o caminho recomendado e trocar para migrations versionadas.

2. Suba o RabbitMQ, se voce ainda nao tiver um RabbitMQ local rodando.

O projeto esta configurado para conectar em:

```text
Host: localhost
Porta: 5672
Usuario: guest
Senha: guest
Exchange: erp.events
Fila: erp.low-stock-alerts
```

Com Docker:

```powershell
docker compose up -d rabbitmq
```

Painel administrativo do RabbitMQ:

```text
http://localhost:15672
```

Login do painel:

```text
guest / guest
```

Se o RabbitMQ estiver fora do ar, a API continua salvando os dados no SQL Server e registra aviso no console. Isso evita perder a operacao principal durante a demonstracao.

3. Rode a API:

```powershell
dotnet restore backend/Erp.Api/Erp.Api.csproj
dotnet run --project backend/Erp.Api/Erp.Api.csproj
```

A API fica em `http://localhost:5242`.

Swagger UI:

```text
http://localhost:5242/swagger
```

Para testar endpoints protegidos, faca login em `/api/auth/login`, copie o `token` retornado e clique em **Authorize** no Swagger. Informe:

```text
Bearer SEU_TOKEN
```

4. Rode o Angular:

```powershell
cd frontend
npm install
npm start
```

O frontend fica em `http://localhost:4200`.

## Usuario inicial

Quando a API conseguir conectar ao SQL Server, ela cria dados iniciais automaticamente:

- Email: `admin@erp.local`
- Senha: `Admin@123`

## Dados de teste

Com `Database:Seed` marcado como `true`, a API tambem insere dados de demonstracao de forma idempotente, ou seja, pode iniciar mais de uma vez sem duplicar os registros principais.

Usuarios de teste, todos com senha `Admin@123`:

- `admin@erp.local` - Admin
- `gerente@erp.local` - Manager
- `comprador@erp.local` - Buyer
- `vendedor@erp.local` - Seller
- `estoque@erp.local` - Stock
- `operador@erp.local` - Operator

Tambem sao criados fornecedores, clientes, materiais, saldos de estoque, pedidos de compra, pedidos de venda, contas a pagar, contas a receber e um registro inicial de auditoria.

Exemplos de registros para procurar nas telas:

- Compra recebida: `PO-DEMO-0001`
- Compra em aberto: `PO-DEMO-0002`
- Venda expedida: `SO-DEMO-0001`
- Venda em aberto: `SO-DEMO-0002`
- Conta a pagar paga: `AP-DEMO-0001`
- Conta a pagar em aberto: `AP-DEMO-0002`
- Conta a receber paga: `AR-DEMO-0001`
- Conta a receber em aberto: `AR-DEMO-0002`

Para inserir esses dados em um banco que ja existe, basta iniciar a API novamente com `Database:Seed` habilitado.

## Configuracoes importantes

- String de conexao: `backend/Erp.Api/appsettings.json`
- Criacao e seed do banco: secao `Database` em `backend/Erp.Api/appsettings.json`
- JWT: secao `Jwt` em `backend/Erp.Api/appsettings.json`
- URL da API no Angular: `frontend/src/environments/environment.ts`
- Documento OpenAPI: `http://localhost:5242/openapi/v1.json`
- Swagger UI: `http://localhost:5242/swagger`
- Saude da API e banco: `http://localhost:5242/api/health`
- Auditoria: `http://localhost:5242/api/audit-logs`
- Financeiro: `http://localhost:5242/api/finance/summary`
- RabbitMQ: secao `RabbitMq` em `backend/Erp.Api/appsettings.json`

## Auditoria

Operacoes de escrita autenticadas (`POST`, `PUT`, `PATCH`, `DELETE`) sao registradas automaticamente na tabela `AuditLogs`.
Campos sensiveis como senha, token, secret e hash sao mascarados.

A consulta fica disponivel para perfis `Admin` e `Manager`:

```text
GET /api/audit-logs
GET /api/audit-logs?entityName=Materials&take=50
```

No Angular, acesse o menu **Auditoria**.

## Financeiro

O modulo financeiro cobre contas a pagar e contas a receber.

Fluxos automaticos:

- Ao receber um pedido de compra, a API gera uma conta a pagar para o fornecedor.
- Ao expedir um pedido de venda, a API gera uma conta a receber para o cliente.
- O dashboard mostra valores em aberto e quantidade de lancamentos vencidos.

Fluxos manuais:

- Criar lancamento avulso de conta a pagar.
- Criar lancamento avulso de conta a receber.
- Baixar lancamento como pago ou recebido.
- Cancelar lancamento em aberto.

Endpoints principais:

```text
GET  /api/finance/summary
GET  /api/finance/payables
GET  /api/finance/receivables
POST /api/finance/entries
POST /api/finance/entries/{id}/settle
POST /api/finance/entries/{id}/cancel
```

No Angular, acesse o menu **Financeiro**.

## RabbitMQ

O RabbitMQ foi usado nos fluxos que combinam melhor com mensageria em um ERP:

- `inventory.stock-movement.created`: publicado quando uma movimentacao de estoque e criada.
- `purchasing.order.received`: publicado quando um pedido de compra e recebido.
- `sales.order.shipped`: publicado quando um pedido de venda e expedido.
- `finance.entry.created`: publicado quando uma conta a pagar ou receber e criada.

A API declara o exchange `erp.events` como `topic`. O consumidor `LowStockAlertConsumer` assina a fila `erp.low-stock-alerts` na routing key `inventory.stock-movement.created`.
Quando o estoque atual fica abaixo do minimo do material, o consumidor registra uma auditoria com a acao `RabbitMq.LowStockDetected`.

Arquivos principais:

- `backend/Erp.Api/Messaging/RabbitMqIntegrationEventPublisher.cs`
- `backend/Erp.Api/Messaging/LowStockAlertConsumer.cs`
- `backend/Erp.Api/Messaging/IntegrationEvents.cs`
- `backend/Erp.Api/Messaging/RabbitMqRoutingKeys.cs`

O arquivo `docker-compose.yml` ficou como opcao caso voce queira subir RabbitMQ ou SQL Server em container. Para usar seu SQL Server local instalado, nao precisa subir o servico `sqlserver`.

Antes de usar em producao, troque a chave JWT e mova segredos para variaveis de ambiente ou um cofre de segredos.
