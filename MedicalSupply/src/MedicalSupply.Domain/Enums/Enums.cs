namespace MedicalSupply.Domain.Enums;

public enum ItemCategory
{
    Medication = 1,
    MedicalSupply = 2,
    LaboratorySupply = 3,
    OfficeSupply = 4
}

public enum SupplyRequestStatus
{
    Draft = 1,
    Submitted = 2,
    PendingManagerApproval = 3,
    PendingPharmacyApproval = 4,
    PendingFinanceApproval = 5,
    Approved = 6,
    Rejected = 7,
    Cancelled = 8,
    Fulfilled = 9
}

public enum ApprovalType
{
    DepartmentManager = 1,
    Pharmacy = 2,
    Finance = 3
}

public enum ApprovalDecision
{
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Application-level roles. Kept in Domain so entity/domain-service authorization
/// invariants (e.g. "only a Pharmacist decision satisfies Pharmacy approval") can
/// reference a role concept without depending on ASP.NET Core Identity.
/// </summary>
public enum UserRole
{
    Requester = 1,
    DepartmentManager = 2,
    Pharmacist = 3,
    FinanceOfficer = 4,
    StoreKeeper = 5,
    Administrator = 6
}
