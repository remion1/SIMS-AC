using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sims.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedSystem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CveNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Incidents",
                columns: new[] { "Id", "AffectedSystem", "AssignedTo", "CveNumber", "Description", "ReportedBy", "Severity", "Status", "Timestamp", "Title" },
                values: new object[,]
                {
                    { 1, "Auth-Server", "bob.schmidt@example.com", "CVE-2024-3094", "Malicious code injected into liblzma 5.6.0/5.6.1 enables unauthorised remote access via systemd/sshd on affected Linux hosts. Auth-Server fleet must be patched immediately.", "alice.mueller@example.com", "Critical", "InProgress", new DateTime(2024, 3, 29, 10, 15, 0, 0, DateTimeKind.Utc), "Critical backdoor in XZ Utils (CVE-2024-3094)" },
                    { 2, "Payment-Gateway", "carol.bauer@example.com", null, "Payment-Gateway reported a 15 % increase in HTTP 500 responses between 07:45 and 08:00 UTC. Root cause identified as a misconfigured load-balancer rule after the nightly deployment.", "monitoring@example.com", "High", "Escalated", new DateTime(2024, 4, 5, 8, 0, 0, 0, DateTimeKind.Utc), "Elevated error rate on Payment Gateway" },
                    { 3, "Internal-Wiki", null, null, "The TLS certificate for the internal Confluence wiki expired and was replaced within the agreed SLA window. No data exposure.", "helpdesk@example.com", "Low", "Closed", new DateTime(2024, 4, 10, 14, 30, 0, 0, DateTimeKind.Utc), "Stale SSL certificate on internal wiki" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@sims.local", true, "$2a$12$HHUciHqOx0wK3/ec8rHqpu.X1zZaVRUZyZdtP5vGRMd5.bEKp.riy", "Admin", "admin" },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "analyst@sims.local", true, "$2a$12$Sg7Ky6dWwpaHZNNhS3T5QOTu1I1tzCBvnZ8/qHGeevp/x/zVMgYXa", "User", "analyst" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
