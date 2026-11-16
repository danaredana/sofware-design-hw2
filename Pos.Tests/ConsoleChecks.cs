using System.Diagnostics;
using System.Text;
using Pos.Infrastructure;
using Xunit;

namespace Pos.Tests;

public sealed class ConsoleChecks
{
    [Fact]
    public void InvalidInputIsRecoverable()
    {
        using var folder = new TestFolder();
        var result = Execute(folder.Path, ["unknown", "4", "1", "Анна", "2", "Проект", "abc", "9", "99", "0"]);
        Assert.True(result.Code == 0 && result.Output.Contains("Неизвестная команда"));
        Assert.True(result.Output.Contains("Сначала добавьте студента") && result.Output.Contains("Введите десятичное число"));
        Assert.Contains("Введите номер от 1 до 1", result.Output);
        using var store = new JsonLedgerStore(folder.Path);
        Assert.True(store.Load().Students.Count == 1 && store.Load().Criteria.Count == 0);
    }

    [Fact]
    public void FullConsoleScenario()
    {
        using var folder = new TestFolder();
        var result = Execute(folder.Path,
            ["1", "Анна", "2", "Проект", "10", "1", "4", "1", "1", "8", "Комментарий",
             "9", "1", "Анна Иванова", "11", "1", "Семинары", "20", "2", "3", "7", "8", folder.File("result.csv"),
             "5", "1", "1", "10", "Исправлено", "6", "1", "1", "10", "1", "12", "1", "0"]);
        Assert.True(result.Code == 0 && !result.Output.Contains("Ошибка:"));
        Assert.Contains("Анна Иванова / Семинары: 8", result.Output);
        Assert.True(decimal.Parse(File.ReadAllLines(folder.File("result.csv"))[1].Split(',')[2], System.Globalization.CultureInfo.InvariantCulture) == 4m);
        using var store = new JsonLedgerStore(folder.Path);
        var ledger = store.Load();
        Assert.True(ledger.Students.Count == 0 && ledger.Criteria.Count == 0 && ledger.Evaluations.Count == 0);
    }

    [Fact]
    public void CorruptedDataAtStartup()
    {
        using var folder = new TestFolder();
        File.WriteAllText(folder.File("gradebook.json"), "{");
        var result = Execute(folder.Path, []);
        Assert.True(result.Code == 1 && result.Error.Contains("Не удалось открыть хранилище"));
        Assert.True(File.ReadAllText(folder.File("gradebook.json")) == "{");
    }

    [Fact]
    public void InvalidExportPathIsRecoverable()
    {
        using var folder = new TestFolder();
        var result = Execute(folder.Path, ["8", "bad\0.csv", "0"]);
        Assert.True(result.Code == 0 && result.Output.Contains("Ошибка:"));
        Assert.DoesNotContain("Unhandled exception", result.Error);
        Assert.True(Directory.GetFiles(folder.Path, "*.tmp").Length == 0);
    }

    [Fact]
    public void ZeroPolicyCanBeSelected()
    {
        using var folder = new TestFolder();
        var result = Execute(folder.Path, ["1", "Анна", "2", "Проект", "10", "1", "7", "0"], "--missing=zero");
        Assert.True(result.Code == 0 && result.Output.Contains("Пропуски считаются нулями"));
        Assert.Contains("Анна: 0 (0/1, есть пропуски)", result.Output);
    }

    [Fact]
    public void CorruptionDuringRunStopsSafely()
    {
        using var folder = new TestFolder();
        var result = Execute(folder.Path, ["7", "0"], corruptDuringRun: true);
        Assert.True(result.Code == 1 && result.Error.Contains("Хранилище повреждено"));
        Assert.True(File.ReadAllText(folder.File("gradebook.json")) == "{");
    }

    private static (int Code, string Output, string Error) Execute(string data, string[] commands,
        string? policy = null, bool corruptDuringRun = false)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var assembly = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../Pos.Console/bin", configuration, "net10.0/Pos.Console.dll"));
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add(data);
        if (policy is not null) start.ArgumentList.Add(policy);
        using var process = Process.Start(start) ?? throw new Exception("Не удалось запустить консоль.");
        var prefix = new StringBuilder();
        if (corruptDuringRun)
        {
            while (true)
            {
                var line = process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                if (line is null) throw new Exception("Консоль завершилась до вывода меню.");
                prefix.AppendLine(line);
                if (line.Contains("0 Выход")) break;
            }
            File.WriteAllText(Path.Combine(data, "gradebook.json"), "{");
        }
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        foreach (var command in commands) process.StandardInput.WriteLine(command);
        process.StandardInput.Close();
        if (!process.WaitForExit(15000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new Exception("Консоль не завершилась за 15 секунд.");
        }
        return (process.ExitCode, prefix + output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }
}
