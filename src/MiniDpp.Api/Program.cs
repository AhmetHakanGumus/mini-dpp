var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

// WebApplicationFactory testlerinin Program sınıfına erişebilmesi için.
public partial class Program;
