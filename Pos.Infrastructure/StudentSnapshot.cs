using System.Text.Json.Serialization;

namespace Pos.Infrastructure;

public sealed record StudentSnapshot([property: JsonRequired] Guid Id, [property: JsonRequired] string Name);
