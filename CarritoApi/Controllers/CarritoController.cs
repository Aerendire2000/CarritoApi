using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarritoApi.Data;
using CarritoApi.DTOs;
using CarritoApi.Models;

namespace CarritoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // → api/carrito
    public class CarritoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CarritoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/carrito/1 → Include + ThenInclude (Carrito → Items → Producto) en 1 sola consulta
        [HttpGet("{usuarioId:int}")]
        public async Task<ActionResult<CarritoDTO>> ObtenerCarrito(int usuarioId)
        {
            var carrito = await _context.Carritos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (carrito is null)
                return NotFound($"El usuario {usuarioId} no tiene carrito.");

            return MapearCarrito(carrito);
        }

        // POST api/carrito/1/agregar   body: [ { "productoId": 2, "cantidad": 1 }, ... ]
        [HttpPost("{usuarioId:int}/agregar")]
        public async Task<ActionResult<CarritoDTO>> AgregarItems(int usuarioId, [FromBody] List<AgregarItemDTO> peticiones)
        {
            if (peticiones.Count == 0)
                return BadRequest("Envía al menos un producto.");

            // 1) Dictionary: agrupar por producto (si llega el mismo producto 2 veces, se suman)
            var itemsAgrupados = new Dictionary<int, int>(); // ProductoId → Cantidad
            foreach (var p in peticiones)
            {
                if (itemsAgrupados.ContainsKey(p.ProductoId))
                    itemsAgrupados[p.ProductoId] += p.Cantidad;
                else
                    itemsAgrupados[p.ProductoId] = p.Cantidad;
            }

            // 2) HashSet: Ids que sí existen en la BD (Contains es instantáneo, O(1))
            var idsPedidos = itemsAgrupados.Keys.ToList();

            var productos = await _context.Productos
                .Where(p => idsPedidos.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id); // ProductoId → Producto

            var idsExistentes = new HashSet<int>(productos.Keys);
            foreach (var id in idsPedidos)
            {
                if (!idsExistentes.Contains(id))
                    return NotFound($"Producto {id} no encontrado.");
            }

            // 3) Buscar el carrito del usuario (o crearlo si no tiene)
            var carrito = await _context.Carritos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (carrito is null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuarioId,
                    FechaCreacion = DateTime.Now
                };
                _context.Carritos.Add(carrito);
            }

            // 4) Sumar al item existente o agregar uno nuevo, validando stock
            foreach (var item in itemsAgrupados)
            {
                var producto = productos[item.Key];
                var itemExistente = carrito.Items.FirstOrDefault(i => i.ProductoId == item.Key);

                int cantidadFinal = (itemExistente?.Cantidad ?? 0) + item.Value;

                if (cantidadFinal > producto.Stock)
                    return BadRequest($"No hay suficiente stock para {producto.Nombre}. Stock disponible: {producto.Stock}.");

                if (itemExistente is not null)
                    itemExistente.Cantidad = cantidadFinal;
                else
                    carrito.Items.Add(new CarritoItem
                    {
                        ProductoId = item.Key,
                        Cantidad = item.Value
                    });
            }

            await _context.SaveChangesAsync();
            return await ObtenerCarrito(usuarioId);
        }

        // PATCH api/carrito/1/items/2   body: { "cantidad": 3 }
        [HttpPatch("{usuarioId:int}/items/{productoId:int}")]
        public async Task<IActionResult> CambiarCantidad(int usuarioId, int productoId, [FromBody] CambiarCantidadDTO dto)
        {
            var item = await _context.CarritoItems
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i => i.Carrito.UsuarioId == usuarioId && i.ProductoId == productoId);

            if (item is null)
                return NotFound($"El producto {productoId} no está en el carrito del usuario {usuarioId}.");

            if (dto.Cantidad > item.Producto.Stock)
                return BadRequest($"No hay suficiente stock para {item.Producto.Nombre}. Stock disponible: {item.Producto.Stock}.");

            item.Cantidad = dto.Cantidad;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/carrito/1/items/2 → quitar un producto del carrito
        [HttpDelete("{usuarioId:int}/items/{productoId:int}")]
        public async Task<IActionResult> QuitarItem(int usuarioId, int productoId)
        {
            var item = await _context.CarritoItems
                .FirstOrDefaultAsync(i => i.Carrito.UsuarioId == usuarioId && i.ProductoId == productoId);

            if (item is null)
                return NotFound($"El producto {productoId} no está en el carrito del usuario {usuarioId}.");

            _context.CarritoItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST api/carrito/1/confirmar → TRANSACCIÓN: descuenta stock y vacía el carrito (todo o nada)
        [HttpPost("{usuarioId:int}/confirmar")]
        public async Task<IActionResult> ConfirmarCompra(int usuarioId)
        {
            var carrito = await _context.Carritos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (carrito is null || carrito.Items.Count == 0)
                return BadRequest("El carrito está vacío.");

            var resumen = MapearCarrito(carrito);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Descontar el stock producto por producto
                foreach (var item in carrito.Items.OrderBy(i => i.Id))
                {
                    if (item.Producto.Stock < item.Cantidad)
                    {
                        await transaction.RollbackAsync(); // deshace lo que ya se descontó
                        return BadRequest($"Compra cancelada: no hay stock suficiente de {item.Producto.Nombre}. Stock disponible: {item.Producto.Stock}.");
                    }

                    item.Producto.Stock -= item.Cantidad;
                    await _context.SaveChangesAsync(); // se guarda, pero aún NO es permanente
                }

                // Vaciar el carrito
                _context.CarritoItems.RemoveRange(carrito.Items);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync(); // ÉXITO: todo queda guardado

                return Ok(new
                {
                    Mensaje = "Compra confirmada.",
                    resumen.Total,
                    Productos = resumen.Items
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(); // ERROR: se revierte todo
                throw; // el middleware global responde el 500
            }
        }

        // Convierte la entidad Carrito (con Items y Producto cargados) al DTO de salida
        private static CarritoDTO MapearCarrito(Carrito carrito)
        {
            var items = carrito.Items
                .OrderBy(i => i.Id)
                .Select(i => new CarritoItemDTO
                {
                    ProductoId = i.ProductoId,
                    Producto = i.Producto.Nombre,
                    PrecioUnitario = i.Producto.Precio,
                    Cantidad = i.Cantidad,
                    Subtotal = i.Producto.Precio * i.Cantidad
                })
                .ToList();

            return new CarritoDTO
            {
                UsuarioId = carrito.UsuarioId,
                Items = items,
                CantidadProductos = items.Sum(i => i.Cantidad),
                Total = items.Sum(i => i.Subtotal)
            };
        }
    }
}