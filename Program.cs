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
builder.Services.AddDbContext<ThryftAiServer.Data.App.AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

// Configure Semantic Kernel
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                  ?? builder.Configuration["OpenAI:ApiKey"] 
                  ?? "your-api-key-here";
builder.Services.AddKernel()
    .AddOpenAIChatCompletion("gpt-4o-mini", openAiApiKey);

// Register Outfit Services
builder.Services.AddScoped<OutfitBuilderService>();

var app = builder.Build();

// Database initialization and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ThryftAiServer.Data.App.AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var imagePath = Path.Combine(app.Environment.ContentRootPath, "Data", "App", "train", "image");
        var annosPath = Path.Combine(app.Environment.ContentRootPath, "Data", "App", "train", "annos");
        await ThryftAiServer.Data.App.DataSeeder.SeedDataAsync(context, imagePath, annosPath);
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

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "Data", "App", "train", "image")),
    RequestPath = "/images"
});

app.MapControllers();

app.Run();
