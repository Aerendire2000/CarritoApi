using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarritoApi.Models
{
    // Tabla "Carritos" (1 carrito por usuario)
    public class Carrito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        // El nombre debe ser IGUAL a la columna del script: FechaCreacion
        public DateTime FechaCreacion { get; set; }

        // Relación 1 a N con CarritoItems. HashSet: no permite el mismo objeto dos veces en memoria
        public ICollection<CarritoItem> Items { get; set; } = new HashSet<CarritoItem>();
    }
}