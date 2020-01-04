using Microsoft.EntityFrameworkCore.Migrations;

namespace Entities.Migrations
{
    public partial class ForeignKeysAdd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Queue",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Queue_UserId",
                table: "Queue",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Queue_AspNetUsers_UserId",
                table: "Queue",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Queue_AspNetUsers_UserId",
                table: "Queue");

            migrationBuilder.DropIndex(
                name: "IX_Queue_UserId",
                table: "Queue");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Queue",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
