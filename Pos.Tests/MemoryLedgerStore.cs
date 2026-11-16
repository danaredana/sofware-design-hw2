using Pos.Domain;

namespace Pos.Tests;

public sealed class MemoryLedgerStore : ILedgerStore
{
    private readonly object _gate = new();
    private readonly Dictionary<AssessmentLedger, long> _versions = [];
    private AssessmentLedger _ledger = new();
    private long _version;

    public bool FailSave { get; set; }

    public AssessmentLedger Load()
    {
        lock (_gate)
        {
            var copy = Copy(_ledger);
            _versions[copy] = _version;
            return copy;
        }
    }

    public void Save(AssessmentLedger ledger)
    {
        lock (_gate)
        {
            if (_versions.GetValueOrDefault(ledger) != _version) throw new LedgerConflictException();
            if (FailSave) throw new IOException("Отказ сохранения.");
            _ledger = Copy(ledger);
            _versions[ledger] = ++_version;
        }
    }

    public void Update(Action<AssessmentLedger> change)
    {
        lock (_gate)
        {
            var copy = Load();
            change(copy);
            Save(copy);
        }
    }

    private static AssessmentLedger Copy(AssessmentLedger source)
    {
        var copy = new AssessmentLedger();
        foreach (var s in source.Students) copy.AddStudent(s.Id, s.Name);
        foreach (var c in source.Criteria) copy.AddCriterion(c.Id, c.Name, c.Maximum, c.Weight);
        foreach (var e in source.Evaluations) copy.Assign(e.StudentId, e.CriterionId, e.Score, e.Comment);
        return copy;
    }
}
