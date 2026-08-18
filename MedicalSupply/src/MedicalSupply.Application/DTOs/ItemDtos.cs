using MedicalSupply.Domain.Enums;

namespace MedicalSupply.Application.DTOs;

public record ItemDto(
    int Id,
    string Code,
    string Name,
    ItemCategory Category,
    decimal UnitPrice,
    int AvailableQuantity,
    int ReservedQuantity,
    int UnreservedQuantity,
    bool RequiresPharmacyApproval,
    bool IsControlledMedication,
    bool IsActive);

public record CreateItemRequest(
    string Code,
    string Name,
    ItemCategory Category,
    decimal UnitPrice,
    int AvailableQuantity,
    bool RequiresPharmacyApproval,
    bool IsControlledMedication,
    bool IsActive = true);

public record UpdateItemRequest(
    string Name,
    ItemCategory Category,
    decimal UnitPrice,
    bool RequiresPharmacyApproval,
    bool IsControlledMedication,
    bool IsActive);

public record ItemSearchRequest(
    string? Search,
    ItemCategory? Category,
    int Page = 1,
    int PageSize = 20);
