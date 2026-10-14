using System.Collections.Generic;
using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

internal interface IWorkRecord
{
    NumberSequenceKind NumberSequenceKind { get; }
    int NumberingPartyId { get; }
    long WorkRecordNumber { get; }
    bool HasWorkRecordNumber { get; }
    void AssignWorkRecordNumber(int partyId, long number, int year, int month);
    void ClearWorkRecordNumber();
    internal IEnumerable<WorkRecordStatusHistory> UnpersistedStatusHistory { get; }
    internal void AppendInitialStatusHistory();
    internal void MarkStatusHistoryPersisted();
}
