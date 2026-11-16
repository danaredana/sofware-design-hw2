using Pos.Domain;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Pos.Infrastructure;

public sealed class JsonLedgerStore : ILedgerStore, IDisposable
{
    private readonly string _path;
    private readonly FileStream _lock;
    private readonly object _gate = new();
    private readonly ConditionalWeakTable<AssessmentLedger, byte[]> _versions = new();
    private bool _disposed;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonLedgerStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "gradebook.json");
        _lock = new FileStream(Path.Combine(directory, ".pos.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    public AssessmentLedger Load()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return LoadCore();
        }
    }

    private byte[]? ReadData()
    {
        try { return File.ReadAllBytes(_path); }
        catch (FileNotFoundException) { return null; }
    }

    private AssessmentLedger LoadCore()
    {
        var bytes = ReadData();
        if (bytes is null)
        {
            var empty = new AssessmentLedger();
            _versions.Add(empty, []);
            return empty;
        }
        try
        {
            using var reader = new StreamReader(new MemoryStream(bytes));
            var data = JsonSerializer.Deserialize<LedgerSnapshot>(reader.ReadToEnd(), Options)
                ?? throw new InvalidDataException("Пустой JSON-документ.");
            if (data.SchemaVersion != 1) throw new InvalidDataException("Неподдерживаемая версия хранилища.");
            if (data.Students is null || data.Criteria is null || data.Evaluations is null)
                throw new InvalidDataException("Отсутствуют коллекции ведомости.");
            if (data.Students.Any(s => s is null) || data.Criteria.Any(c => c is null) || data.Evaluations.Any(e => e is null))
                throw new InvalidDataException("Пустой элемент в коллекции.");
            var ledger = new AssessmentLedger();
            foreach (var c in data.Criteria) ledger.AddCriterion(c.Id, c.Name, c.Maximum, c.Weight);
            foreach (var s in data.Students) ledger.AddStudent(s.Id, s.Name);
            foreach (var e in data.Evaluations) ledger.Assign(e.StudentId, e.CriterionId, new Score(e.Score), e.Comment);
            _versions.Add(ledger, SHA256.HashData(bytes));
            return ledger;
        }
        catch (Exception e) when (e is JsonException or DomainException)
        {
            throw new InvalidDataException("Повреждённая ведомость: " + e.Message, e);
        }
    }

    public void Save(AssessmentLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            SaveCore(ledger);
        }
    }

    public void Update(Action<AssessmentLedger> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var ledger = LoadCore();
            change(ledger);
            SaveCore(ledger);
        }
    }

    private void SaveCore(AssessmentLedger ledger)
    {
        var current = ReadData();
        var actualVersion = current is null ? [] : SHA256.HashData(current);
        var expectedVersion = _versions.TryGetValue(ledger, out var version) ? version : [];
        if (!actualVersion.SequenceEqual(expectedVersion)) throw new LedgerConflictException();
        var data = new LedgerSnapshot(1, ledger.Evaluations.Count > 0,
            ledger.Students.Select(s => new StudentSnapshot(s.Id, s.Name)).ToList(),
            ledger.Criteria.Select(c => new CriterionSnapshot(c.Id, c.Name, c.Maximum, c.Weight)).ToList(),
            ledger.Evaluations.Select(e => new AssessmentSnapshot(e.StudentId, e.CriterionId, e.Score.Value, e.Comment)).ToList());
        var bytes = JsonSerializer.SerializeToUtf8Bytes(data, Options);
        AtomicFile.Write(_path, stream => stream.Write(bytes), overwrite: true);
        _versions.Remove(ledger);
        _versions.Add(ledger, SHA256.HashData(bytes));
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _lock.Dispose();
            _disposed = true;
        }
    }
}
