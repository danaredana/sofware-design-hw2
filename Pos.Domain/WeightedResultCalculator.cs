namespace Pos.Domain;

public sealed class WeightedResultCalculator(IMissingEvaluationPolicy policy)
{
    public FinalResult Calculate(AssessmentLedger ledger, Guid studentId)
    {
        ledger.FindStudent(studentId);
        return Calculate(ledger.Criteria, ledger.Evaluations.Where(e => e.StudentId == studentId));
    }

    public IReadOnlyDictionary<Guid, FinalResult> CalculateAll(AssessmentLedger ledger)
    {
        var assessments = ledger.Evaluations.ToLookup(e => e.StudentId);
        return ledger.Students.ToDictionary(s => s.Id, s => Calculate(ledger.Criteria, assessments[s.Id]));
    }

    private FinalResult Calculate(IReadOnlyList<AssessmentCriterion> criteria, IEnumerable<StudentEvaluation> assessments)
    {
        var evaluations = assessments.ToDictionary(e => e.CriterionId);
        if (criteria.Count == 0) return new FinalResult(null, evaluations.Count, 0);

        decimal sum = 0;
        decimal weights = 0;
        var maximumWeight = criteria.Max(c => c.Weight);
        foreach (var criterion in criteria)
        {
            evaluations.TryGetValue(criterion.Id, out var evaluation);
            var normalizedScore = policy.Normalize(evaluation, criterion);
            if (normalizedScore is null) return new FinalResult(null, evaluations.Count, criteria.Count);
            if (normalizedScore is < 0 or > 1)
                throw new DomainException("Политика должна возвращать долю от 0 до 1 или отсутствие результата.");
            var weight = criterion.Weight / maximumWeight;
            sum += weight * normalizedScore.Value;
            weights += weight;
        }
        var result = Math.Round(sum / weights * 10, 2, MidpointRounding.AwayFromZero);
        return new FinalResult(result, evaluations.Count, criteria.Count);
    }
}
