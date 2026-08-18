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
    Approved = 3,
    Rejected = 4,
    Cancelled = 5,
    Fulfilled = 6
}

public enum UserRole
{
    Requester = 1,
    DepartmentManager = 2,
    StoreKeeper = 3,
    Administrator = 4
}
