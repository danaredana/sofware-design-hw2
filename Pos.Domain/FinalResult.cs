namespace Pos.Domain;

public sealed record FinalResult
{
    public decimal? Score { get; }
    public int EvaluatedCount { get; }
    public int CriteriaCount { get; }
    public bool HasAllEvaluations => CriteriaCount > 0 && EvaluatedCount == CriteriaCount;
    public bool CanCalculate => Score.HasValue;

    public FinalResult(decimal? score, int evaluatedCount, int criteriaCount)
    {
        if (criteriaCount < 0 || evaluatedCount < 0 || evaluatedCount > criteriaCount)
            throw new DomainException("Некорректное количество оценок или критериев.");
        if (score is < 0 or > 10 || (criteriaCount == 0 && score.HasValue) ||
            (criteriaCount > 0 && evaluatedCount == criteriaCount && !score.HasValue))
            throw new DomainException("Итог не согласован с оценками и шкалой от 0 до 10.");
        Score = score;
        EvaluatedCount = evaluatedCount;
        CriteriaCount = criteriaCount;
    }
}
