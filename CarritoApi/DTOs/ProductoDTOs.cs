using System.ComponentModel.DataAnnotations;

namespace CarritoApi.DTOs
{
    // SALIDA del catálogo (lo llena Dapper): EstadoStock es TEXTO calculado en el SQL
    public class ProductoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string EstadoStock { get; set; } = string.Empty; // "Disponible" o "Agotado"
    }

    // ENTRADA para POST y PUT
    public class ProductoCrearDTO
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }
    }

    // ENTRADA para PATCH (todo opcional)
    public class ProductoPatchDTO
    {
        [StringLength(100)]
        public string? Nombre { get; set; }

        [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal? Precio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int? Stock { get; set; }
    }
}