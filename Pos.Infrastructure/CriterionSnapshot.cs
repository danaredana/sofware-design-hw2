using System.Text.Json.Serialization;

namespace Pos.Infrastructure;

public sealed record CriterionSnapshot([property: JsonRequired] Guid Id, [property: JsonRequired] string Name,
    [property: JsonRequired] decimal Maximum, [property: JsonRequired] decimal Weight);
