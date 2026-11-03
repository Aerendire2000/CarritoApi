using System.ComponentModel.DataAnnotations;

namespace CarritoApi.DTOs
{
    // ENTRADA: un producto que se quiere agregar al carrito
    public class AgregarItemDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El Id del producto es obligatorio.")]
        public int ProductoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser un valor positivo.")]
        public int Cantidad { get; set; } = 1;
    }

    // ENTRADA: nueva cantidad de un item (PATCH)
    public class CambiarCantidadDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser un valor positivo.")]
        public int Cantidad { get; set; }
    }

    // SALIDA: el carrito con sus productos y el total
    public class CarritoDTO
    {
        public int UsuarioId { get; set; }
        public List<CarritoItemDTO> Items { get; set; } = new();
        public int CantidadProductos { get; set; }
        public decimal Total { get; set; }
    }

    public class CarritoItemDTO
    {
        public int ProductoId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }
}
