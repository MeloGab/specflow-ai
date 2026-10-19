export interface SpecificationDocument {
  id: string;
  businessDemand: string;
  specification: TechnicalSpecification;
  generatedAtUtc: string;
  generator: string;
  model: string;
  schemaVersion: string;
}

export interface AiProvider {
  id: string;
  name: string;
  description: string;
  dataDestination: string;
  model: string;
  available: boolean;
  requiresAuthentication: boolean;
  authenticated: boolean;
  account: string | null;
  message: string | null;
}

export interface TechnicalSpecification {
  summary: string;
  businessRules: string[];
  openQuestions: string[];
  suggestedApi: SuggestedApiContract;
  acceptanceCriteria: AcceptanceCriterion[];
  testScenarios: TestScenario[];
}

export interface SuggestedApiContract {
  method: string;
  route: string;
  purpose: string;
  validations: string[];
  responses: string[];
}

export interface AcceptanceCriterion {
  given: string;
  when: string;
  then: string;
}

export interface TestScenario extends AcceptanceCriterion {
  name: string;
  type: string;
}
