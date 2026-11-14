using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Antiforgery;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

// adding cors config
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for Antiforgery cookies!
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Configure Anti-Forgery to look for X-Xsrf-Token header
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-Xsrf-Token";
});

// 1. Get the connection string from appsettings.json
var azureConnectionString = builder.Configuration.GetConnectionString("AzureBlobStorage");
// 2. Register BlobServiceClient as a Singleton service
builder.Services.AddSingleton(x => new BlobServiceClient(azureConnectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles(); // Automatically looks for index.html
app.UseStaticFiles();  // Serves JS, CSS, and images
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();

app.MapControllers();
// Endpoint for React / Burp to obtain the CSRF Cookie and Request Token
app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
});
app.Run();
