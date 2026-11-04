using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;
using MiniDpp.Api.Import;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Bağıl SQLite yolu, uygulamanın çalıştırıldığı klasöre göre değişmesin diye içerik köküne bağlanır.
var connectionString = builder.Configuration.GetConnectionString("Dpp")
    ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "dpp.db")}";
builder.Services.AddDbContext<DppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<ProductImportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DppDbContext>().Database.Migrate();
}

app.MapControllers();

app.Run();

// WebApplicationFactory testlerinin Program sınıfına erişebilmesi için.
public partial class Program;
