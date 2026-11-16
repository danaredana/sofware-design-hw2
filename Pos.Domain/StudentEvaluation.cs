namespace Pos.Domain;

public sealed class StudentEvaluation
{
    public Guid StudentId { get; }
    public Guid CriterionId { get; }
    public Score Score { get; }
    public string? Comment { get; }

    private StudentEvaluation(Guid studentId, Guid criterionId, Score score, string? comment)
    {
        StudentId = studentId;
        CriterionId = criterionId;
        Score = score;
        Comment = comment;
    }

    internal static StudentEvaluation Create(AssessmentLedger ledger, Guid studentId, Guid criterionId,
        Score score, string? comment)
    {
        ArgumentNullException.ThrowIfNull(score);
        ledger.FindStudent(studentId);
        var criterion = ledger.FindCriterion(criterionId);
        if (score.Value > criterion.Maximum) throw new DomainException("Балл превышает максимум.");
        return new StudentEvaluation(studentId, criterionId, score, comment);
    }
}
