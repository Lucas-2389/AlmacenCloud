using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmacenCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VentaId",
                table: "MovimientosInventario",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TipoDocumento = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    NombreRazonSocial = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    Direccion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clientes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Ruc = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false),
                    RazonSocial = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    NombreComercial = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    Direccion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proveedores_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SecuenciasVenta",
                columns: table => new
                {
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UltimoNumero = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuenciasVenta", x => x.EmpresaId);
                    table.ForeignKey(
                        name: "FK_SecuenciasVenta_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ClienteId = table.Column<Guid>(type: "char(36)", nullable: true),
                    ClienteNombre = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    AlmacenId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Observacion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AnuladaPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true),
                    AnuladaEn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ventas_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ventas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ventas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ventas_Usuarios_AnuladaPorUsuarioId",
                        column: x => x.AnuladaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ventas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VentaDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    VentaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "char(36)", nullable: false),
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
                    table.PrimaryKey("PK_VentaDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_VentaId",
                table: "MovimientosInventario",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EmpresaId_TipoDocumento_NumeroDocumento",
                table: "Clientes",
                columns: new[] { "EmpresaId", "TipoDocumento", "NumeroDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_EmpresaId_Ruc",
                table: "Proveedores",
                columns: new[] { "EmpresaId", "Ruc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_ProductoId",
                table: "VentaDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_VentaId_ProductoId",
                table: "VentaDetalles",
                columns: new[] { "VentaId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_AlmacenId",
                table: "Ventas",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_AnuladaPorUsuarioId",
                table: "Ventas",
                column: "AnuladaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_EmpresaId_Fecha",
                table: "Ventas",
                columns: new[] { "EmpresaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_EmpresaId_Numero",
                table: "Ventas",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_UsuarioId",
                table: "Ventas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Ventas_VentaId",
                table: "MovimientosInventario",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Ventas_VentaId",
                table: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "SecuenciasVenta");

            migrationBuilder.DropTable(
                name: "VentaDetalles");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_VentaId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "VentaId",
                table: "MovimientosInventario");
        }
    }
}
