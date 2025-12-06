using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPPC.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixStationIdTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForecastModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelType = table.Column<int>(type: "integer", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelData = table.Column<byte[]>(type: "bytea", nullable: true),
                    TrainingAccuracy = table.Column<float>(type: "real", nullable: true),
                    ValidationAccuracy = table.Column<float>(type: "real", nullable: true),
                    ValidationMAE = table.Column<float>(type: "real", nullable: true),
                    ValidationRMSE = table.Column<float>(type: "real", nullable: true),
                    TrainingSampleCount = table.Column<int>(type: "integer", nullable: true),
                    ValidationSampleCount = table.Column<int>(type: "integer", nullable: true),
                    Hyperparameters = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TrainingStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrainingEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LookbackWindow = table.Column<int>(type: "integer", nullable: false),
                    ForecastHorizon = table.Column<int>(type: "integer", nullable: false),
                    ValidationMAPE = table.Column<float>(type: "real", nullable: false),
                    FeatureCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastModels_Configurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForecastTrainingData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationDeclarationId = table.Column<int>(type: "integer", nullable: false),
                    ObservationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DemandValue = table.Column<int>(type: "integer", nullable: false),
                    BufferLevel = table.Column<int>(type: "integer", nullable: true),
                    OrderAmount = table.Column<int>(type: "integer", nullable: true),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Quarter = table.Column<int>(type: "integer", nullable: false),
                    ExogenousFactors = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StationDeclarationId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastTrainingData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastTrainingData_Configurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForecastTrainingData_StationDeclarations_StationDeclaration~",
                        column: x => x.StationDeclarationId1,
                        principalTable: "StationDeclarations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ForecastPredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationDeclarationId = table.Column<int>(type: "integer", nullable: false),
                    PredictionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ForecastStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictedValues = table.Column<string>(type: "jsonb", nullable: false),
                    UpperConfidenceInterval = table.Column<string>(type: "jsonb", nullable: true),
                    LowerConfidenceInterval = table.Column<string>(type: "jsonb", nullable: true),
                    ConfidenceLevel = table.Column<float>(type: "real", nullable: true),
                    ActualValues = table.Column<string>(type: "jsonb", nullable: true),
                    MAE = table.Column<float>(type: "real", nullable: true),
                    MAPE = table.Column<float>(type: "real", nullable: true),
                    WasUsedInPlanning = table.Column<bool>(type: "boolean", nullable: false),
                    WasOverridden = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StationDeclarationId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    LowerBound = table.Column<string>(type: "text", nullable: false),
                    UpperBound = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastPredictions_ForecastModels_ForecastModelId",
                        column: x => x.ForecastModelId,
                        principalTable: "ForecastModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ForecastPredictions_StationDeclarations_StationDeclarationI~",
                        column: x => x.StationDeclarationId1,
                        principalTable: "StationDeclarations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ModelMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationDeclarationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvaluationType = table.Column<int>(type: "integer", nullable: false),
                    MAE = table.Column<float>(type: "real", nullable: false),
                    MAPE = table.Column<float>(type: "real", nullable: false),
                    RMSE = table.Column<float>(type: "real", nullable: false),
                    RSquared = table.Column<float>(type: "real", nullable: true),
                    MeanForecastError = table.Column<float>(type: "real", nullable: true),
                    ForecastErrorStdDev = table.Column<float>(type: "real", nullable: true),
                    TrackingSignal = table.Column<float>(type: "real", nullable: true),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    EvaluationStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EvaluationEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ForecastHorizon = table.Column<int>(type: "integer", nullable: true),
                    AdditionalMetrics = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelMetrics_ForecastModels_ForecastModelId",
                        column: x => x.ForecastModelId,
                        principalTable: "ForecastModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModelMetrics_StationDeclarations_StationDeclarationId",
                        column: x => x.StationDeclarationId,
                        principalTable: "StationDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastModels_ConfigurationId_IsActive",
                table: "ForecastModels",
                columns: new[] { "ConfigurationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastModels_CreatedAt",
                table: "ForecastModels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastPredictions_ForecastModelId_PredictionDate",
                table: "ForecastPredictions",
                columns: new[] { "ForecastModelId", "PredictionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastPredictions_PredictionDate",
                table: "ForecastPredictions",
                column: "PredictionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastPredictions_StationDeclarationId_ForecastStartDate",
                table: "ForecastPredictions",
                columns: new[] { "StationDeclarationId", "ForecastStartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastPredictions_StationDeclarationId1",
                table: "ForecastPredictions",
                column: "StationDeclarationId1");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastTrainingData_ConfigurationId_ObservationDate",
                table: "ForecastTrainingData",
                columns: new[] { "ConfigurationId", "ObservationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastTrainingData_ObservationDate",
                table: "ForecastTrainingData",
                column: "ObservationDate");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastTrainingData_StationDeclarationId_ObservationDate",
                table: "ForecastTrainingData",
                columns: new[] { "StationDeclarationId", "ObservationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastTrainingData_StationDeclarationId1",
                table: "ForecastTrainingData",
                column: "StationDeclarationId1");

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetrics_CreatedAt",
                table: "ModelMetrics",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetrics_ForecastModelId_EvaluationType",
                table: "ModelMetrics",
                columns: new[] { "ForecastModelId", "EvaluationType" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetrics_ForecastModelId_StationDeclarationId",
                table: "ModelMetrics",
                columns: new[] { "ForecastModelId", "StationDeclarationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelMetrics_StationDeclarationId",
                table: "ModelMetrics",
                column: "StationDeclarationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForecastPredictions");

            migrationBuilder.DropTable(
                name: "ForecastTrainingData");

            migrationBuilder.DropTable(
                name: "ModelMetrics");

            migrationBuilder.DropTable(
                name: "ForecastModels");
        }
    }
}
