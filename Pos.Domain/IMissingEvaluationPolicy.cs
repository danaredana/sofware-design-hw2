namespace Pos.Domain;

public interface IMissingEvaluationPolicy
{
    decimal? Normalize(StudentEvaluation? assessment, AssessmentCriterion criterion);
}
