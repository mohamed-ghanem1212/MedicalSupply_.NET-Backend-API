using MedicalSupply.Domain.Entities;
using MedicalSupply.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Departments.AnyAsync())
            return;

        db.Departments.AddRange(
            new Department { Code = "PHARM", Name = "Pharmacy", MonthlyBudget = 50000 },
            new Department { Code = "CLIN", Name = "Clinics", MonthlyBudget = 20000 },
            new Department { Code = "NURS", Name = "Nursing Units", MonthlyBudget = 15000 },
            new Department { Code = "LAB", Name = "Laboratories", MonthlyBudget = 10000 }
        );

        db.Items.AddRange(
            new Item { Code = "MED-001", Name = "Paracetamol 500mg (box of 100)", Category = ItemCategory.Medication, UnitPrice = 12.50m, AvailableQuantity = 500 },
            new Item { Code = "MED-002", Name = "Amoxicillin 250mg (box of 100)", Category = ItemCategory.Medication, UnitPrice = 18.00m, AvailableQuantity = 300 },
            new Item { Code = "SUP-001", Name = "Surgical Gloves (box of 100)", Category = ItemCategory.MedicalSupply, UnitPrice = 8.75m, AvailableQuantity = 1000 },
            new Item { Code = "SUP-002", Name = "IV Catheter 18G (box of 50)", Category = ItemCategory.MedicalSupply, UnitPrice = 22.00m, AvailableQuantity = 200 },
            new Item { Code = "LAB-001", Name = "Blood Collection Tubes (box of 100)", Category = ItemCategory.LaboratorySupply, UnitPrice = 15.30m, AvailableQuantity = 400 },
            new Item { Code = "OFF-001", Name = "A4 Paper Ream", Category = ItemCategory.OfficeSupply, UnitPrice = 4.20m, AvailableQuantity = 600 }
        );

        await db.SaveChangesAsync();
    }
}
