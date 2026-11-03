using Dapper;
using Microsoft.Data.SqlClient;
using CarritoApi.DTOs;

namespace CarritoApi.Data.Repositories
{
    public interface IProductoRepository
    {
        Task<IEnumerable<ProductoDTO>> ObtenerCatalogoAsync();
        Task<IEnumerable<ProductoDTO>> BuscarPorPrecioMaximoAsync(decimal precioMaximo);
    }

    // microORM (Dapper): SQL escrito a mano + mapeo automático a ProductoDTO
    public class ProductoRepository : IProductoRepository
    {
        private readonly string _connectionString;

        public ProductoRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IEnumerable<ProductoDTO>> ObtenerCatalogoAsync()
        {
            using var conn = new SqlConnection(_connectionString);

            var sql = @"
                SELECT Id, Nombre, Precio, Stock,
                       CASE WHEN Stock > 0 THEN 'Disponible' ELSE 'Agotado' END AS EstadoStock
                FROM Productos
                ORDER BY Nombre";

            return await conn.QueryAsync<ProductoDTO>(sql);
        }

        public async Task<IEnumerable<ProductoDTO>> BuscarPorPrecioMaximoAsync(decimal precioMaximo)
        {
            using var conn = new SqlConnection(_connectionString);

            var sql = @"
                SELECT Id, Nombre, Precio, Stock,
                       CASE WHEN Stock > 0 THEN 'Disponible' ELSE 'Agotado' END AS EstadoStock
                FROM Productos
                WHERE Precio <= @PrecioMaximo
                ORDER BY Precio";

            // El objeto anónimo llena el parámetro @PrecioMaximo (sin inyección SQL)
            return await conn.QueryAsync<ProductoDTO>(sql, new { PrecioMaximo = precioMaximo });
        }
    }
}