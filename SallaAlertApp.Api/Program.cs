using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add Subscription Service
builder.Services.AddScoped<SallaAlertApp.Api.Services.SubscriptionService>();
builder.Services.AddScoped<SallaAlertApp.Api.Services.ReportService>();
builder.Services.AddHostedService<SallaAlertApp.Api.Services.ReportScheduler>();

builder.Services.AddControllers();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Check for Railway's DATABASE_URL and override if present
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    try 
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        
        connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true;";
        
        Console.WriteLine($"[Railway Setup] DATABASE_URL found.");
        Console.WriteLine($"[Railway Setup] Host: {uri.Host}, Port: {port}, User: {username}");
        Console.WriteLine($"[Railway Setup] Password Length: {password.Length}"); 
        Console.WriteLine($"[Deployment] Force Update: Login Screen Fix Applied"); 
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
    }
}

builder.Services.AddDbContext<SallaAlertApp.Api.Data.ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Telegram Service
builder.Services.AddSingleton<SallaAlertApp.Api.Services.TelegramService>();
builder.Services.AddHttpClient<SallaAlertApp.Api.Services.EmailService>();

// Add Subscription Service
builder.Services.AddScoped<SallaAlertApp.Api.Services.SubscriptionService>();

var app = builder.Build();

// Auto-apply migrations on startup (for Railway deployment)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SallaAlertApp.Api.Data.ApplicationDbContext>();
    db.Database.Migrate();
}

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

// Health check endpoint for monitoring services
app.MapGet("/health", () => Results.Json(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Serve landing page at root
app.MapGet("/", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "index.html");
    await context.Response.SendFileAsync(path);
});

// Serve settings.html at root/settings
app.MapGet("/settings", async (HttpContext context) => {
    // DEBUG: Verify if code is updated
    await context.Response.WriteAsync("DEBUG: SERVER IS UPDATED. SETTINGS PAGE SHOULD BE HERE.");
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

app.MapGet("/contact", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "contact.html");
    await context.Response.SendFileAsync(path);
});

app.MapGet("/how-it-works", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "how-it-works.html");
    await context.Response.SendFileAsync(path);
});

app.MapGet("/faqs", async (HttpContext context) => {
    context.Response.ContentType = "text/html";
    var path = Path.Combine(app.Environment.WebRootPath, "faqs.html");
    await context.Response.SendFileAsync(path);
});

// Redirect .html versions to clean URLs
app.MapGet("/privacy.html", () => Results.Redirect("/privacy"));
app.MapGet("/terms.html", () => Results.Redirect("/terms"));
app.MapGet("/contact.html", () => Results.Redirect("/contact"));
app.MapGet("/how-it-works.html", () => Results.Redirect("/how-it-works"));
app.MapGet("/faqs.html", () => Results.Redirect("/faqs"));

app.Run();
