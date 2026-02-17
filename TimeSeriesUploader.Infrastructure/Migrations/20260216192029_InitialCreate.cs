using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSeriesUploader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TimeDeltaSeconds = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false),
                    FirstExecutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvgExecutionTime = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false),
                    AvgValue = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false),
                    MedianValue = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false),
                    MaxValue = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false),
                    MinValue = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.FileName);
                });

            migrationBuilder.CreateTable(
                name: "Values",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutionTime = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false),
                    Value = table.Column<double>(type: "double precision", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Values", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Values_FileName_Date",
                table: "Values",
                columns: new[] { "FileName", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Values");
        }
    }
}
