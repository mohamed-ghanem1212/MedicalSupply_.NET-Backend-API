using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Domain.Entities;

/// <summary>
/// Immutable audit record of a single approval decision. Never updated after
/// creation — see spec Section 11 (audit records must not be overwritten).
/// </summary>
public class ApprovalRecord
{
    public int Id { get; private set; }
    public int SupplyRequestId { get; private set; }
    public ApprovalType ApprovalType { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public string DecisionBy { get; private set; } = null!;
    public DateTime DecisionDate { get; private set; }
    public string? Comments { get; private set; }

    private ApprovalRecord() { } // EF Core

    internal ApprovalRecord(
        int supplyRequestId,
        ApprovalType approvalType,
        ApprovalDecision decision,
        string decisionBy,
        DateTime decisionDate,
        string? comments)
    {
        SupplyRequestId = supplyRequestId;
        ApprovalType = approvalType;
        Decision = decision;
        DecisionBy = decisionBy;
        DecisionDate = decisionDate;
        Comments = comments;
    }
}
