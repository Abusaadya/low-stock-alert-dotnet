using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Add DbContext
builder.Services.AddDbContext<SallaAlertApp.Api.Data.ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add WhatsApp Service
builder.Services.AddSingleton<SallaAlertApp.Api.Services.WhatsAppService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable wwwroot
app.UseAuthorization();

app.MapControllers();
// Serve settings.html at root/settings
app.MapGet("/settings", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/settings.html");
});

app.Run();
