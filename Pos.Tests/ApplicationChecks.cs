using Pos.Application;
using Pos.Domain;
using Pos.Infrastructure;
using Xunit;

namespace Pos.Tests;

public sealed class ApplicationChecks
{
    [Fact]
    public void ScenarioSurvivesRestart()
    {
        using var folder = new TestFolder();
        Guid student; Guid criterion;
        using (var store = new JsonLedgerStore(folder.Path))
        {
            var app = CreateApp(store);
            student = app.AddStudent("  Анна  "); criterion = app.AddCriterion("  Проект  ", 20, 2);
            app.Assign(student, criterion, 15, "Комментарий");
            Assert.True(app.Students().Single().Name == "Анна" && app.Criteria().Single().Name == "Проект");
            Assert.True(app.Evaluations().Single().Comment == "Комментарий" && app.Results().Single().Score == 7.5m);
        }
        using var reopened = new JsonLedgerStore(folder.Path);
        var next = CreateApp(reopened);
        next.Correct(student, criterion, 20, "Исправлено");
        next.Export(folder.File("result.csv"));
        Assert.True(next.Results().Single().Score == 10 && next.Evaluations().Single().Comment == "Исправлено");
        next.Cancel(student, criterion);
        Assert.True(next.Results().Single().Score is null);
        next.DeleteStudent(student); next.DeleteCriterion(criterion);
        Assert.True(next.Students().Count == 0 && next.Criteria().Count == 0 && next.Evaluations().Count == 0);
    }

    [Fact]
    public void RejectedCommandsDoNotChangeData()
    {
        using var folder = new TestFolder();
        using var store = new JsonLedgerStore(folder.Path);
        var app = CreateApp(store);
        var student = app.AddStudent("Анна"); var criterion = app.AddCriterion("Критерий", 10, 1);
        app.Assign(student, criterion, 8);
        var previous = System.IO.File.ReadAllBytes(folder.File("gradebook.json"));
        Action[] invalid = [() => app.Assign(student, criterion, 7), () => app.Correct(student, criterion, 11),
            () => app.RenameStudent(student, " "), () => app.UpdateCriterion(criterion, "Критерий", 7, 1),
            () => app.UpdateCriterion(criterion, "Критерий", 10, 0), () => app.DeleteStudent(student), () => app.DeleteCriterion(criterion)];
        foreach (var action in invalid)
        {
            Assert.Throws<DomainException>(action);
            Assert.True(previous.SequenceEqual(System.IO.File.ReadAllBytes(folder.File("gradebook.json"))));
        }
    }

    [Fact]
    public void ApplicationWorksWithIndependentMemoryAdapterAndRollsBackFailure()
    {
        var store = new MemoryLedgerStore();
        var app = new AssessmentOperations(store, new WeightedResultCalculator(new MissingAsZeroPolicy()), new CsvReportWriter());
        var student = app.AddStudent("Анна"); var criterion = app.AddCriterion("Проект", 10, 1);
        app.Assign(student, criterion, 8);
        Assert.Equal(8m, app.Results().Single().Score);
        var view = app.Overview().Evaluations.Single();
        Assert.Equal("Анна", view.StudentName);
        Assert.Equal("Проект", view.CriterionName);
        store.FailSave = true;
        Assert.Throws<IOException>(() => app.RenameStudent(student, "Другое имя"));
        Assert.Equal("Анна", app.Students().Single().Name);
    }

    private static AssessmentOperations CreateApp(ILedgerStore store)
        => new(store, new WeightedResultCalculator(new RequireAllCriteriaPolicy()), new CsvReportWriter());
}
