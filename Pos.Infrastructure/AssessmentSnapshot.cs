using System.Text.Json.Serialization;

namespace Pos.Infrastructure;

public sealed record AssessmentSnapshot([property: JsonRequired] Guid StudentId, [property: JsonRequired] Guid CriterionId,
    [property: JsonRequired] decimal Score, string? Comment);
