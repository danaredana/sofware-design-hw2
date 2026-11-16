namespace Pos.Domain;

public interface ILedgerStore
{
    AssessmentLedger Load();
    void Save(AssessmentLedger ledger);
    void Update(Action<AssessmentLedger> change);
}
