# Arquitetura do ERP Suite

Este projeto foi organizado como uma aplicacao full stack de ERP para demonstrar dominio de API REST, autenticacao, banco relacional e frontend administrativo.

## Backend

- ASP.NET Core Web API.
- Entity Framework Core com SQL Server.
- Autenticacao JWT Bearer.
- Entidades separadas por arquivo em `Models/`.
- DTOs para proteger a API de acoplamento direto com entidades.
- Inicializacao de banco em `DatabaseInitializer`.
- Seed idempotente em `DataSeeder`.
- Servico financeiro para gerar contas a pagar/receber a partir de compras e vendas.
- Endpoint de saude em `/api/health`.
- Swagger UI em `/swagger`, configurado com autorizacao JWT Bearer.
- Auditoria automatica de operacoes autenticadas em `AuditLogs`.
- RabbitMQ com exchange `erp.events`, eventos de integracao e consumidor de estoque baixo.
- Criacao automatica de banco habilitada para demonstracao local; para producao, evoluir para migrations versionadas.

## Frontend

- Angular standalone.
- Rotas protegidas por guarda de autenticacao.
- Interceptor HTTP para enviar o token JWT.
- Telas administrativas para dashboard, materiais, estoque, parceiros, compras, vendas e usuarios.

## Fluxo principal

1. Usuario faz login em `/api/auth/login`.
2. API retorna JWT.
3. Angular salva o token e envia `Authorization: Bearer <token>` nas chamadas seguintes.
4. Endpoints protegidos validam o token e aplicam regras por perfil.
5. Operacoes de compra, venda e estoque gravam movimentos no SQL Server.
6. Operacoes relevantes publicam eventos no RabbitMQ para processamento assincrono.
7. Recebimento de compra gera conta a pagar; expedicao de venda gera conta a receber.
8. O consumidor de estoque baixo recebe eventos de movimentacao e registra auditoria quando o saldo fica abaixo do minimo.

## Pontos para apresentar

- A API separa entidade de contrato usando DTOs.
- O estoque e calculado por movimentos, nao por campo editado manualmente.
- Recebimento de compra gera entrada de estoque.
- Expedicao de venda gera saida de estoque.
- O financeiro e integrado ao processo operacional, evitando lancamentos soltos sem origem.
- A baixa financeira registra quem executou o pagamento/recebimento.
- Acoes de escrita sao auditadas com usuario, endpoint, entidade, status e detalhes mascarando dados sensiveis.
- RabbitMQ desacopla a operacao principal de efeitos secundarios, como alerta de estoque baixo.
- O publisher trata indisponibilidade do RabbitMQ sem derrubar a transacao principal do ERP.
- O endpoint `/api/health` mostra status do SQL Server e do RabbitMQ.
- O seed cria usuario admin e dados de exemplo para facilitar a demonstracao.
- O projeto ja esta preparado para evoluir com migrations, outbox pattern ou servicos separados.
