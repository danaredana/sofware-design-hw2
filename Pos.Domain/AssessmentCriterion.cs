namespace Pos.Domain;

public sealed class AssessmentCriterion
{
    public Guid Id { get; }
    public string Name { get; }
    public decimal Maximum { get; }
    public decimal Weight { get; }

    internal AssessmentCriterion(Guid id, string name, decimal maximum, decimal weight)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name))
            throw new DomainException("У критерия должны быть ID и название.");
        if (maximum <= 0 || weight <= 0)
            throw new DomainException("Максимум и вес должны быть положительными.");
        Id = id;
        Name = name.Trim();
        Maximum = maximum;
        Weight = weight;
    }
}
