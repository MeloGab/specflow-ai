using SpecFlowAI.Application.Abstractions;
using SpecFlowAI.Domain;

namespace SpecFlowAI.Infrastructure.AI;

public sealed class DemoSpecificationGenerator : ISpecificationGenerator
{
    public string Provider => "Demo";

    public Task<SpecificationGeneration> GenerateAsync(
        string businessDemand,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var specification = new TechnicalSpecification(
            Summary: $"Estruturar tecnicamente a demanda: {businessDemand}",
            BusinessRules:
            [
                "A operação deve validar o estado atual do recurso antes de alterar seus dados.",
                "Regras ainda não explícitas na demanda devem ser confirmadas antes da implementação.",
            ],
            OpenQuestions:
            [
                "Quais perfis estão autorizados a executar a operação?",
                "Quais regras de prazo e cálculo precisam ser formalizadas?",
                "A operação precisa gerar auditoria ou eventos para outros sistemas?",
            ],
            SuggestedApi: new SuggestedApiContract(
                Method: "POST",
                Route: "/api/resources/{id}/actions",
                Purpose: "Executar a ação descrita pela demanda de forma auditável.",
                Validations:
                [
                    "O recurso deve existir.",
                    "A transição solicitada deve ser válida para o estado atual.",
                ],
                Responses:
                [
                    "200: operação concluída",
                    "400: dados ou regra inválida",
                    "404: recurso não encontrado",
                    "409: conflito com o estado atual",
                ]),
            AcceptanceCriteria:
            [
                new(
                    "que o recurso existe e atende às regras da operação",
                    "o usuário envia uma solicitação válida",
                    "a operação é concluída e o novo estado é retornado"),
                new(
                    "que uma regra obrigatória não foi atendida",
                    "o usuário tenta executar a operação",
                    "a alteração é recusada com uma razão clara"),
            ],
            TestScenarios:
            [
                new(
                    "Fluxo principal",
                    "happy-path",
                    "um recurso elegível",
                    "a operação é solicitada",
                    "o resultado é persistido com sucesso"),
                new(
                    "Recurso inexistente",
                    "error",
                    "um identificador desconhecido",
                    "a operação é solicitada",
                    "a API responde 404 e não persiste alterações"),
                new(
                    "Estado incompatível",
                    "business-rule",
                    "um recurso em estado não elegível",
                    "a operação é solicitada",
                    "a API responde 409 e informa a regra violada"),
            ]);

        return Task.FromResult(new SpecificationGeneration(specification, "Demo", "deterministic-template"));
    }
}
