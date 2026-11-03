using Microsoft.EntityFrameworkCore;
using CarritoApi.Data;
using CarritoApi.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ORM: EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// microORM: el repositorio con Dapper (sin esta línea, el controller no se puede crear)
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware global de errores: cualquier excepción no controlada termina aquí
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500; // Internal Server Error
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            Mensaje = "Ocurrió un error inesperado en el servidor.",
            Hora = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
