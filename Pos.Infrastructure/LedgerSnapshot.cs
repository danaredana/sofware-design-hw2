using System.Text.Json.Serialization;

namespace Pos.Infrastructure;

public sealed record LedgerSnapshot([property: JsonRequired] int SchemaVersion, bool RubricLocked,
    [property: JsonRequired] List<StudentSnapshot> Students, [property: JsonRequired] List<CriterionSnapshot> Criteria,
    [property: JsonRequired] List<AssessmentSnapshot> Evaluations);
