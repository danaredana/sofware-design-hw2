namespace Pos.Domain;

public sealed class AssessmentLedger
{
    private readonly List<StudentProfile> _students = [];
    private readonly List<AssessmentCriterion> _criteria = [];
    private readonly List<StudentEvaluation> _evaluations = [];

    private readonly Dictionary<Guid, StudentProfile> _studentIndex = [];
    private readonly Dictionary<Guid, AssessmentCriterion> _criterionIndex = [];
    private readonly Dictionary<(Guid, Guid), StudentEvaluation> _evaluationIndex = [];

    public IReadOnlyList<StudentProfile> Students { get; }
    public IReadOnlyList<AssessmentCriterion> Criteria { get; }
    public IReadOnlyList<StudentEvaluation> Evaluations { get; }

    public AssessmentLedger()
    {
        Students = _students.AsReadOnly();
        Criteria = _criteria.AsReadOnly();
        Evaluations = _evaluations.AsReadOnly();
    }

    public void AddStudent(Guid id, string name)
    {
        if (_studentIndex.ContainsKey(id)) throw new DomainException("Повторный ID студента.");
        var student = new StudentProfile(id, name);
        _students.Add(student);
        _studentIndex.Add(id, student);
    }

    public void AddCriterion(Guid id, string name, decimal maximum, decimal weight)
    {
        if (_criterionIndex.ContainsKey(id)) throw new DomainException("Повторный ID критерия.");
        var criterion = new AssessmentCriterion(id, name, maximum, weight);
        _criteria.Add(criterion);
        _criterionIndex.Add(id, criterion);
    }

    public StudentProfile FindStudent(Guid id) => _studentIndex.TryGetValue(id, out var student)
        ? student : throw new DomainException("Студент не найден.");

    public void Assign(Guid studentId, Guid criterionId, Score score, string? comment = null)
    {
        if (_evaluationIndex.ContainsKey((studentId, criterionId)))
            throw new DomainException("Оценка уже есть. Используйте исправление.");
        var evaluation = StudentEvaluation.Create(this, studentId, criterionId, score, comment);
        _evaluations.Add(evaluation);
        _evaluationIndex.Add((studentId, criterionId), evaluation);
    }

    public void Correct(Guid studentId, Guid criterionId, Score score, string? comment = null)
    {
        var previous = FindEvaluation(studentId, criterionId);
        var replacement = StudentEvaluation.Create(this, studentId, criterionId, score, comment);
        _evaluations[_evaluations.IndexOf(previous)] = replacement;
        _evaluationIndex[(studentId, criterionId)] = replacement;
    }

    public void Cancel(Guid studentId, Guid criterionId)
    {
        _evaluations.Remove(FindEvaluation(studentId, criterionId));
        _evaluationIndex.Remove((studentId, criterionId));
    }

    public void RenameStudent(Guid id, string name)
    {
        var previous = FindStudent(id);
        var replacement = new StudentProfile(id, name);
        _students[_students.IndexOf(previous)] = replacement;
        _studentIndex[id] = replacement;
    }

    public void DeleteStudent(Guid id)
    {
        var student = FindStudent(id);
        if (_evaluations.Any(e => e.StudentId == id))
            throw new DomainException("Сначала отмените оценки студента.");
        _students.Remove(student);
        _studentIndex.Remove(id);
    }

    public void UpdateCriterion(Guid id, string name, decimal maximum, decimal weight)
    {
        var criterion = FindCriterion(id);
        if (_evaluations.Any(e => e.CriterionId == id && e.Score.Value > maximum))
            throw new DomainException("Максимум меньше уже выставленной оценки.");
        var replacement = new AssessmentCriterion(id, name, maximum, weight);
        _criteria[_criteria.IndexOf(criterion)] = replacement;
        _criterionIndex[id] = replacement;
    }

    public void DeleteCriterion(Guid id)
    {
        var criterion = FindCriterion(id);
        if (_evaluations.Any(e => e.CriterionId == id))
            throw new DomainException("Сначала отмените оценки по критерию.");
        _criteria.Remove(criterion);
        _criterionIndex.Remove(id);
    }

    internal AssessmentCriterion FindCriterion(Guid id) => _criterionIndex.TryGetValue(id, out var criterion)
        ? criterion : throw new DomainException("Критерий не найден.");

    private StudentEvaluation FindEvaluation(Guid studentId, Guid criterionId)
        => _evaluationIndex.TryGetValue((studentId, criterionId), out var evaluation)
            ? evaluation : throw new DomainException("Оценка не найдена.");
}
