using Pos.Domain;
using Pos.Infrastructure;
using Xunit;

namespace Pos.Tests;

public sealed class StorageChecks
{
    [Fact]
    public void JsonRoundTripPreservesDataAndIsolatesSnapshots()
    {
        using var folder = new TestFolder();
        using (var store = new JsonLedgerStore(folder.Path)) store.Save(CreateLedger());
        var path = folder.File("gradebook.json");
        File.WriteAllText(path, File.ReadAllText(path), new System.Text.UTF8Encoding(true));
        using var reopened = new JsonLedgerStore(folder.Path);
        var snapshot = reopened.Load();
        Assert.Equal("Анна, \"Аня\"", snapshot.Students.Single().Name);
        Assert.Equal(8.25m, snapshot.Evaluations.Single().Score.Value);
        snapshot.RenameStudent(snapshot.Students[0].Id, "Другое имя");
        Assert.NotEqual("Другое имя", reopened.Load().Students.Single().Name);
        reopened.Save(snapshot);
        Assert.Equal("Другое имя", reopened.Load().Students.Single().Name);
    }

    [Fact]
    public void CorruptedJsonIsRejectedWithoutOverwrite()
    {
        using var folder = new TestFolder();
        using var store = new JsonLedgerStore(folder.Path);
        File.WriteAllText(folder.File("gradebook.json"), "{");
        Assert.Throws<InvalidDataException>(() => store.Load());
        Assert.Equal("{", File.ReadAllText(folder.File("gradebook.json")));
    }

    [Fact]
    public void StaleSnapshotCannotOverwriteNewerChanges()
    {
        using var folder = new TestFolder();
        using var store = new JsonLedgerStore(folder.Path);
        var first = store.Load(); var stale = store.Load();
        first.AddStudent(Guid.NewGuid(), "Первый"); store.Save(first);
        stale.AddStudent(Guid.NewGuid(), "Второй");
        Assert.Throws<LedgerConflictException>(() => store.Save(stale));
        Assert.Equal("Первый", store.Load().Students.Single().Name);
        Assert.Throws<LedgerConflictException>(() => store.Save(new AssessmentLedger()));
    }

    [Fact]
    public void ConcurrentUpdatesDoNotLoseStudents()
    {
        using var folder = new TestFolder();
        using var store = new JsonLedgerStore(folder.Path);
        Parallel.For(0, 20, i => store.Update(ledger => ledger.AddStudent(Guid.NewGuid(), $"Студент {i}")));
        Assert.Equal(20, store.Load().Students.Count);
    }

    [Fact]
    public void FailedSavePreservesPreviousFile()
    {
        using var folder = new TestFolder();
        using var store = new JsonLedgerStore(folder.Path);
        store.Save(CreateLedger());
        var path = folder.File("gradebook.json");
        var previous = File.ReadAllBytes(path);
        var snapshot = store.Load();
        snapshot.RenameStudent(snapshot.Students[0].Id, "Другое имя");
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            TestCheck.FileError(() => store.Save(snapshot));
        Assert.Equal(previous, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));
    }

    private static AssessmentLedger CreateLedger()
    {
        var ledger = new AssessmentLedger();
        var student = Guid.NewGuid();
        var criterion = Guid.NewGuid();
        ledger.AddStudent(student, "Анна, \"Аня\"");
        ledger.AddCriterion(criterion, "Проект", 12.5m, 0.75m);
        ledger.Assign(student, criterion, new Score(8.25m), "Строка 1\nСтрока 2, \"цитата\"");
        return ledger;
    }
}
