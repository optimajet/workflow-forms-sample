using System;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using OptimaJet.Workflow.Core.Builder;
using OptimaJet.Workflow.Core.Parser;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.Workflow.Migrator;
using OptimaJet.Workflow.Plugins.FormsPlugin;
using WorkflowLib.Migrations;

namespace WorkflowLib;

public class WorkflowRuntimeService
{
    public WorkflowRuntime Runtime { get; private set; }

    public WorkflowRuntimeService(MsSqlProviderService workflowProviderLocator, WeeklyReportRuleProvider weeklyReportRuleProvider,
        WeeklyReportActionProvider weeklyReportActionProvider, WeeklyReportParameterProvider weeklyReportParameterProvider, IConfiguration configRoot)
    {
        WorkflowApiConfiguration? configuration = configRoot.Get<WorkflowApiConfiguration>();

        if (configuration?.Workflow == null)
        {
            throw new Exception("Runtime settings is not set");
        }

        if (configuration.Workflow.FormsManagerUrl == null)
        {
            throw new Exception("Runtime.FormsManagerUrl setting is not set");
        }

        if (!string.IsNullOrWhiteSpace(configuration.Workflow.LicenseKey))
        {
            WorkflowRuntime.RegisterLicense(configuration.Workflow.LicenseKey);
        }

        IWorkflowBuilder builder = new WorkflowBuilder<XElement>(
            workflowProviderLocator.Provider,
            new XmlWorkflowParser(),
            workflowProviderLocator.Provider
        ).WithDefaultCache();

        var formsPluginSettings = new FormsPluginSettings
        {
            FormsManagerUrl = configuration.Workflow.FormsManagerUrl,
            FormVersionPropertyName = nameof(WeeklyReportData.Version),
            FormValidator = new WeeklyReportValidator()
        };

        var formsPlugin = new FormsPlugin(formsPluginSettings);
        WorkflowRuntime runtime = new WorkflowRuntime()
            .WithPlugin(formsPlugin)
            .WithBuilder(builder)
            .WithPersistenceProvider(workflowProviderLocator.Provider)
            .EnableCodeActions()
            .SwitchAutoUpdateSchemeBeforeGetAvailableCommandsOn()
            .WithRuleProvider(weeklyReportRuleProvider)
            .WithActionProvider(weeklyReportActionProvider)
            .WithExternalParametersProvider(weeklyReportParameterProvider)
            .RunMigrations()
            .RunCustomMigration(typeof(CreateBusinessObjectsMigration).Assembly)
            .AsSingleServer();

        Runtime = runtime;
    }
}
