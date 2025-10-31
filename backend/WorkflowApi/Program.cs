using OptimaJet.Workflow.Core.Runtime;
using WorkflowLib;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string rule = "MyCorsRule";
builder.Services.Configure<WorkflowApiConfiguration>(builder.Configuration);
WorkflowApiConfiguration? apiConfiguration = builder.Configuration.Get<WorkflowApiConfiguration>();

if (apiConfiguration?.Cors?.Origins?.Count > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(rule, policy =>
        {
            policy.WithOrigins(apiConfiguration.Cors.Origins.ToArray());
            policy.AllowAnyHeader();
            policy.AllowCredentials();
        });
    });
}

builder.Services.AddSingleton<MsSqlProviderService>();
builder.Services.AddSingleton<WeeklyReportRepository>();
builder.Services.AddSingleton<WeeklyReportRuleProvider>();
builder.Services.AddSingleton<WeeklyReportActionProvider>();
builder.Services.AddSingleton<WeeklyReportParameterProvider>();
builder.Services.AddSingleton<WorkflowRuntimeService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors(rule);

app.MapControllers();

WorkflowRuntimeService? workflowRuntimeService = app.Services.GetService<WorkflowRuntimeService>();
if (workflowRuntimeService is null)
{
    throw new NullReferenceException("WorkflowRuntimeService is not registered");
}

await workflowRuntimeService.Runtime.StartAsync();

app.Run();
