namespace Pos.Application;

public sealed record LedgerView(IReadOnlyList<StudentView> Students, IReadOnlyList<CriterionView> Criteria,
    IReadOnlyList<EvaluationView> Evaluations);
