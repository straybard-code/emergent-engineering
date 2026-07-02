using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var databaseConnectionOptions = new DatabaseConnectionOptions(ResolveConnectionStrings());

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(databaseConnectionOptions);
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options.UseSqlServer(serviceProvider.GetRequiredService<DatabaseConnectionOptions>().ConnectionString));
builder.Services.AddHttpClient();

var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    builder.Services.AddScoped<ILlmService, MockLlmService>();
}
else
{
    builder.Services.AddScoped<ILlmService, OpenAiLlmService>();
}

builder.Services.AddScoped<ISimulationRunner, SimulationRunner>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await MigrateDatabaseAsync(db, scope.ServiceProvider.GetRequiredService<DatabaseConnectionOptions>());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/api/simulations", async (AppDbContext db) =>
    await db.SimulationProjects
        .OrderByDescending(project => project.CreatedAt)
        .Select(project => new
        {
            project.Id,
            project.Name,
            project.Purpose,
            project.AgentCount,
            project.TotalSteps,
            project.CurrentStep,
            project.Status,
            project.Phase,
            project.CreatedAt
        })
        .ToListAsync());

app.MapGet("/api/simulations/{id:int}", async (int id, AppDbContext db) =>
{
    var project = await db.SimulationProjects
        .Include(project => project.Agents)
        .Include(project => project.Steps)
        .FirstOrDefaultAsync(project => project.Id == id);

    return project is null ? Results.NotFound() : Results.Ok(project);
});

app.MapPost("/api/simulations", async (CreateSimulationRequest request, AppDbContext db) =>
{
    var project = SimulationFactory.CreateProject(request);
    db.SimulationProjects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/simulations/{project.Id}", project);
});

app.MapPost("/api/simulations/{id:int}/run-one", async (int id, ISimulationRunner runner) =>
{
    var step = await runner.RunOneStepAsync(id);
    return step is null ? Results.BadRequest("Simulation not found or already completed.") : Results.Ok(step);
});

app.MapPost("/api/simulations/{id:int}/run-all", async (int id, ISimulationRunner runner) =>
{
    var project = await runner.RunAllAsync(id);
    return project is null ? Results.NotFound() : Results.Ok(project);
});

app.Run();

static string[] ResolveConnectionStrings()
{
    var fromEnvironment = Environment.GetEnvironmentVariable("ORG_SIM_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(fromEnvironment))
    {
        return [fromEnvironment];
    }

    return
    [
        "Server=(localdb)\\MSSQLLocalDB;Database=OrgSimDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
        "Server=.\\SQLEXPRESS;Database=OrgSimDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    ];
}

static async Task MigrateDatabaseAsync(AppDbContext db, DatabaseConnectionOptions databaseConnectionOptions)
{
    List<Exception> errors = [];

    foreach (var connectionString in databaseConnectionOptions.ConnectionStrings)
    {
        try
        {
            db.Database.SetConnectionString(connectionString);
            await db.Database.MigrateAsync();
            databaseConnectionOptions.ConnectionString = connectionString;
            return;
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }
    }

    throw new AggregateException("Unable to connect to SQL Server using the configured connection string candidates.", errors);
}

sealed class DatabaseConnectionOptions(string[] connectionStrings)
{
    public string[] ConnectionStrings { get; } = connectionStrings;
    public string ConnectionString { get; set; } = connectionStrings[0];
}
