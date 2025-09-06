using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brotal.FireflyBuddy.Core.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "buddy");

            migrationBuilder.CreateTable(
                name: "IngestMessages",
                schema: "buddy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessingError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                schema: "buddy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngestMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsTransactional = table.Column<bool>(type: "boolean", nullable: false),
                    IsConfident = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_IngestMessages_IngestMessageId",
                        column: x => x.IngestMessageId,
                        principalSchema: "buddy",
                        principalTable: "IngestMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionDrafts",
                schema: "buddy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngestMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SourceAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DestinationAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BudgetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubscriptionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FireflyTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubmissionError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionDrafts_IngestMessages_IngestMessageId",
                        column: x => x.IngestMessageId,
                        principalSchema: "buddy",
                        principalTable: "IngestMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_CreatedAt",
                schema: "buddy",
                table: "AnalysisResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_IngestMessageId",
                schema: "buddy",
                table: "AnalysisResults",
                column: "IngestMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_IsConfident",
                schema: "buddy",
                table: "AnalysisResults",
                column: "IsConfident");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_IsTransactional",
                schema: "buddy",
                table: "AnalysisResults",
                column: "IsTransactional");

            migrationBuilder.CreateIndex(
                name: "IX_IngestMessages_CreatedAt",
                schema: "buddy",
                table: "IngestMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IngestMessages_ExternalId",
                schema: "buddy",
                table: "IngestMessages",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IngestMessages_Status",
                schema: "buddy",
                table: "IngestMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDrafts_CreatedAt",
                schema: "buddy",
                table: "TransactionDrafts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDrafts_Date",
                schema: "buddy",
                table: "TransactionDrafts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDrafts_IngestMessageId",
                schema: "buddy",
                table: "TransactionDrafts",
                column: "IngestMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDrafts_Status",
                schema: "buddy",
                table: "TransactionDrafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDrafts_Type",
                schema: "buddy",
                table: "TransactionDrafts",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisResults",
                schema: "buddy");

            migrationBuilder.DropTable(
                name: "TransactionDrafts",
                schema: "buddy");

            migrationBuilder.DropTable(
                name: "IngestMessages",
                schema: "buddy");
        }
    }
}
