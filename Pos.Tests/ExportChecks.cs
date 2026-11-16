using Microsoft.VisualBasic.FileIO;
using Pos.Infrastructure;
using Xunit;

namespace Pos.Tests;

public sealed class ExportChecks
{
    [Fact]
    public void CsvEscapesTextAndFormatsNumbers()
    {
        using var folder = new TestFolder();
        var name = "=Анна, \"Аня\"\nИванова";
        new CsvReportWriter().Export([new(Guid.NewGuid(), name, 7.25m, true, 2, 2)], folder.File("report.csv"));
        var rows = Read(folder.File("report.csv"));
        Assert.Equal(2, rows.Count);
        Assert.Equal("'" + name, rows[1][1]);
        Assert.Equal("7.25", rows[1][2]);
        Assert.Equal("true", rows[1][3]);
    }

    [Fact]
    public void ExportDoesNotOverwriteExistingFile()
    {
        using var folder = new TestFolder();
        File.WriteAllText(folder.File("report.csv"), "original");
        Assert.Throws<IOException>(() => new CsvReportWriter().Export([], folder.File("report.csv")));
        Assert.Equal("original", File.ReadAllText(folder.File("report.csv")));
    }

    [Fact]
    public void FailedExportLeavesNoPartialFileAndCanBeRetried()
    {
        using var folder = new TestFolder();
        var writer = new CsvReportWriter();
        Assert.Throws<IOException>(() => writer.Export(new FailingReport(), folder.File("report.csv")));
        Assert.False(File.Exists(folder.File("report.csv")));
        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));
        writer.Export([], folder.File("report.csv"));
        Assert.Single(File.ReadAllLines(folder.File("report.csv")));
    }

    private static List<string[]> Read(string path)
    {
        using var parser = new TextFieldParser(path);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;
        var rows = new List<string[]>();
        while (!parser.EndOfData) rows.Add(parser.ReadFields()!);
        return rows;
    }
}
