using VideoRentingSystem.Api.Agent;
using VideoRentingSystem.Api.Bootstrap;
using VideoRentingSystem.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// register MVC controllers and the OpenAPI / Swagger generator
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// allow the React dev-server origins so browser fetch calls succeed during development
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// singleton store state keeps custom in-memory indexes alive for the API lifetime
builder.Services.AddSingleton(StoreBootstrapper.Initialize());
builder.Services.AddSingleton<AuthSessionService>();
// AI agent is a singleton — stateless between requests, tools read from the shared store
builder.Services.AddSingleton<AgentService>();

var app = builder.Build();

// expose the OpenAPI spec only in development so it stays out of production
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// pipeline order matters: CORS headers must be set before auth reads them,
// and auth middleware must run before controller routing
app.UseCors("ClientDev");
app.UseMiddleware<AuthMiddleware>();
app.MapControllers();
app.Run();

// partial declaration allows WebApplicationFactory<Program> to access this entrypoint in tests
public partial class Program;
