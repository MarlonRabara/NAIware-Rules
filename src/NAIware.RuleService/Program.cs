using NAIware.RuleService.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Assembly loading is stateful and caches collectible load contexts; register as a singleton
// so model and translator DLLs are loaded once per process and share a single type identity.
builder.Services.AddSingleton<AssemblyModelLoader>();
builder.Services.AddScoped<ModelDeserializationService>();
builder.Services.AddScoped<RulesLibraryLoader>();
builder.Services.AddScoped<RuleEvaluationService>();

WebApplication app = builder.Build();

app.MapControllers();

app.Run();

/// <summary>
/// Program entry point. Declared public partial so the test host
/// (<c>WebApplicationFactory&lt;Program&gt;</c>) can reference it.
/// </summary>
public partial class Program;
