using Pos.Domain;
using Xunit;

namespace Pos.Tests;

public sealed class DomainChecks
{
    [Fact]
    public void ScoreHasValueEqualityAndRejectsNegativeValues()
    {
        Assert.Equal(new Score(5), new Score(5));
        Assert.NotEqual(new Score(5), new Score(6));
        Assert.Throws<DomainException>(() => new Score(-1));
    }

    [Fact]
    public void StudentIdsMustBeNonemptyAndUnique()
    {
        var ledger = new AssessmentLedger();
        var id = Guid.NewGuid();
        Assert.Throws<DomainException>(() => ledger.AddStudent(Guid.Empty, "Анна"));
        ledger.AddStudent(id, "Анна");
        Assert.Throws<DomainException>(() => ledger.AddStudent(id, "Пётр"));
        Assert.Single(ledger.Students);
    }

    [Fact]
    public void CriterionRequiresNameAndPositiveMaximumAndWeight()
    {
        var ledger = new AssessmentLedger();
        Assert.Throws<DomainException>(() => ledger.AddCriterion(Guid.NewGuid(), " ", 10, 1));
        Assert.Throws<DomainException>(() => ledger.AddCriterion(Guid.NewGuid(), "Проект", 0, 1));
        Assert.Throws<DomainException>(() => ledger.AddCriterion(Guid.NewGuid(), "Проект", 10, 0));
        Assert.Empty(ledger.Criteria);
    }

    [Fact]
    public void EvaluationRequiresExistingStudentAndCriterion()
    {
        var (ledger, student, criterion) = CreateLedger();
        Assert.Throws<DomainException>(() => ledger.Assign(Guid.NewGuid(), criterion, new Score(5)));
        Assert.Throws<DomainException>(() => ledger.Assign(student, Guid.NewGuid(), new Score(5)));
        Assert.Empty(ledger.Evaluations);
    }

    [Fact]
    public void EvaluationRejectsExcessiveScoreAndDuplicates()
    {
        var (ledger, student, criterion) = CreateLedger();
        Assert.Throws<DomainException>(() => ledger.Assign(student, criterion, new Score(11)));
        ledger.Assign(student, criterion, new Score(8));
        Assert.Throws<DomainException>(() => ledger.Assign(student, criterion, new Score(7)));
        Assert.Equal(8m, ledger.Evaluations.Single().Score.Value);
    }

    [Fact]
    public void EvaluationCanBeCorrectedAndCancelled()
    {
        var (ledger, student, criterion) = CreateLedger();
        ledger.Assign(student, criterion, new Score(8));
        Assert.Throws<DomainException>(() => ledger.Correct(student, criterion, new Score(11)));
        Assert.Equal(8m, ledger.Evaluations.Single().Score.Value);
        ledger.Correct(student, criterion, new Score(10), "Исправлено");
        Assert.Equal(10m, ledger.Evaluations.Single().Score.Value);
        ledger.Cancel(student, criterion);
        Assert.Empty(ledger.Evaluations);
    }

    [Fact]
    public void LinkedObjectsAndCriterionMaximumAreProtected()
    {
        var (ledger, student, criterion) = CreateLedger();
        ledger.Assign(student, criterion, new Score(8));
        Assert.Throws<DomainException>(() => ledger.DeleteStudent(student));
        Assert.Throws<DomainException>(() => ledger.DeleteCriterion(criterion));
        Assert.Throws<DomainException>(() => ledger.UpdateCriterion(criterion, "Проект", 7, 1));
        ledger.Cancel(student, criterion);
        ledger.DeleteStudent(student);
        ledger.DeleteCriterion(criterion);
        Assert.Empty(ledger.Students);
        Assert.Empty(ledger.Criteria);
    }

    [Fact]
    public void PreviouslyReturnedEntitiesRemainImmutable()
    {
        var (ledger, student, criterion) = CreateLedger();
        var oldStudent = ledger.Students[0];
        var oldCriterion = ledger.Criteria[0];
        ledger.RenameStudent(student, "Анна Иванова");
        ledger.UpdateCriterion(criterion, "Экзамен", 20, 2);
        Assert.Equal("Анна", oldStudent.Name);
        Assert.Equal(10m, oldCriterion.Maximum);
        Assert.Equal(student, ledger.Students[0].Id);
        Assert.Equal(20m, ledger.Criteria[0].Maximum);
    }

    [Fact]
    public void WeightedResultUsesCriterionMaximumsAndWeights()
    {
        var (ledger, student, first) = CreateLedger();
        var second = Guid.NewGuid();
        ledger.UpdateCriterion(first, "Практика", 10, 2);
        ledger.AddCriterion(second, "Проект", 20, 3);
        ledger.Assign(student, first, new Score(8));
        ledger.Assign(student, second, new Score(15));
        Assert.Equal(7.7m, Calculate(ledger, student));
    }

    [Fact]
    public void MissingPoliciesAndEmptyRubricHaveExplicitResults()
    {
        var (ledger, student, _) = CreateLedger();
        Assert.Null(Calculate(ledger, student));
        var result = new WeightedResultCalculator(new MissingAsZeroPolicy()).Calculate(ledger, student);
        Assert.Equal(0m, result.Score);
        Assert.False(result.HasAllEvaluations);
        ledger.DeleteCriterion(ledger.Criteria[0].Id);
        Assert.Null(new WeightedResultCalculator(new MissingAsZeroPolicy()).Calculate(ledger, student).Score);
    }

    [Fact]
    public void ExtremeWeightsDoNotOverflow()
    {
        var (ledger, student, first) = CreateLedger();
        var second = Guid.NewGuid();
        ledger.UpdateCriterion(first, "Первый", 10, decimal.MaxValue);
        ledger.AddCriterion(second, "Второй", 10, decimal.MaxValue);
        ledger.Assign(student, first, new Score(10));
        ledger.Assign(student, second, new Score(5));
        Assert.Equal(7.5m, Calculate(ledger, student));
    }

    [Fact]
    public void ResultRoundsMidpointsAwayFromZero()
    {
        var (ledger, student, criterion) = CreateLedger();
        ledger.UpdateCriterion(criterion, "Проект", 16, 1);
        ledger.Assign(student, criterion, new Score(1));
        Assert.Equal(0.63m, Calculate(ledger, student));
    }

    [Fact]
    public void FinalResultRejectsInconsistentStates()
    {
        Assert.Throws<DomainException>(() => new FinalResult(11, 1, 1));
        Assert.Throws<DomainException>(() => new FinalResult(5, 2, 1));
        Assert.Throws<DomainException>(() => new FinalResult(null, 1, 1));
    }

    private static (AssessmentLedger Ledger, Guid Student, Guid Criterion) CreateLedger()
    {
        var ledger = new AssessmentLedger();
        var student = Guid.NewGuid(); var criterion = Guid.NewGuid();
        ledger.AddStudent(student, "Анна");
        ledger.AddCriterion(criterion, "Проект", 10, 1);
        return (ledger, student, criterion);
    }

    private static decimal? Calculate(AssessmentLedger ledger, Guid student)
        => new WeightedResultCalculator(new RequireAllCriteriaPolicy()).Calculate(ledger, student).Score;
}
