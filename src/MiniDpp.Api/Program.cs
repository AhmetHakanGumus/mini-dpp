using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using MiniDpp.Api.Data;
using MiniDpp.Api.Import;
using MiniDpp.Api.Passports;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Razor Türkçe karakterleri de varlık koduna çevirmesin; < > & gibi karakterler yine kaçışlanır.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Bağıl SQLite yolu, uygulamanın çalıştırıldığı klasöre göre değişmesin diye içerik köküne bağlanır.
var connectionString = builder.Configuration.GetConnectionString("Dpp")
    ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "dpp.db")}";
builder.Services.AddDbContext<DppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<ProductImportService>();
builder.Services.AddScoped<PassportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DppDbContext>().Database.Migrate();
}

app.MapControllers();

app.Run();

// WebApplicationFactory testlerinin Program sınıfına erişebilmesi için.
public partial class Program;
