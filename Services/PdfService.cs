using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VentifyAPI.Data;
using VentifyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace VentifyAPI.Services
{
    public class PdfService
    {
        private readonly AppDbContext _context;

        public PdfService(AppDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateVentaPdf(int ventaId, int negocioId)
        {
            var venta = _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Negocio)
                .FirstOrDefault(v => v.Id == ventaId && v.NegocioId == negocioId);

            if (venta == null) throw new Exception("Venta no encontrada.");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text($"Ticket de Venta #{venta.Id}")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            x.Item().Text($"Fecha: {venta.FechaHora:dd/MM/yyyy HH:mm}");
                                // Cliente eliminado
                            x.Item().Text($"Vendedor: {venta.Usuario?.Nombre ?? "N/A"}");
                            x.Item().Text($"Negocio: {venta.Negocio?.NombreNegocio ?? "N/A"}");

                            x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            x.Item().Text("Productos:").SemiBold();
                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(200);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(Block).Text("Producto");
                                    header.Cell().Element(Block).Text("Cant.");
                                    header.Cell().Element(Block).Text("Precio");
                                    header.Cell().Element(Block).Text("Total");

                                    static IContainer Block(IContainer container)
                                    {
                                        return container
                                            .BorderBottom(1)
                                            .PaddingVertical(5)
                                            .AlignCenter();
                                    }
                                });

                                foreach (var detalle in venta.Detalles)
                                {
                                    table.Cell().Element(Block).Text(detalle.Producto?.Nombre ?? "N/A");
                                    table.Cell().Element(Block).Text(detalle.Cantidad.ToString());
                                    table.Cell().Element(Block).Text($"${detalle.PrecioUnitario:F2}");
                                    table.Cell().Element(Block).Text($"${detalle.Subtotal:F2}");

                                    static IContainer Block(IContainer container)
                                    {
                                        return container
                                            .BorderBottom(1)
                                            .PaddingVertical(5);
                                    }
                                }
                            });

                            x.Item().AlignRight().Text($"Total: ${venta.TotalPagado:F2}").Bold().FontSize(16);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Gracias por su compra!")
                        .FontSize(10);
                });
            }).GeneratePdf();
        }

        public byte[] GenerateInventarioPdf(int negocioId, string? categoria = null, bool stockBajo = false)
        {
            var query = _context.Productos
                .Include(p => p.Category)
                .Include(p => p.Usuario)
                .Where(p => p.Usuario != null && p.Usuario.NegocioId == negocioId && p.Activo);

            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(p => p.Category != null && p.Category.Name.Contains(categoria));
            }

            if (stockBajo)
            {
                query = query.Where(p => p.StockActual <= p.StockMinimo);
            }

            var productos = query.OrderBy(p => p.Nombre).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Reporte de Inventario")
                        .SemiBold().FontSize(20).FontColor(Colors.Green.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            x.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            x.Item().Text($"Total productos: {productos.Count}");

                            if (!string.IsNullOrEmpty(categoria))
                                x.Item().Text($"Filtro categoría: {categoria}");

                            if (stockBajo)
                                x.Item().Text("Filtro: Solo productos con stock bajo");

                            x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(200);
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(Block).Text("Producto");
                                    header.Cell().Element(Block).Text("Categoría");
                                    header.Cell().Element(Block).Text("Stock");
                                    header.Cell().Element(Block).Text("Mínimo");
                                    header.Cell().Element(Block).Text("Precio");

                                    static IContainer Block(IContainer container)
                                    {
                                        return container
                                            .BorderBottom(1)
                                            .PaddingVertical(5)
                                            .AlignCenter();
                                    }
                                });

                                foreach (var producto in productos)
                                {
                                    var stockColor = producto.StockActual <= producto.StockMinimo ? Colors.Red.Medium : Colors.Black;

                                    table.Cell().Element(Block).Text(producto.Nombre);
                                    table.Cell().Element(Block).Text(producto.Category?.Name ?? "Sin categoría");
                                    table.Cell().Element(Block).Text(producto.StockActual.ToString()).FontColor(stockColor);
                                    table.Cell().Element(Block).Text(producto.StockMinimo.ToString());
                                    table.Cell().Element(Block).Text($"${producto.PrecioVenta:F2}");

                                    static IContainer Block(IContainer container)
                                    {
                                        return container
                                            .BorderBottom(1)
                                            .PaddingVertical(5);
                                    }
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Sistema Ventify - Reporte de Inventario")
                        .FontSize(10);
                });
            }).GeneratePdf();
        }
    }
}