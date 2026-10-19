# SpecFlow AI

PoC para transformar uma demanda de negócio em uma especificação técnica estruturada. O foco não é gerar código automaticamente: é usar IA para explicitar regras, ambiguidades, contratos e critérios verificáveis, mantendo a decisão com o engenheiro.

## Estado do MVP

- Formulário Angular para uma demanda em linguagem natural.
- API ASP.NET Core com validação de entrada.
- Geração estruturada por `Microsoft.Extensions.AI` e `IChatClient`.
- Saída tipada com resumo, regras, dúvidas, API sugerida, critérios de aceite e cenários de teste.
- Repositório em memória com leitura posterior por identificador.
- Seleção por requisição entre `Demo`, `Ollama`, `OpenAI` e `GitHub Copilot`.
- Login GitHub OAuth com token mantido somente na memória do backend.
- OpenAPI em desenvolvimento, testes unitários e contrato HTTP de exemplo.
- Docker Compose para API e frontend, usando o Ollama instalado na máquina.

## Stack validada em 22/09/2026

| Componente | Versão |
|---|---:|
| .NET SDK | 10.0.400 |
| ASP.NET Core | 10.0.11 |
| Microsoft.Extensions.AI | 10.10.0 |
| Microsoft.Extensions.AI.OpenAI | 10.10.0 |
| OllamaSharp | 5.4.30 |
| GitHub Copilot SDK | 1.0.14 (estável) |
| Angular / Angular CLI | 22.1.x / 22.1.8 |
| Node.js | 24.19.0 |
| TypeScript | 6.0.x |

O .NET está fixado em `global.json`; as dependências do frontend estão fixadas pelo `package-lock.json`.

## Arquitetura mínima

```text
Angular ──► escolhe o provedor por requisição
  │ POST /api/specifications { businessDemand, provider }
  ▼
API ──► Application ──► ISpecificationGenerator por nome
              │                    ├─ Demo
              │                    ├─ Ollama ──► Microsoft.Extensions.AI
              │                    ├─ OpenAI ──► Microsoft.Extensions.AI
              │                    └─ GitHub Copilot SDK
              ▼
     ISpecificationRepository ──► memória
```

```text
src/
  SpecFlowAI.Domain/          schema e documento de especificação
  SpecFlowAI.Application/     caso de uso e portas
  SpecFlowAI.Infrastructure/  Microsoft.Extensions.AI, Ollama, OpenAI e repositório
  SpecFlowAI.Api/             composição e endpoints HTTP
tests/
  SpecFlowAI.UnitTests/
web/                          Angular standalone
```

### Decisões

1. **O schema é o contrato central.** O modelo retorna `TechnicalSpecification`, não texto livre. O helper `GetResponseAsync<T>` solicita JSON Schema quando o provedor oferece suporte e a aplicação rejeita uma resposta incompatível.
2. **A escolha é isolada por requisição.** A tela envia o identificador do provedor e o caso de uso resolve a implementação correspondente. Não existe uma chave global que um usuário possa trocar para todos os demais.
3. **Há um checkpoint humano explícito.** O prompt manda registrar ambiguidades em vez de inventar regras, e a interface identifica o resultado como proposta para revisão.
4. **O modo Demo não finge ser IA.** Ele aparece como `Demo / deterministic-template`, permite apresentar o fluxo sem credencial e mantém o mesmo contrato do modo real.
5. **Persistência é deliberadamente simples.** Especificações e sessões GitHub ficam em memória. Ao reiniciar a API, o histórico e o login desaparecem; não há banco nesta PoC.
6. **O Copilot não recebe ferramentas.** O SDK usa o modo `Empty`, uma allowlist vazia, memória e session store desativados. Cada solicitação cria um diretório temporário removido ao final.

## Executar

Em um terminal, inicie a API:

```powershell
dotnet run --project src/SpecFlowAI.Api --urls http://localhost:5130
```

Em outro terminal, inicie o frontend:

```powershell
cd web
npm install
npm start
```

Acesse `http://localhost:4200`. O proxy do Angular encaminha `/api` para a porta 5130.

Na tela, escolha o provedor antes de gerar. `Demo` e `Ollama` funcionam sem conta; `OpenAI` depende de uma chave com créditos; `GitHub Copilot` depende do login GitHub descrito abaixo.

