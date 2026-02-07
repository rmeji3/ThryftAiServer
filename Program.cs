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
        await context.Database.EnsureCreatedAsync();

        /* Uncomment to seed or enrich data
        var imagePath = Path.Combine(app.Environment.ContentRootPath, "Data", "App", "train", "image");
        var annosPath = Path.Combine(app.Environment.ContentRootPath, "Data", "App", "train", "annos");
        await ThryftAiServer.Data.App.DataSeeder.SeedDataAsync(context, imagePath, annosPath);

        var kernel = services.GetRequiredService<Kernel>();
        await ThryftAiServer.Data.App.DataSeeder.EnrichWithVisionAsync(context, kernel, imagePath, limit: 500);
        */

        /* Migration complete - uncomment only if needed to re-sync
        var isPostgres = connectionString.Contains("Host=") || connectionString.Contains("Server=");
        if (isPostgres)
        {
            await ThryftAiServer.Data.App.DataSeeder.MigrateFromSqliteAsync(context, "Data Source=app.db");
        }
        */

        /* Uncomment to update DB links to S3
        await ThryftAiServer.Data.App.DataSeeder.UpdateUrlsToS3Async(context, "https://thryftai.s3.amazonaws.com");
        */
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
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
