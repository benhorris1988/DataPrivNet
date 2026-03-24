using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataProvisioning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "virtual_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    owner_id = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_virtual_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_virtual_groups_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "datasets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    owner_group_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datasets", x => x.id);
                    table.ForeignKey(
                        name: "FK_datasets_virtual_groups_owner_group_id",
                        column: x => x.owner_group_id,
                        principalTable: "virtual_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "virtual_group_members",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    added_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_virtual_group_members", x => new { x.group_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_virtual_group_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_virtual_group_members_virtual_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "virtual_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_policy_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dataset_id = table.Column<int>(type: "int", nullable: false),
                    owner_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_policy_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_policy_groups_datasets_dataset_id",
                        column: x => x.dataset_id,
                        principalTable: "datasets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asset_policy_groups_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "columns",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dataset_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_pii = table.Column<bool>(type: "bit", nullable: false),
                    sample_data = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_columns", x => x.id);
                    table.ForeignKey(
                        name: "FK_columns_datasets_dataset_id",
                        column: x => x.dataset_id,
                        principalTable: "datasets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_datasets",
                columns: table => new
                {
                    dataset_id = table.Column<int>(type: "int", nullable: false),
                    report_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_datasets", x => new { x.dataset_id, x.report_id });
                    table.ForeignKey(
                        name: "FK_report_datasets_datasets_dataset_id",
                        column: x => x.dataset_id,
                        principalTable: "datasets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_datasets_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "access_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    dataset_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    requested_rls_filters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    justification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    policy_group_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_access_requests_asset_policy_groups_policy_group_id",
                        column: x => x.policy_group_id,
                        principalTable: "asset_policy_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_access_requests_datasets_dataset_id",
                        column: x => x.dataset_id,
                        principalTable: "datasets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_access_requests_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_access_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_policy_columns",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    policy_group_id = table.Column<int>(type: "int", nullable: false),
                    column_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_hidden = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_policy_columns", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_policy_columns_asset_policy_groups_policy_group_id",
                        column: x => x.policy_group_id,
                        principalTable: "asset_policy_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_policy_conditions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    policy_group_id = table.Column<int>(type: "int", nullable: false),
                    column_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "nvarchar(max)", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_policy_conditions", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_policy_conditions_asset_policy_groups_policy_group_id",
                        column: x => x.policy_group_id,
                        principalTable: "asset_policy_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_requests_dataset_id",
                table: "access_requests",
                column: "dataset_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_requests_policy_group_id",
                table: "access_requests",
                column: "policy_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_requests_reviewed_by",
                table: "access_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_access_requests_user_id",
                table: "access_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_policy_columns_policy_group_id",
                table: "asset_policy_columns",
                column: "policy_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_policy_conditions_policy_group_id",
                table: "asset_policy_conditions",
                column: "policy_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_policy_groups_dataset_id",
                table: "asset_policy_groups",
                column: "dataset_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_policy_groups_owner_id",
                table: "asset_policy_groups",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_columns_dataset_id",
                table: "columns",
                column: "dataset_id");

            migrationBuilder.CreateIndex(
                name: "IX_datasets_owner_group_id",
                table: "datasets",
                column: "owner_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_datasets_report_id",
                table: "report_datasets",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_group_members_user_id",
                table: "virtual_group_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_groups_owner_id",
                table: "virtual_groups",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_requests");

            migrationBuilder.DropTable(
                name: "asset_policy_columns");

            migrationBuilder.DropTable(
                name: "asset_policy_conditions");

            migrationBuilder.DropTable(
                name: "columns");

            migrationBuilder.DropTable(
                name: "report_datasets");

            migrationBuilder.DropTable(
                name: "virtual_group_members");

            migrationBuilder.DropTable(
                name: "asset_policy_groups");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "datasets");

            migrationBuilder.DropTable(
                name: "virtual_groups");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
