using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seed data. Safe to call on every startup — it only inserts when the
/// Departments table is empty. Run migrations first (`dotnet ef database update`),
/// then this executes automatically on next run (see Program.cs) or can be invoked
/// manually. Item quantities are deliberately small/tight so a reviewer can trigger
/// InsufficientStock and BudgetExceeded scenarios without editing data by hand.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(MedicalSupplyDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Departments.AnyAsync(ct))
            return;

        var pharmacy = new Department("PHARM", "Pharmacy", 50_000m);
        var clinics = new Department("CLIN", "Clinics", 20_000m);
        var nursing = new Department("NURS", "Nursing Units", 15_000m);
        var laboratory = new Department("LAB", "Laboratories", 10_000m);
        var finance = new Department("FIN", "Finance", 5_000m);
        var admin = new Department("ADM", "Administration", 3_000m);

        db.Departments.AddRange(pharmacy, clinics, nursing, laboratory, finance, admin);

        var items = new[]
        {
            new Item("MED-001", "Paracetamol 500mg (box of 100)", ItemCategory.Medication, 12.50m, 500, false, false),
            new Item("MED-002", "Morphine Sulfate 10mg/ml (vial)", ItemCategory.Medication, 45.00m, 50, true, true),
            new Item("MED-003", "Amoxicillin 250mg (box of 100)", ItemCategory.Medication, 18.00m, 300, false, false),
            new Item("SUP-001", "Surgical Gloves (box of 100)", ItemCategory.MedicalSupply, 8.75m, 1000, false, false),
            new Item("SUP-002", "IV Catheter 18G (box of 50)", ItemCategory.MedicalSupply, 22.00m, 200, false, false),
            new Item("LAB-001", "Blood Collection Tubes (box of 100)", ItemCategory.LaboratorySupply, 15.30m, 400, false, false),
            new Item("LAB-002", "Reagent Strips (box of 50)", ItemCategory.LaboratorySupply, 60.00m, 80, true, false),
            new Item("OFF-001", "A4 Paper Ream", ItemCategory.OfficeSupply, 4.20m, 600, false, false),
            new Item("OFF-002", "Toner Cartridge", ItemCategory.OfficeSupply, 95.00m, 40, false, false),
        };

        db.Items.AddRange(items);

        await db.SaveChangesAsync(ct);
    }
}
