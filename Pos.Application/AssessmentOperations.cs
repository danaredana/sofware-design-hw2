using Pos.Domain;

namespace Pos.Application;

public sealed class AssessmentOperations(ILedgerStore repository, WeightedResultCalculator calculator, IFileReportExporter exporter)
{
    public Guid AddStudent(string name)
    {
        var id = Guid.NewGuid();
        repository.Update(ledger => ledger.AddStudent(id, name));
        return id;
    }

    public Guid AddCriterion(string name, decimal maximum, decimal weight)
    {
        var id = Guid.NewGuid();
        repository.Update(ledger => ledger.AddCriterion(id, name, maximum, weight));
        return id;
    }

    public void Assign(Guid student, Guid criterion, decimal score, string? comment = null)
    {
        repository.Update(ledger => ledger.Assign(student, criterion, new Score(score), comment));
    }
    public void Correct(Guid student, Guid criterion, decimal score, string? comment = null)
    {
        repository.Update(ledger => ledger.Correct(student, criterion, new Score(score), comment));
    }
    public void Cancel(Guid student, Guid criterion)
    {
        repository.Update(ledger => ledger.Cancel(student, criterion));
    }

    public IReadOnlyList<StudentView> Students() => repository.Load().Students
        .Select(s => new StudentView(s.Id, s.Name)).ToList();
    public IReadOnlyList<CriterionView> Criteria() => repository.Load().Criteria
        .Select(c => new CriterionView(c.Id, c.Name, c.Maximum, c.Weight)).ToList();
    public IReadOnlyList<EvaluationView> Evaluations() => Overview().Evaluations;

    public LedgerView Overview()
    {
        var ledger = repository.Load();
        var students = ledger.Students.ToDictionary(s => s.Id);
        var criteria = ledger.Criteria.ToDictionary(c => c.Id);
        return new LedgerView(
            ledger.Students.Select(s => new StudentView(s.Id, s.Name)).ToList(),
            ledger.Criteria.Select(c => new CriterionView(c.Id, c.Name, c.Maximum, c.Weight)).ToList(),
            ledger.Evaluations.Select(e => new EvaluationView(e.StudentId, e.CriterionId, e.Score.Value,
                e.Comment, students[e.StudentId].Name, criteria[e.CriterionId].Name)).ToList());
    }

    public IReadOnlyList<ResultView> Results()
    {
        var ledger = repository.Load();
        var results = calculator.CalculateAll(ledger);
        return ledger.Students.Select(s =>
        {
            var result = results[s.Id];
            return new ResultView(s.Id, s.Name, result.Score, result.HasAllEvaluations,
                result.EvaluatedCount, result.CriteriaCount);
        }).ToList();
    }

    public void RenameStudent(Guid id, string name)
    {
        repository.Update(ledger => ledger.RenameStudent(id, name));
    }

    public void DeleteStudent(Guid id)
    {
        repository.Update(ledger => ledger.DeleteStudent(id));
    }

    public void UpdateCriterion(Guid id, string name, decimal maximum, decimal weight)
    {
        repository.Update(ledger => ledger.UpdateCriterion(id, name, maximum, weight));
    }

    public void DeleteCriterion(Guid id)
    {
        repository.Update(ledger => ledger.DeleteCriterion(id));
    }

    public void Export(string path) => exporter.Export(Results(), path);
}
