using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BuffetDiscovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Offerings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RestaurantSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RestaurantId = table.Column<int>(type: "integer", nullable: false),
                    CancellationCutoffMinutes = table.Column<int>(type: "integer", nullable: false),
                    WaitlistOfferWindowMinutes = table.Column<int>(type: "integer", nullable: false),
                    OverbookingTolerancePercent = table.Column<int>(type: "integer", nullable: false),
                    IsFoundingRestaurant = table.Column<bool>(type: "boolean", nullable: false),
                    ReferredByRestaurantId = table.Column<int>(type: "integer", nullable: true),
                    FeaturedScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantSettings_Restaurants_ReferredByRestaurantId",
                        column: x => x.ReferredByRestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantSettings_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferingId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    BufferMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeSlots_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferingId = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferingId = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ConfirmationCode",
                table: "Bookings",
                column: "ConfirmationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerPhone",
                table: "Bookings",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_OfferingId_Date_Status",
                table: "Bookings",
                columns: new[] { "OfferingId", "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TimeSlotId_Date_Status",
                table: "Bookings",
                columns: new[] { "TimeSlotId", "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantSettings_ReferredByRestaurantId",
                table: "RestaurantSettings",
                column: "ReferredByRestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantSettings_RestaurantId",
                table: "RestaurantSettings",
                column: "RestaurantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_OfferingId_IsDeleted",
                table: "TimeSlots",
                columns: new[] { "OfferingId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_CustomerPhone",
                table: "WaitlistEntries",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_OfferingId_Date_Status_Position",
                table: "WaitlistEntries",
                columns: new[] { "OfferingId", "Date", "Status", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_TimeSlotId_Date_Status_Position",
                table: "WaitlistEntries",
                columns: new[] { "TimeSlotId", "Date", "Status", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "RestaurantSettings");

            migrationBuilder.DropTable(
                name: "WaitlistEntries");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Offerings");
        }
    }
}
