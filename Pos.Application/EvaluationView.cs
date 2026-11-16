namespace Pos.Application;

public sealed record EvaluationView(Guid StudentId, Guid CriterionId, decimal Score, string? Comment,
    string StudentName, string CriterionName);
