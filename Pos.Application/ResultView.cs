namespace Pos.Application;

public sealed record ResultView(Guid StudentId, string Name, decimal? Score, bool HasAllEvaluations, int EvaluatedCount, int CriteriaCount);
