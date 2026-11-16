namespace Pos.Application;

public interface IFileReportExporter
{
    void Export(IReadOnlyList<ResultView> results, string path);
}
