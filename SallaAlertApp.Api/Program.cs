using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Add DbContext
builder.Services.AddDbContext<SallaAlertApp.Api.Data.ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Telegram Service
builder.Services.AddSingleton<SallaAlertApp.Api.Services.TelegramService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disabled for Railway (SSL Termination at Edge)
app.UseStaticFiles(); // Enable wwwroot
app.UseAuthorization();

app.MapControllers();
// Serve settings.html at root/settings
app.MapGet("/settings", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "settings.html");
    await context.Response.SendFileAsync(path);
});


app.Run();
