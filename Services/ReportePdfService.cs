using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VentifyAPI.DTOs;

namespace VentifyAPI.Services
{
    /// <summary>
    /// Servicio profesional para generación de reportes en PDF usando QuestPDF
    /// </summary>
    public class ReportePdfService
    {
        public ReportePdfService()
        {
            // Configurar licencia de QuestPDF (Community para proyectos opensource/personales)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Genera un archivo PDF profesional con el reporte de ventas
        /// </summary>
        public byte[] GenerarReporteVentas(ReporteVentasCompletoDTO reporte)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ctx => CrearEncabezado(ctx, reporte));
                    page.Content().Element(ctx => CrearContenido(ctx, reporte));
                    page.Footer().Element(CrearPieDePagina);
                });
            }).GeneratePdf();
        }

        private void CrearEncabezado(IContainer container, ReporteVentasCompletoDTO reporte)
        {
            container.Column(column =>
            {
                // Título principal
                column.Item().PaddingBottom(10).Text(text =>
                {
                    text.Span($"REPORTE DE VENTAS\n").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    text.Span(reporte.NombreNegocio.ToUpper()).FontSize(16).Bold();
                });

                // Información del reporte
                column.Item().Background(Colors.Grey.Lighten3).Padding(10).Text(text =>
                {
                    text.Span($"Tipo: ").Bold();
                    text.Span($"{reporte.TipoReporte}\n");
                    text.Span($"Período: ").Bold();
                    text.Span($"{reporte.FechaInicio:dd/MM/yyyy} - {reporte.FechaFin:dd/MM/yyyy}\n");
                    text.Span($"Generado: ").Bold();
                    text.Span($"{reporte.FechaGeneracion:dd/MM/yyyy HH:mm}");
                });

                column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
            });
        }

        private void CrearContenido(IContainer container, ReporteVentasCompletoDTO reporte)
        {
            container.PaddingTop(20).Column(column =>
            {
                // Resumen General
                column.Item().Element(ctx => CrearSeccionResumen(ctx, reporte.ResumenGeneral));

                // Desglose por Método de Pago
                column.Item().PaddingTop(15).Element(ctx => CrearSeccionMetodosPago(ctx, reporte.ResumenGeneral));

                // Datos por Período
                if (reporte.DatosPorPeriodo.Any())
                {
                    column.Item().PageBreak();
                    column.Item().Element(ctx => CrearSeccionDatosPorPeriodo(ctx, reporte));
                }

                // Top Productos
                if (reporte.TopProductos.Any())
                {
                    column.Item().PageBreak();
                    column.Item().Element(ctx => CrearSeccionTopProductos(ctx, reporte));
                }
            });
        }

        private void CrearSeccionResumen(IContainer container, ReporteVentasAgregadoDTO resumen)
        {
            container.Column(column =>
            {
                // Título de sección
                column.Item().Background(Colors.Blue.Darken2).Padding(8).Text("RESUMEN GENERAL")
                    .FontSize(14).Bold().FontColor(Colors.White);

                // Tabla de KPIs
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    // Fila 1
                    AgregarFilaKPI(table, "Total de Ventas:", $"{resumen.TotalVentas:N0}", Colors.Blue.Lighten4);
                    AgregarFilaKPI(table, "Total Ingresos:", $"${resumen.TotalIngresos:N2}", Colors.White);
                    AgregarFilaKPI(table, "Subtotal:", $"${resumen.TotalSubtotal:N2}", Colors.Blue.Lighten4);
                    AgregarFilaKPI(table, "IVA:", $"${resumen.TotalIva:N2}", Colors.White);
                    AgregarFilaKPI(table, "Descuentos:", $"${resumen.TotalDescuentos:N2}", Colors.Blue.Lighten4);
                    AgregarFilaKPI(table, "Ticket Promedio:", $"${resumen.TicketPromedio:N2}", Colors.White);
                    AgregarFilaKPI(table, "Venta Máxima:", $"${resumen.VentaMaxima:N2}", Colors.Blue.Lighten4);
                    AgregarFilaKPI(table, "Venta Mínima:", $"${resumen.VentaMinima:N2}", Colors.White);
                    AgregarFilaKPI(table, "Cajeros Activos:", $"{resumen.CajerosActivos}", Colors.Blue.Lighten4);
                    AgregarFilaKPI(table, "Clientes Únicos:", $"{resumen.ClientesUnicos}", Colors.White);
                });
            });
        }

        private void CrearSeccionMetodosPago(IContainer container, ReporteVentasAgregadoDTO resumen)
        {
            container.Column(column =>
            {
                // Título de sección
                column.Item().Background(Colors.Green.Darken2).Padding(8).Text("DESGLOSE POR MÉTODO DE PAGO")
                    .FontSize(14).Bold().FontColor(Colors.White);

                // Tabla
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    // Encabezados
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                        .Text("Método").Bold().FontSize(11);
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                        .Text("Total").Bold().FontSize(11);
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                        .Text("# Ventas").Bold().FontSize(11);

                    // Efectivo
                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).Text("Efectivo");
                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).AlignRight()
                        .Text($"${resumen.TotalEfectivo:N2}");
                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).AlignCenter()
                        .Text($"{resumen.VentasEfectivo}");

                    // Tarjeta
                    table.Cell().Padding(5).Text("Tarjeta");
                    table.Cell().Padding(5).AlignRight().Text($"${resumen.TotalTarjeta:N2}");
                    table.Cell().Padding(5).AlignCenter().Text($"{resumen.VentasTarjeta}");

                    // Transferencia
                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).Text("Transferencia");
                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).AlignRight()
                        .Text($"${resumen.TotalTransferencia:N2}");
                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).AlignCenter()
                        .Text($"{resumen.VentasTransferencia}");
                });
            });
        }

        private void CrearSeccionDatosPorPeriodo(IContainer container, ReporteVentasCompletoDTO reporte)
        {
            container.Column(column =>
            {
                // Título de sección
                column.Item().Background(Colors.Orange.Darken2).Padding(8).Text("DATOS POR PERÍODO")
                    .FontSize(14).Bold().FontColor(Colors.White);

                // Tabla
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    // Encabezados
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Período").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Ventas").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Ingresos").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Ticket Prom.").Bold();

                    // Datos
                    bool alternate = false;
                    foreach (var dato in reporte.DatosPorPeriodo.Take(20)) // Limitar a 20 para evitar overflow
                    {
                        var bgColor = alternate ? Colors.Orange.Lighten4 : Colors.White;

                        table.Cell().Background(bgColor).Padding(5).Text(dato.Periodo);
                        table.Cell().Background(bgColor).Padding(5).AlignCenter().Text($"{dato.TotalVentas}");
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${dato.TotalIngresos:N2}");
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${dato.TicketPromedio:N2}");

                        alternate = !alternate;
                    }
                });
            });
        }

        private void CrearSeccionTopProductos(IContainer container, ReporteVentasCompletoDTO reporte)
        {
            container.Column(column =>
            {
                // Título de sección
                column.Item().Background(Colors.Purple.Darken2).Padding(8).Text("TOP 20 PRODUCTOS MÁS VENDIDOS")
                    .FontSize(14).Bold().FontColor(Colors.White);

                // Tabla
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    // Encabezados
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Producto").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Categoría").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Cantidad").Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Total Ventas").Bold();

                    // Datos
                    bool alternate = false;
                    foreach (var producto in reporte.TopProductos.Take(20))
                    {
                        var bgColor = alternate ? Colors.Purple.Lighten4 : Colors.White;

                        table.Cell().Background(bgColor).Padding(5).Text(producto.ProductoNombre);
                        table.Cell().Background(bgColor).Padding(5).Text(producto.CategoriaNombre ?? "N/A");
                        table.Cell().Background(bgColor).Padding(5).AlignCenter().Text($"{producto.CantidadVendida}");
                        table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${producto.TotalVentas:N2}");

                        alternate = !alternate;
                    }
                });
            });
        }

        private void CrearPieDePagina(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.DefaultTextStyle(TextStyle.Default.FontSize(9).FontColor(Colors.Grey.Medium));
                x.Span("Página ");
                x.CurrentPageNumber();
                x.Span(" de ");
                x.TotalPages();
            });
        }

        private void AgregarFilaKPI(TableDescriptor table, string etiqueta, string valor, string backgroundColor)
        {
            table.Cell().Background(backgroundColor).Padding(8).Text(etiqueta).Bold();
            table.Cell().Background(backgroundColor).Padding(8).AlignRight().Text(valor);
        }
    }
}
