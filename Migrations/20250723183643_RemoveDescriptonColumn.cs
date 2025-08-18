using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace productApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDescriptonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
        name: "Descripton",   // yanlış kolon adı
        table: "Products");   // tablo adı
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
