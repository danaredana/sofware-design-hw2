using System.Collections;
using Pos.Application;

namespace Pos.Tests;

public sealed class FailingReport : IReadOnlyList<ResultView>
{
    public int Count => 2;
    public ResultView this[int index] => index == 0
        ? new ResultView(Guid.NewGuid(), "Анна", 10, true, 1, 1)
        : throw new IOException("Ошибка получения строки отчёта.");

    public IEnumerator<ResultView> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
