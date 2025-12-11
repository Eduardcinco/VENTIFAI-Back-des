using System;
using VentifyAPI.Models;

namespace VentifyAPI.Services
{
    public class DescuentoService
    {
        /// <summary>
        /// Calcula el precio final de un producto aplicando el descuento si está activo
        /// </summary>
        public static decimal CalcularPrecioConDescuento(Producto producto)
        {
            // Si no hay descuento configurado, retornar precio normal
            if (!producto.DescuentoPorcentaje.HasValue || producto.DescuentoPorcentaje.Value <= 0)
            {
                return producto.PrecioVenta;
            }

            // Verificar si el descuento está dentro del rango de fechas
            var ahora = DateTime.Now;
            
            // Validar rango de fechas (si están configuradas)
            if (producto.DescuentoFechaInicio.HasValue && ahora < producto.DescuentoFechaInicio.Value)
            {
                return producto.PrecioVenta; // Descuento aún no inicia
            }
            
            if (producto.DescuentoFechaFin.HasValue && ahora > producto.DescuentoFechaFin.Value)
            {
                return producto.PrecioVenta; // Descuento ya expiró
            }

            // Validar rango de horas (si están configuradas)
            if (producto.DescuentoHoraInicio.HasValue && producto.DescuentoHoraFin.HasValue)
            {
                var horaActual = ahora.TimeOfDay;
                
                // Si hora fin > hora inicio (ej: 09:00 - 18:00)
                if (producto.DescuentoHoraFin.Value > producto.DescuentoHoraInicio.Value)
                {
                    if (horaActual < producto.DescuentoHoraInicio.Value || horaActual > producto.DescuentoHoraFin.Value)
                    {
                        return producto.PrecioVenta; // Fuera del horario
                    }
                }
                // Si hora fin < hora inicio (ej: 22:00 - 06:00, horario nocturno)
                else if (producto.DescuentoHoraFin.Value < producto.DescuentoHoraInicio.Value)
                {
                    if (horaActual < producto.DescuentoHoraInicio.Value && horaActual > producto.DescuentoHoraFin.Value)
                    {
                        return producto.PrecioVenta; // Fuera del horario nocturno
                    }
                }
            }

            // Aplicar descuento
            var porcentajeDescuento = producto.DescuentoPorcentaje.Value / 100m;
            var montoDescuento = producto.PrecioVenta * porcentajeDescuento;
            var precioFinal = producto.PrecioVenta - montoDescuento;
            
            // Redondear a 2 decimales
            return Math.Round(precioFinal, 2);
        }

        /// <summary>
        /// Verifica si un descuento está activo en este momento
        /// </summary>
        public static bool DescuentoEstaActivo(Producto producto)
        {
            if (!producto.DescuentoPorcentaje.HasValue || producto.DescuentoPorcentaje.Value <= 0)
            {
                return false;
            }

            var ahora = DateTime.Now;
            
            // Validar fechas
            if (producto.DescuentoFechaInicio.HasValue && ahora < producto.DescuentoFechaInicio.Value)
                return false;
            
            if (producto.DescuentoFechaFin.HasValue && ahora > producto.DescuentoFechaFin.Value)
                return false;

            // Validar horas
            if (producto.DescuentoHoraInicio.HasValue && producto.DescuentoHoraFin.HasValue)
            {
                var horaActual = ahora.TimeOfDay;
                
                if (producto.DescuentoHoraFin.Value > producto.DescuentoHoraInicio.Value)
                {
                    if (horaActual < producto.DescuentoHoraInicio.Value || horaActual > producto.DescuentoHoraFin.Value)
                        return false;
                }
                else if (producto.DescuentoHoraFin.Value < producto.DescuentoHoraInicio.Value)
                {
                    if (horaActual < producto.DescuentoHoraInicio.Value && horaActual > producto.DescuentoHoraFin.Value)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calcula el ahorro generado por el descuento
        /// </summary>
        public static decimal CalcularAhorro(Producto producto)
        {
            if (!DescuentoEstaActivo(producto))
                return 0;

            var precioOriginal = producto.PrecioVenta;
            var precioConDescuento = CalcularPrecioConDescuento(producto);
            
            return Math.Round(precioOriginal - precioConDescuento, 2);
        }
    }
}
