namespace Pos.Domain;

public sealed class LedgerConflictException() : Exception("Ведомость изменилась после загрузки. Повторите операцию с актуальными данными.");
