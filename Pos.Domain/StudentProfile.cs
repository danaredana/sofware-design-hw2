namespace Pos.Domain;

public sealed class StudentProfile
{
    public Guid Id { get; }
    public string Name { get; }

    internal StudentProfile(Guid id, string name)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name))
            throw new DomainException("У студента должны быть ID и имя.");
        Id = id;
        Name = name.Trim();
    }
}
