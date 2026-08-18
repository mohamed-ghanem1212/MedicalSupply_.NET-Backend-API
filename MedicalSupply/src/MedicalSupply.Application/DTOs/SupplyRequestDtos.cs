using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.DTOs;

public record CreateSupplyRequestItemDto(int ItemId, int RequestedQuantity);

public record CreateSupplyRequestRequest(
    int DepartmentId,
    string RequestedBy,
    List<CreateSupplyRequestItemDto> Items);

public record SupplyRequestItemDto(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    int RequestedQuantity,
    int? ApprovedQuantity,
    decimal UnitPrice,
    decimal TotalPrice);

public record ApprovalRecordDto(
    ApprovalType ApprovalType,
    ApprovalDecision Decision,
    string DecisionBy,
    DateTime DecisionDate,
    string? Comments);

public record SupplyRequestSummaryDto(
    int Id,
    string RequestNumber,
    int DepartmentId,
    string DepartmentName,
    string RequestedBy,
    DateTime RequestDate,
    SupplyRequestStatus Status,
    decimal TotalAmount);

public record SupplyRequestDetailsDto(
    int Id,
    string RequestNumber,
    int DepartmentId,
    string DepartmentName,
    string RequestedBy,
    DateTime RequestDate,
    SupplyRequestStatus Status,
    decimal TotalAmount,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool RequiresPharmacyApproval,
    bool RequiresFinanceApproval,
    List<SupplyRequestItemDto> Items,
    List<ApprovalType> RequiredApprovals,
    List<ApprovalRecordDto> CompletedApprovals);

public record SupplyRequestSearchRequest(
    int? DepartmentId,
    SupplyRequestStatus? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 20);

public record ApprovalActionRequest(ApprovalType ApprovalType, string DecisionBy, string? Comments);
public record RejectionActionRequest(ApprovalType ApprovalType, string DecisionBy, string Reason);
