using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using ThryftAiServer.Services.OutfitBuilder;
using ThryftAiServer.Services.Ai;
using ThryftAiServer.Services.Aws;
using ThryftAiServer.Services.Inventory;
using ThryftAiServer.Services.Listing;
using ThryftAiServer.Services.Discovery;
using ThryftAiServer.Services.Personalization;
using Amazon.S3;

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

// Register AWS & AI Services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<S3Service>();
builder.Services.AddScoped<ProductEnrichmentService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ListingAutofillService>();
builder.Services.AddScoped<VisualSearchService>();
builder.Services.AddScoped<PersonalizedRecommendationService>();
builder.Services.AddScoped<OutfitBuilderService>();
builder.Services.AddScoped<VisualOutfitBuilderService>();

var app = builder.Build();

app.UseCors("AllowAll");

// No seeding needed at runtime

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
