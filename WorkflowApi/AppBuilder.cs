using System.Net.Mime;
using Microsoft.AspNetCore.Diagnostics;
using OptimaJet.Workflow.Api;
using OptimaJet.Workflow.Api.Forms;
using OptimaJet.Workflow.Api.Mssql;
using OptimaJet.Workflow.Api.Options;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.Workflow.Migrator;
using WorkflowApi.Data;
using WorkflowApi.Data.Migrations;
using WorkflowApi.Controllers;
using WorkflowApi.Forms;
using WorkflowApi.Workflow;

namespace WorkflowApi;

public sealed class AppBuilder
{
    public AppBuilder(params string[] args)
        : this(new WebApplicationOptions { Args = args })
    {
    }

    public AppBuilder(WebApplicationOptions options)
    {
        Builder = WebApplication.CreateBuilder(options);
    }

    public WebApplicationBuilder Builder { get; }

    public Configuration Configuration => Builder.Configuration.Get<Configuration>() ?? new Configuration();

    public void ConfigureServices()
    {
        if (_servicesConfigured)
        {
            return;
        }

        var configuration = Configuration;
        var repository = new WeeklyReportRepository(configuration.WorkflowTenantCreationOptions.ConnectionString);
        var actionProvider = new WeeklyReportActionProvider();
        var ruleProvider = new WeeklyReportRuleProvider();
        var parameterProvider = new WeeklyReportParameterProvider(repository);
        var validator = new WeeklyReportValidator();

        Builder.Services.AddSingleton(configuration);
        Builder.Services.AddSingleton(repository);
        Builder.Services.AddSingleton(actionProvider);
        Builder.Services.AddSingleton(ruleProvider);
        Builder.Services.AddSingleton(parameterProvider);
        Builder.Services.AddSingleton(validator);
        Builder.Services.AddControllers().AddApplicationPart(typeof(UsersController).Assembly);
        Builder.Services.AddEndpointsApiExplorer();
        Builder.Services.AddSwaggerGen(options => options.OrderActionsBy(_ => String.Empty));
        Builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(configuration.Cors.Origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        Builder.Services.AddWorkflowApiMssql();
        Builder.Services.AddWorkflowApiCore(options => CopyCoreOptions(configuration.WorkflowApiCoreOptions, options));
        Builder.Services.AddWorkflowRuntime(options =>
        {
            options.PersistenceProviderId = configuration.WorkflowTenantCreationOptions.PersistenceProviderId;
            options.ConnectionString = configuration.WorkflowTenantCreationOptions.ConnectionString;
            options.DataProviderCreationOptions = configuration.WorkflowTenantCreationOptions.DataProviderCreationOptions;
            options.WorkflowProviderCreationOptions = configuration.WorkflowTenantCreationOptions.WorkflowProviderCreationOptions;
            options.WorkflowRuntimeCreationOptions.DisableMigrationsExecution = true;
            options.WorkflowRuntimeCreationOptions.ConfigureWorkflowRuntime = runtime => ConfigureWorkflowRuntime(
                runtime,
                actionProvider,
                ruleProvider,
                parameterProvider);
        });
        Builder.Services.AddFormsPlugin(options =>
        {
            options.FormsManagerUrl = configuration.Forms.FormsManagerUrl;
            options.FormVersionPropertyName = nameof(WeeklyReportData.Version);
            options.FormValidator = validator;
        });
        Builder.Services.AddFormDataPipelineStep<CamelCaseFormsApiFormDataPipeline>();
        Builder.Services.ConfigureWorkflowApi(FormsApiConfiguration.Configure);

        _servicesConfigured = true;
    }

    public WebApplication Build()
    {
        ConfigureServices();
        var app = Builder.Build();

        app.UseExceptionHandler(exceptionHandler => exceptionHandler.Run(WriteExceptionAsync));
        app.UseCors(CorsPolicyName);
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapWorkflowApi();
        app.MapControllers();

        return app;
    }

    private static void CopyCoreOptions(WorkflowApiCoreOptions source, WorkflowApiCoreOptions destination)
    {
        destination.BasePath = source.BasePath;
        destination.LicenseKey = source.LicenseKey;
        destination.DefaultTenantId = source.DefaultTenantId;
        destination.ProtectedGlobalParameterTypes = new HashSet<string>(
            source.ProtectedGlobalParameterTypes,
            StringComparer.Ordinal);
    }

    private static void ConfigureWorkflowRuntime(
        WorkflowRuntime runtime,
        WeeklyReportActionProvider actionProvider,
        WeeklyReportRuleProvider ruleProvider,
        WeeklyReportParameterProvider parameterProvider)
    {
        runtime
            .EnableCodeActions()
            .SwitchAutoUpdateSchemeBeforeGetAvailableCommandsOn()
            .WithRuleProvider(ruleProvider)
            .WithActionProvider(actionProvider)
            .WithExternalParametersProvider(parameterProvider)
            .RunMigrations()
            .RunCustomMigration(typeof(CreateBusinessObjectsMigration).Assembly)
            .AsSingleServer();
    }

    private static async Task WriteExceptionAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        context.Response.StatusCode = exception is BadHttpRequestException badRequestException
            ? badRequestException.StatusCode
            : StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsJsonAsync(new
        {
            message = exception?.Message ?? "An unexpected error occurred."
        });
    }

    private const string CorsPolicyName = "FormsSample";
    private bool _servicesConfigured;
}
