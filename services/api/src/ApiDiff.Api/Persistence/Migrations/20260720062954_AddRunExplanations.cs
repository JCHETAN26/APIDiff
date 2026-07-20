using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiDiff.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRunExplanations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunExplanations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Detail = table.Column<string>(type: "text", nullable: false),
                    ScenarioIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Severity = table.Column<double>(type: "double precision", nullable: false),
                    LikelyCause = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunExplanations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunExplanations_RegressionRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "RegressionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunExplanations_RunId_Severity",
                table: "RunExplanations",
                columns: new[] { "RunId", "Severity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunExplanations");
        }
    }
}
