namespace Pos.Domain;

public sealed class MissingAsZeroPolicy : IMissingEvaluationPolicy
{
    public decimal? Normalize(StudentEvaluation? assessment, AssessmentCriterion criterion)
        => assessment is null ? 0 : assessment.Score.Value / criterion.Maximum;
}
