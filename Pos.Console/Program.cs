using Pos.Domain;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application;
using Pos.Infrastructure;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
try
{
    var services = new ServiceCollection();
    if (args.Length > 2 || (args.Length == 2 && args[1] is not ("--missing=strict" or "--missing=zero")))
        throw new ArgumentException("Запуск: Pos.Console [каталог] [--missing=strict|--missing=zero].");
    var dataPath = Path.GetFullPath(args.FirstOrDefault() ?? "data");
    services.AddSingleton<ILedgerStore>(_ => new JsonLedgerStore(dataPath));
    services.AddSingleton<IMissingEvaluationPolicy>(args.ElementAtOrDefault(1) == "--missing=zero"
        ? new MissingAsZeroPolicy() : new RequireAllCriteriaPolicy());
    services.AddSingleton<WeightedResultCalculator>();
    services.AddSingleton<AssessmentOperations>();
    services.AddSingleton<IFileReportExporter, CsvReportWriter>();
    using var provider = services.BuildServiceProvider();
    var app = provider.GetRequiredService<AssessmentOperations>();
    app.Students();
    Console.WriteLine($"ПОС. Хранилище: {dataPath}");
    Console.WriteLine(args.ElementAtOrDefault(1) == "--missing=zero"
        ? "Пропуски считаются нулями." : "Итог доступен после выставления всех оценок.");
    Console.WriteLine("Изменение максимума, веса или набора критериев пересчитывает итоги всех студентов.");
    while (true)
    {
        Console.WriteLine("1 Студент | 2 Критерий | 3 Списки | 4 Оценить | 5 Исправить | 6 Отменить | 7 Итоги | 8 Экспорт CSV | 9 Изменить студента | 10 Удалить студента | 11 Изменить критерий | 12 Удалить критерий | 0 Выход");
        var command = Console.ReadLine();
        if (command is null or "0") break;
        try
        {
            switch (command)
            {
                case "1": app.AddStudent(Read("Имя")); break;
                case "2": app.AddCriterion(Read("Название"), Number("Максимум"), Number("Вес")); break;
                case "3":
                    var overview = app.Overview();
                    var students = overview.Students;
                    var criteria = overview.Criteria;
                    for (int i = 0; i < students.Count; i++) Console.WriteLine($"Студент {i + 1}: {students[i].Name}");
                    for (int i = 0; i < criteria.Count; i++) Console.WriteLine($"Критерий {i + 1}: {criteria[i].Name}, максимум {criteria[i].Maximum}, вес {criteria[i].Weight}");
                    foreach (var e in overview.Evaluations)
                    {
                        Console.WriteLine($"{e.StudentName} / {e.CriterionName}: {e.Score}, {e.Comment}");
                    }
                    break;
                case "4": app.Assign(SelectStudent(app), SelectCriterion(app), Number("Балл"), Read("Комментарий (можно пустой)")); break;
                case "5": app.Correct(SelectStudent(app), SelectCriterion(app), Number("Новый балл"), Read("Комментарий (можно пустой)")); break;
                case "6": app.Cancel(SelectStudent(app), SelectCriterion(app)); break;
                case "7":
                    foreach (var r in app.Results()) Console.WriteLine($"{r.Name}: {r.Score?.ToString() ?? "не завершён"} ({r.EvaluatedCount}/{r.CriteriaCount}, {(r.HasAllEvaluations ? "все оценки" : "есть пропуски")})");
                    break;
                case "8": app.Export(Read("Путь к новому CSV-файлу")); break;
                case "9": app.RenameStudent(SelectStudent(app), Read("Новое имя")); break;
                case "10": app.DeleteStudent(SelectStudent(app)); break;
                case "11": app.UpdateCriterion(SelectCriterion(app), Read("Новое название"), Number("Новый максимум"), Number("Новый вес")); break;
                case "12": app.DeleteCriterion(SelectCriterion(app)); break;
                default: Console.WriteLine("Неизвестная команда."); continue;
            }
            Console.WriteLine("Готово.");
        }
        catch (InvalidDataException e)
        {
            Console.Error.WriteLine("Хранилище повреждено. Работа остановлена: " + e.Message);
            Environment.ExitCode = 1;
            break;
        }
        catch (Exception e) when (e is DomainException or LedgerConflictException or FormatException or IOException or UnauthorizedAccessException or OverflowException or ArgumentException)
        {
            Console.WriteLine("Ошибка: " + e.Message);
        }
    }
}
catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
{
    Console.Error.WriteLine("Не удалось открыть хранилище: " + e.Message);
    Environment.ExitCode = 1;
}

static string Read(string prompt)
{
    Console.Write(prompt + ": ");
    return Console.ReadLine() ?? throw new IOException("Ввод завершён.");
}
static Guid SelectStudent(AssessmentOperations app)
{
    var students = app.Students();
    if (students.Count == 0) throw new FormatException("Сначала добавьте студента.");
    for (int i = 0; i < students.Count; i++) Console.WriteLine($"{i + 1}. {students[i].Name}");
    return students[ReadIndex(students.Count)].Id;
}

static Guid SelectCriterion(AssessmentOperations app)
{
    var criteria = app.Criteria();
    if (criteria.Count == 0) throw new FormatException("Сначала добавьте критерий.");
    for (int i = 0; i < criteria.Count; i++) Console.WriteLine($"{i + 1}. {criteria[i].Name}");
    return criteria[ReadIndex(criteria.Count)].Id;
}

static int ReadIndex(int count)
{
    if (!int.TryParse(Read("Номер"), out int number) || number < 1 || number > count)
        throw new FormatException($"Введите номер от 1 до {count}.");
    return number - 1;
}

static decimal Number(string prompt) => decimal.TryParse(Read(prompt).Replace(',', '.'),
    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
    ? value : throw new FormatException("Введите десятичное число.");
