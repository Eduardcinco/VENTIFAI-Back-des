using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using VentifyAPI.DTOs;

namespace VentifyAPI.Services
{
    /// <summary>
    /// Servicio profesional para generación de reportes en Excel usando EPPlus
    /// </summary>
    public class ReporteExcelService
    {
        public ReporteExcelService() { }

        /// <summary>
        /// Genera un archivo Excel profesional con el reporte de ventas
        /// </summary>
        public byte[] GenerarReporteVentas(ReporteVentasCompletoDTO reporte)
        {
            using var package = new ExcelPackage();
            
            // Hoja 1: Resumen General
            CrearHojaResumen(package, reporte);
            
            // Hoja 2: Datos por Período
            CrearHojaDatosPorPeriodo(package, reporte);
            
            // Hoja 3: Top Productos
            CrearHojaTopProductos(package, reporte);
            
            // Hoja 4: Ventas Detalladas (si existe)
            if (reporte.VentasDetalladas != null && reporte.VentasDetalladas.Any())
            {
                CrearHojaVentasDetalladas(package, reporte);
            }
            
            return package.GetAsByteArray();
        }

        private void CrearHojaResumen(ExcelPackage package, ReporteVentasCompletoDTO reporte)
        {
            var worksheet = package.Workbook.Worksheets.Add("Resumen General");
            var resumen = reporte.ResumenGeneral;
            
            // Encabezado principal
            worksheet.Cells["A1:B1"].Merge = true;
            worksheet.Cells["A1"].Value = $"REPORTE DE VENTAS - {reporte.NombreNegocio.ToUpper()}";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            // Información del reporte
            int row = 3;
            worksheet.Cells[$"A{row}"].Value = "Tipo de Reporte:";
            worksheet.Cells[$"B{row}"].Value = reporte.TipoReporte;
            row++;
            
            worksheet.Cells[$"A{row}"].Value = "Período:";
            worksheet.Cells[$"B{row}"].Value = $"{reporte.FechaInicio:dd/MM/yyyy} - {reporte.FechaFin:dd/MM/yyyy}";
            row++;
            
            worksheet.Cells[$"A{row}"].Value = "Fecha de Generación:";
            worksheet.Cells[$"B{row}"].Value = reporte.FechaGeneracion.ToString("dd/MM/yyyy HH:mm");
            row += 2;
            
            // KPIs Principales
            worksheet.Cells[$"A{row}:B{row}"].Merge = true;
            worksheet.Cells[$"A{row}"].Value = "INDICADORES CLAVE";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            worksheet.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
            worksheet.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
            row++;
            
            AgregarFila(worksheet, ref row, "Total de Ventas:", resumen.TotalVentas.ToString("N0"));
            AgregarFila(worksheet, ref row, "Total Ingresos:", $"${resumen.TotalIngresos:N2}");
            AgregarFila(worksheet, ref row, "Subtotal:", $"${resumen.TotalSubtotal:N2}");
            AgregarFila(worksheet, ref row, "IVA:", $"${resumen.TotalIva:N2}");
            AgregarFila(worksheet, ref row, "Descuentos Aplicados:", $"${resumen.TotalDescuentos:N2}");
            AgregarFila(worksheet, ref row, "Ticket Promedio:", $"${resumen.TicketPromedio:N2}");
            AgregarFila(worksheet, ref row, "Venta Máxima:", $"${resumen.VentaMaxima:N2}");
            AgregarFila(worksheet, ref row, "Venta Mínima:", $"${resumen.VentaMinima:N2}");
            row++;
            
            // Desglose por Método de Pago
            worksheet.Cells[$"A{row}:B{row}"].Merge = true;
            worksheet.Cells[$"A{row}"].Value = "DESGLOSE POR MÉTODO DE PAGO";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            worksheet.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
            worksheet.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
            row++;
            
            AgregarFila(worksheet, ref row, "Efectivo:", $"${resumen.TotalEfectivo:N2} ({resumen.VentasEfectivo} ventas)");
            AgregarFila(worksheet, ref row, "Tarjeta:", $"${resumen.TotalTarjeta:N2} ({resumen.VentasTarjeta} ventas)");
            AgregarFila(worksheet, ref row, "Transferencia:", $"${resumen.TotalTransferencia:N2} ({resumen.VentasTransferencia} ventas)");
            row++;
            
            // Otros indicadores
            worksheet.Cells[$"A{row}:B{row}"].Merge = true;
            worksheet.Cells[$"A{row}"].Value = "OTROS INDICADORES";
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            worksheet.Cells[$"A{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[$"A{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
            worksheet.Cells[$"A{row}"].Style.Font.Color.SetColor(Color.White);
            row++;
            
            AgregarFila(worksheet, ref row, "Cajeros Activos:", resumen.CajerosActivos.ToString());
            AgregarFila(worksheet, ref row, "Clientes Únicos:", resumen.ClientesUnicos.ToString());
            
            // Formato y ancho de columnas
            worksheet.Column(1).Width = 30;
            worksheet.Column(2).Width = 25;
            worksheet.Cells[$"B{row - 15}:B{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }

        private void CrearHojaDatosPorPeriodo(ExcelPackage package, ReporteVentasCompletoDTO reporte)
        {
            var worksheet = package.Workbook.Worksheets.Add("Datos por Período");
            
            // Encabezados
            int row = 1;
            var headers = new[] { "Período", "Total Ventas", "Ingresos", "Subtotal", "IVA", 
                "Descuentos", "Ticket Promedio", "Efectivo", "Tarjeta", "Transferencia" };
            
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cells[row, col + 1];
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            row++;
            
            // Datos
            foreach (var dato in reporte.DatosPorPeriodo)
            {
                worksheet.Cells[row, 1].Value = dato.Periodo;
                worksheet.Cells[row, 2].Value = dato.TotalVentas;
                worksheet.Cells[row, 3].Value = dato.TotalIngresos;
                worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 4].Value = dato.TotalSubtotal;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 5].Value = dato.TotalIva;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 6].Value = dato.TotalDescuentos;
                worksheet.Cells[row, 6].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 7].Value = dato.TicketPromedio;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 8].Value = dato.TotalEfectivo;
                worksheet.Cells[row, 8].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 9].Value = dato.TotalTarjeta;
                worksheet.Cells[row, 9].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 10].Value = dato.TotalTransferencia;
                worksheet.Cells[row, 10].Style.Numberformat.Format = "$#,##0.00";
                row++;
            }
            
            // Autoajustar columnas
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            // Bordes
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        private void CrearHojaTopProductos(ExcelPackage package, ReporteVentasCompletoDTO reporte)
        {
            var worksheet = package.Workbook.Worksheets.Add("Top Productos");
            
            // Encabezados
            int row = 1;
            var headers = new[] { "Producto", "Categoría", "Código de Barras", "Cantidad Vendida", 
                "Total Ventas", "# Transacciones", "Precio Promedio" };
            
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cells[row, col + 1];
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            row++;
            
            // Datos
            foreach (var producto in reporte.TopProductos)
            {
                worksheet.Cells[row, 1].Value = producto.ProductoNombre;
                worksheet.Cells[row, 2].Value = producto.CategoriaNombre ?? "Sin categoría";
                worksheet.Cells[row, 3].Value = producto.CodigoBarras ?? "N/A";
                worksheet.Cells[row, 4].Value = producto.CantidadVendida;
                worksheet.Cells[row, 5].Value = producto.TotalVentas;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 6].Value = producto.NumeroTransacciones;
                worksheet.Cells[row, 7].Value = producto.PrecioPromedio;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "$#,##0.00";
                row++;
            }
            
            // Autoajustar columnas
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            // Bordes
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        private void CrearHojaVentasDetalladas(ExcelPackage package, ReporteVentasCompletoDTO reporte)
        {
            var worksheet = package.Workbook.Worksheets.Add("Ventas Detalladas");
            
            // Encabezados
            int row = 1;
            var headers = new[] { "Folio", "Fecha", "Cajero", "Cliente", "Método Pago", 
                "Subtotal", "IVA", "Descuento", "Total", "# Productos" };
            
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cells[row, col + 1];
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            row++;
            
            // Datos
            foreach (var venta in reporte.VentasDetalladas!)
            {
                worksheet.Cells[row, 1].Value = venta.Folio;
                worksheet.Cells[row, 2].Value = venta.FechaVenta;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
                worksheet.Cells[row, 3].Value = venta.NombreCajero;
                worksheet.Cells[row, 4].Value = venta.NombreCliente;
                worksheet.Cells[row, 5].Value = venta.MetodoPago;
                worksheet.Cells[row, 6].Value = venta.Subtotal;
                worksheet.Cells[row, 6].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 7].Value = venta.Iva;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 8].Value = venta.DescuentoAplicado;
                worksheet.Cells[row, 8].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 9].Value = venta.Total;
                worksheet.Cells[row, 9].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 10].Value = venta.CantidadProductos;
                row++;
            }
            
            // Autoajustar columnas
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            // Bordes
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[1, 1, row - 1, headers.Length].Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        private void AgregarFila(ExcelWorksheet worksheet, ref int row, string etiqueta, string valor)
        {
            worksheet.Cells[$"A{row}"].Value = etiqueta;
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
            worksheet.Cells[$"B{row}"].Value = valor;
            row++;
        }
    }
}
