using System.Globalization;
using System.Text;
using Pos.Application;

namespace Pos.Infrastructure;

public sealed class CsvReportWriter : IFileReportExporter
{
    public void Export(IReadOnlyList<ResultView> results, string path)
    {
        AtomicFile.Write(path, stream => Write(results, stream), overwrite: false);
    }

    public void Write(IReadOnlyList<ResultView> results, Stream stream)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true);
        writer.WriteLine("student_id,name,score,is_complete,evaluated_count,criteria_count");
        foreach (var r in results)
            writer.WriteLine(string.Join(",", r.StudentId, Escape(r.Name),
                r.Score?.ToString(CultureInfo.InvariantCulture) ?? "", r.HasAllEvaluations ? "true" : "false",
                r.EvaluatedCount, r.CriteriaCount));
    }

    private static string Escape(string value)
    {
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') ||
            value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
