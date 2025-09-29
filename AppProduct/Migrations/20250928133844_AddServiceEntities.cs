using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppProduct.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DetailedDescription = table.Column<string>(type: "TEXT", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    DailyRate = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    ProjectRate = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    PricingType = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceCategoryId = table.Column<long>(type: "INTEGER", nullable: true),
                    ServiceCompanyId = table.Column<long>(type: "INTEGER", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstimatedDurationHours = table.Column<int>(type: "INTEGER", nullable: true),
                    RequiresOnsite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludesTravel = table.Column<bool>(type: "INTEGER", nullable: false),
                    Requirements = table.Column<string>(type: "TEXT", nullable: true),
                    Deliverables = table.Column<string>(type: "TEXT", nullable: true),
                    Complexity = table.Column<int>(type: "INTEGER", nullable: false),
                    SKU = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCart",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCart", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconClass = table.Column<string>(type: "TEXT", nullable: true),
                    ColorCode = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCompany",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCertified = table.Column<bool>(type: "INTEGER", nullable: false),
                    Certifications = table.Column<string>(type: "TEXT", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "INTEGER", nullable: true),
                    Specialties = table.Column<string>(type: "TEXT", nullable: true),
                    AverageRating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    TotalReviews = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCompany", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceFeature",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconClass = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceFeature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrder",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    OrderDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    ExpenseAmount = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentIntentId = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerAddress = table.Column<string>(type: "TEXT", nullable: true),
                    BillingAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduledStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScheduledEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresOnsite = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnsiteAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CompletionNotes = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerSignature = table.Column<string>(type: "TEXT", nullable: true),
                    SignatureDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTag",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ColorCode = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceReview",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerEmail = table.Column<string>(type: "TEXT", nullable: true),
                    Rating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewText = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsVerifiedCustomer = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    Response = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResponseBy = table.Column<string>(type: "TEXT", nullable: true),
                    HelpfulVotes = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalVotes = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    QualityRating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    TimelinessRating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    CommunicationRating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    ValueRating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    WouldRecommend = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProjectType = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceReview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceReview_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceCartItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceCartId = table.Column<long>(type: "INTEGER", nullable: true),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    PricingType = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SpecialRequirements = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresOnsite = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCartItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCartItem_ServiceCart_ServiceCartId",
                        column: x => x.ServiceCartId,
                        principalTable: "ServiceCart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceCartItem_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceServiceCategory",
                columns: table => new
                {
                    ServiceCategoryId = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceServiceCategory", x => new { x.ServiceCategoryId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_ServiceServiceCategory_ServiceCategory_ServiceCategoryId",
                        column: x => x.ServiceCategoryId,
                        principalTable: "ServiceCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceServiceCategory_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceServiceCompany",
                columns: table => new
                {
                    ServiceCompanyId = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceServiceCompany", x => new { x.ServiceCompanyId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_ServiceServiceCompany_ServiceCompany_ServiceCompanyId",
                        column: x => x.ServiceCompanyId,
                        principalTable: "ServiceCompany",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceServiceCompany_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceServiceFeature",
                columns: table => new
                {
                    ServiceFeatureId = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceServiceFeature", x => new { x.ServiceFeatureId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_ServiceServiceFeature_ServiceFeature_ServiceFeatureId",
                        column: x => x.ServiceFeatureId,
                        principalTable: "ServiceFeature",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceServiceFeature_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceExpense",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceOrderId = table.Column<long>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Receipt = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsReimbursable = table.Column<bool>(type: "INTEGER", nullable: false),
                    VendorName = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceExpense", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceExpense_ServiceOrder_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrderItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceOrderId = table.Column<long>(type: "INTEGER", nullable: true),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: true),
                    HoursEstimated = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    HoursActual = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    TotalPrice = table.Column<decimal>(type: "TEXT", precision: 19, scale: 4, nullable: true),
                    PricingType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Deliverables = table.Column<string>(type: "TEXT", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderItem_ServiceOrder_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceOrderItem_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceServiceTag",
                columns: table => new
                {
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceTagId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceServiceTag", x => new { x.ServiceId, x.ServiceTagId });
                    table.ForeignKey(
                        name: "FK_ServiceServiceTag_ServiceTag_ServiceTagId",
                        column: x => x.ServiceTagId,
                        principalTable: "ServiceTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceServiceTag_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCartItem_ServiceCartId",
                table: "ServiceCartItem",
                column: "ServiceCartId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCartItem_ServiceId",
                table: "ServiceCartItem",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceExpense_ServiceOrderId",
                table: "ServiceExpense",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderItem_ServiceId",
                table: "ServiceOrderItem",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderItem_ServiceOrderId",
                table: "ServiceOrderItem",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReview_ServiceId",
                table: "ServiceReview",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceServiceCategory_ServiceId",
                table: "ServiceServiceCategory",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceServiceCompany_ServiceId",
                table: "ServiceServiceCompany",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceServiceFeature_ServiceId",
                table: "ServiceServiceFeature",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceServiceTag_ServiceTagId",
                table: "ServiceServiceTag",
                column: "ServiceTagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceCartItem");

            migrationBuilder.DropTable(
                name: "ServiceExpense");

            migrationBuilder.DropTable(
                name: "ServiceOrderItem");

            migrationBuilder.DropTable(
                name: "ServiceReview");

            migrationBuilder.DropTable(
                name: "ServiceServiceCategory");

            migrationBuilder.DropTable(
                name: "ServiceServiceCompany");

            migrationBuilder.DropTable(
                name: "ServiceServiceFeature");

            migrationBuilder.DropTable(
                name: "ServiceServiceTag");

            migrationBuilder.DropTable(
                name: "ServiceCart");

            migrationBuilder.DropTable(
                name: "ServiceOrder");

            migrationBuilder.DropTable(
                name: "ServiceCategory");

            migrationBuilder.DropTable(
                name: "ServiceCompany");

            migrationBuilder.DropTable(
                name: "ServiceFeature");

            migrationBuilder.DropTable(
                name: "ServiceTag");

            migrationBuilder.DropTable(
                name: "Service");
        }
    }
}
