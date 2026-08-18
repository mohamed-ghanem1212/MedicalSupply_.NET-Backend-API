using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalSupply.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonthlyBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresPharmacyApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsControlledMedication = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplyRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequiresPharmacyApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresFinanceApproval = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplyRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovalType = table.Column<int>(type: "INTEGER", nullable: false),
                    Decision = table.Column<int>(type: "INTEGER", nullable: false),
                    DecisionBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DecisionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Comments = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRecords_SupplyRequests_SupplyRequestId",
                        column: x => x.SupplyRequestId,
                        principalTable: "SupplyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplyRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplyRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovedQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyRequestItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequestItems_SupplyRequests_SupplyRequestId",
                        column: x => x.SupplyRequestId,
                        principalTable: "SupplyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_SupplyRequestId_ApprovalType",
                table: "ApprovalRecords",
                columns: new[] { "SupplyRequestId", "ApprovalType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Category",
                table: "Items",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Code",
                table: "Items",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                table: "Items",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequestItems_ItemId",
                table: "SupplyRequestItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequestItems_SupplyRequestId_ItemId",
                table: "SupplyRequestItems",
                columns: new[] { "SupplyRequestId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_DepartmentId",
                table: "SupplyRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_RequestDate",
                table: "SupplyRequests",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_RequestNumber",
                table: "SupplyRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_Status",
                table: "SupplyRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRecords");

            migrationBuilder.DropTable(
                name: "SupplyRequestItems");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "SupplyRequests");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
