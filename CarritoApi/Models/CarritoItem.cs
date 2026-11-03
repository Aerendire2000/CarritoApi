using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarritoApi.Models
{
    // Tabla intermedia "CarritoItems": une Carritos y Productos (muchos a muchos)
    public class CarritoItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CarritoId { get; set; }

        public int ProductoId { get; set; }

        public int Cantidad { get; set; } = 1;

        // Propiedades de navegación
        public Carrito Carrito { get; set; } = null!;

        public Producto Producto { get; set; } = null!;
    }
}