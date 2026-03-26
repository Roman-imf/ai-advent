using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FridayWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "friday");

            migrationBuilder.CreateTable(
                name: "friday_settings",
                schema: "friday",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric", nullable: false),
                    max_tokens = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friday_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "message_history",
                schema: "friday",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    input = table.Column<string>(type: "text", nullable: false),
                    output = table.Column<string>(type: "text", nullable: false),
                    token_input = table.Column<long>(type: "bigint", nullable: false),
                    token_output = table.Column<long>(type: "bigint", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ElapsedSeconds = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_history", x => x.id);
                });
            
            migrationBuilder.Sql("insert into friday.friday_settings values (gen_random_uuid(),'haiku', 0.5,1000 )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "friday_settings",
                schema: "friday");

            migrationBuilder.DropTable(
                name: "message_history",
                schema: "friday");
        }
    }
}
