using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using ThryftAiServer.Services.OutfitBuilder;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Configure DB
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                      ?? builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=app.db";

builder.Services.AddDbContext<ThryftAiServer.Data.App.AppDbContext>(options =>
{
    if (connectionString.Contains("Host=") || connectionString.Contains("Server="))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Configure Semantic Kernel
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                  ?? builder.Configuration["OpenAI:ApiKey"] 
                  ?? "your-api-key-here";
builder.Services.AddKernel()
    .AddOpenAIChatCompletion("gpt-4o-mini", openAiApiKey);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register Outfit Services
builder.Services.AddScoped<OutfitBuilderService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Database initialization and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ThryftAiServer.Data.App.AppDbContext>();

        Console.WriteLine("Force-dropping table for clean migration...");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"FashionProducts\" CASCADE;");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE;");
        Console.WriteLine("Applying migrations to AWS RDS...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Database schema sync successful.");
        
        // Seeding from CSV
        var stylesPath = Path.Combine(app.Environment.ContentRootPath, "Data", "App", "styles.csv");
        var imagesPath = Path.Combine(app.Environment.ContentRootPath, "Data", "App", "images.csv");
        await ThryftAiServer.Data.App.DataSeeder.SeedFromCsvAsync(context, stylesPath, imagesPath);

        Console.WriteLine("Data migration complete.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while resetting the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
