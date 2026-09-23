using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmacenCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompraId",
                table: "MovimientosInventario",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "char(36)", nullable: false),
                    AlmacenId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    NumeroDocumentoProveedor = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Observacion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AnuladoEn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AnuladoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compras_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compras_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compras_Usuarios_AnuladoPorUsuarioId",
                        column: x => x.AnuladoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compras_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SecuenciasCompra",
                columns: table => new
                {
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UltimoNumero = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuenciasCompra", x => x.EmpresaId);
                    table.ForeignKey(
                        name: "FK_SecuenciasCompra_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CompraDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CompraId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductoId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CodigoProducto = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    NombreProducto = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    UnidadMedida = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompraDetalles_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompraDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_CompraId",
                table: "MovimientosInventario",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_CompraId_ProductoId",
                table: "CompraDetalles",
                columns: new[] { "CompraId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_ProductoId",
                table: "CompraDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_AlmacenId",
                table: "Compras",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_AnuladoPorUsuarioId",
                table: "Compras",
                column: "AnuladoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_EmpresaId_Fecha",
                table: "Compras",
                columns: new[] { "EmpresaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Compras_EmpresaId_Numero",
                table: "Compras",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_ProveedorId",
                table: "Compras",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_UsuarioId",
                table: "Compras",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Compras_CompraId",
                table: "MovimientosInventario",
                column: "CompraId",
                principalTable: "Compras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Compras_CompraId",
                table: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "CompraDetalles");

            migrationBuilder.DropTable(
                name: "SecuenciasCompra");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_CompraId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "CompraId",
                table: "MovimientosInventario");
        }
    }
}
