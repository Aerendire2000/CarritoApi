using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarritoApi.Data;
using CarritoApi.Data.Repositories;
using CarritoApi.DTOs;
using CarritoApi.Models;

namespace CarritoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // → api/productos
    public class ProductosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;            // ORM (EF Core)
        private readonly IProductoRepository _productoRepository;  // microORM (Dapper)

        public ProductosController(ApplicationDbContext context, IProductoRepository productoRepository)
        {
            _context = context;
            _productoRepository = productoRepository;
        }

        // GET api/productos → catálogo con Dapper
        [HttpGet]
        public async Task<IActionResult> ObtenerCatalogo()
        {
            return Ok(await _productoRepository.ObtenerCatalogoAsync());
        }

        // GET api/productos/buscar?precioMaximo=200 → Dapper con parámetro
        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarPorPrecio([FromQuery] decimal precioMaximo)
        {
            return Ok(await _productoRepository.BuscarPorPrecioMaximoAsync(precioMaximo));
        }

        // GET api/productos/1 → EF Core
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Producto>> GetById(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto is null)
                return NotFound($"No existe el producto con Id {id}.");

            return producto;
        }

        // POST api/productos → EF Core
        [HttpPost]
        public async Task<ActionResult<Producto>> Create([FromBody] ProductoCrearDTO dto)
        {
            var producto = new Producto
            {
                Nombre = dto.Nombre,
                Precio = dto.Precio,
                Stock = dto.Stock
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // 201 Created + cabecera Location: api/productos/{id}
            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
        }

        // PUT api/productos/1 → EF Core
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductoCrearDTO dto)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto is null)
                return NotFound($"No existe el producto con Id {id}.");

            producto.Nombre = dto.Nombre;
            producto.Precio = dto.Precio;
            producto.Stock = dto.Stock;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PATCH api/productos/1 → EF Core (solo cambia lo que llega)
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] ProductoPatchDTO dto)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto is null)
                return NotFound($"No existe el producto con Id {id}.");

            if (dto.Nombre is not null) producto.Nombre = dto.Nombre;
            if (dto.Precio is not null) producto.Precio = dto.Precio.Value;
            if (dto.Stock is not null) producto.Stock = dto.Stock.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/productos/1 → EF Core
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto is null)
                return NotFound($"No existe el producto con Id {id}.");

            if (await _context.CarritoItems.AnyAsync(ci => ci.ProductoId == id))
                return Conflict("No se puede eliminar: el producto está en un carrito.");

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET api/productos/probar-error → solo para ver el middleware global de errores
        [HttpGet("probar-error")]
        public IActionResult ProbarError()
        {
            throw new InvalidOperationException("Error de prueba lanzado desde el controller.");
        }
    }
}