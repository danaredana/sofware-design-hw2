namespace Pos.Domain;

public sealed class RequireAllCriteriaPolicy : IMissingEvaluationPolicy
{
    public decimal? Normalize(StudentEvaluation? assessment, AssessmentCriterion criterion)
        => assessment is null ? null : assessment.Score.Value / criterion.Maximum;
}
