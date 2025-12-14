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

// Serve landing page at root
app.MapGet("/", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "index.html");
    await context.Response.SendFileAsync(path);
});

// Serve settings.html at root/settings
app.MapGet("/settings", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "settings.html");
    await context.Response.SendFileAsync(path);
});


// Serve privacy and terms pages
app.MapGet("/privacy", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "privacy.html");
    await context.Response.SendFileAsync(path);
});

app.MapGet("/terms", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "terms.html");
    await context.Response.SendFileAsync(path);
});

// Redirect .html versions to clean URLs
app.MapGet("/privacy.html", () => Results.Redirect("/privacy"));
app.MapGet("/terms.html", () => Results.Redirect("/terms"));

app.Run();