### Usar geração real com OpenAI

Não grave a chave no repositório. No terminal que iniciará a API:

Guarde a chave nos User Secrets do .NET, fora do repositório:

```powershell
dotnet user-secrets set "AI:OpenAI:ApiKey" "sua-chave" --project src/SpecFlowAI.Api
```

Depois, basta iniciar a API normalmente e escolher `OpenAI` na tela.

O modelo é configurável porque disponibilidade e custo variam por conta e ambiente.

### Usar geração local gratuita com Ollama

Instale o Ollama para Windows e baixe um modelo uma única vez:

```powershell
ollama pull llama3.2
```

O ambiente `Development` usa `Ollama / llama3.2` como seleção inicial. Basta iniciar a API normalmente:

```powershell
dotnet run --project src/SpecFlowAI.Api --urls http://localhost:5130
```

O Ollama precisa estar ativo, mas não exige chave nem créditos. A escolha agora é feita diretamente na tela, sem reiniciar a aplicação.

### Usar o GitHub Copilot

Crie uma OAuth App em `GitHub → Settings → Developer settings → OAuth Apps → New OAuth App` com estes valores locais:

```text
Homepage URL:               http://localhost:4200
Authorization callback URL: http://localhost:4200/api/auth/github/callback
```

Guarde o Client ID e o Client Secret nos User Secrets:

```powershell
dotnet user-secrets set "GitHubOAuth:ClientId" "seu-client-id" --project src/SpecFlowAI.Api
dotnet user-secrets set "GitHubOAuth:ClientSecret" "seu-client-secret" --project src/SpecFlowAI.Api
```

Reinicie a API. A opção `GitHub Copilot` mostrará o botão **Entrar com GitHub**. A senha é informada somente no site do GitHub; o token retornado fica na memória do backend e é descartado no logout ou quando a API reinicia.

Cada conta precisa ter acesso ao GitHub Copilot e está sujeita à própria cota. O projeto usa a seleção de modelo `auto`, compatível com o plano Copilot Free; a integração não utiliza uma chave de modelo compartilhada.

## Executar com Docker

O Docker Compose sobe o Angular e a API. Por padrão, a API no container usa o Ollama já instalado no Windows pelo endereço `host.docker.internal`.

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Acesse `http://localhost:4200`. Se quiser usar OpenAI ou GitHub Copilot no Docker, preencha somente o arquivo `.env` local. Esse arquivo está ignorado pelo Git e não deve ser enviado ao repositório.

O OAuth App usa a mesma URL de callback informada acima. Os containers não incluem banco de dados; reiniciar a API encerra a sessão do GitHub.

## Contrato HTTP

```http
POST /api/specifications
Content-Type: application/json

{
  "businessDemand": "Ao cancelar uma apólice antes de 7 dias...",
  "provider": "Ollama"
}
```

O retorno é `201 Created`, inclui o identificador, metadados do gerador e a especificação. `GET /api/specifications/{id}` recupera o documento enquanto a API estiver ativa.

## Validação

```powershell
dotnet build SpecFlowAI.slnx
dotnet test SpecFlowAI.slnx --no-build

cd web
npm run build
npm test -- --watch=false
```

## Próximas evoluções

1. **Persistência real:** substituir somente `ISpecificationRepository` por SQLite/PostgreSQL e adicionar histórico de versões.
2. **Avaliação da saída:** regras determinísticas e uma rubrica para medir completude, rastreabilidade e alucinação antes de aceitar o documento.
3. **RAG:** introduzir uma porta `IRequirementContextProvider` para recuperar glossário, regras e exemplos aprovados; o contexto entra no prompt com origem rastreável.
4. **MCP:** expor ferramentas aprovadas ao `IChatClient` na infraestrutura, com allowlist, autorização e auditoria. O MCP não deve acessar sistemas livremente por decisão do modelo.
5. **Análise de implementação:** criar outro caso de uso que compare especificação, diff e testes, produzindo evidências e lacunas — sem misturar essa responsabilidade com a geração inicial.
6. **Operação:** autenticação, limites, timeout, cancelamento, telemetria com conteúdo sensível desabilitado e política de retenção.

Essas extensões reutilizam o schema e as portas atuais; nenhuma exige reescrever o fluxo do MVP.
