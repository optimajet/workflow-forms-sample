# Workflow Engine by Optimajet Forms sample

This sample demonstrates how to use FormsPlugin through the standard Workflow Engine Web API. It implements a weekly
report workflow with form editing, validation, command execution, optimistic version checks, Forms Manager, Workflow
Designer, and a read-only fallback form.

The sample uses Microsoft SQL Server and consists of:

- `WorkflowApi` — the ASP.NET Core backend;
- `frontend` — the React application;
- `WorkflowApi.IntegrationTests` — HTTP integration tests;
- `Scripts` — scripts for managing the sample database;
- `Forms.sln` — the backend solution.

The sample has no authentication. The selected user is a demo identity supplied by the browser and must not be treated
as a security boundary. A production application should obtain the identity from an authenticated context and configure
Workflow API authorization.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) compatible with React and Vite; the recommended version is specified in
  `frontend/.nvmrc`
- [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
- [Docker](https://www.docker.com/get-started) running Linux containers
- A Workflow Engine license that includes HTTP API and Forms
- A Form Engine license for the browser UI

## Configure licenses

Set the Workflow Engine license in `WorkflowApi/appsettings.json`:

```json
{
  "WorkflowApiCoreOptions": {
    "LicenseKey": "YOUR_WORKFLOW_ENGINE_LICENSE_KEY"
  }
}
```

Alternatively, provide it through the `WorkflowApiCoreOptions__LicenseKey` environment variable.

Set `licenseKey` in `frontend/src/config.ts` to your Form Engine license.
The frontend uses this ordinary TypeScript configuration file; no environment file is required.

## Start the database

From the repository root, create and initialize the MSSQL container:

```bash
pwsh ./Scripts/create-containers.ps1
```

The default connection string in `WorkflowApi/appsettings.json` is configured for this container. To use another MSSQL
instance, replace `WorkflowTenantCreationOptions:ConnectionString` with your connection string.

## Run the backend

```bash
dotnet run --project ./WorkflowApi
```

The backend starts at [http://localhost:5141](http://localhost:5141). Open
[Swagger UI](http://localhost:5141/swagger/index.html) to inspect and call the API.

The Workflow Designer endpoint is available at `http://localhost:5141/workflow-api/designer`.

## Verify the API

After starting the backend, call the readiness endpoint:

```bash
curl -X GET "http://localhost:5141/workflow-api/readiness" -H "Accept: application/json"
```

Using Windows PowerShell:

```bash
Invoke-RestMethod -Uri "http://localhost:5141/workflow-api/readiness" -Method Get -Headers @{"Accept"="application/json"}
```

## Run the frontend

In another terminal:

```bash
cd ./frontend
npm install
npm run dev
```

Open [http://localhost:5142](http://localhost:5142). Forms Manager is available at
[http://localhost:5142/formsManager](http://localhost:5142/formsManager), and Workflow Designer is available at
[http://localhost:5142/designer](http://localhost:5142/designer).

## Try the workflow

1. Select **Paula** and create an AppDev weekly report.
2. Fill in and submit the report.
3. Select **Peter** to review it.
4. Choose **Accept**, **Reject**, or **Rework**.
5. After the final transition, open the report to see the read-only `View` form.

The application stores form data in an external business table and uses the Forms API for loading, saving, validating,
and executing forms.

The endpoint should return `200 OK` with `"isHealthy": true`.

## Tests

Run the integration tests against a temporary MSSQL container:

```bash
pwsh ./Scripts/run-tests.ps1
```

Run the frontend checks from `frontend`:

```bash
npm run test
npm run lint
npm run build
```

## Cleanup

Remove the sample database container when it is no longer needed:

```bash
pwsh ./Scripts/remove-containers.ps1
```

## Configuration

Backend settings are stored in `WorkflowApi/appsettings.json`. If the frontend origin changes, update both
`Cors:Origins` and `Forms:FormsManagerUrl`.

The backend `Forms` section also supports:

- `AllowedFormNames` - the exact names that `get-form` may return;
- `ViewFallback:Enabled` - enables the backend read-only fallback (default: `true`);
- `ViewFallback:FormKey:FormName` - fallback definition (default: `View`);
- `ViewFallback:FormKey:FormVersion` - optional fixed version (default: latest).

The fallback data parameter is fixed by `WeeklyReportParameterProvider.ParameterName` in server code. A missing
fallback definition or version returns `404`; disabling the fallback preserves an empty successful list.

When a known user cannot read a WeeklyReport and no executable forms are available, the backend returns the
`DocumentAccessDenied` informational form instead of an empty list. It contains no report data, save action, or commands.
Migration `2000013` adds this form to new and existing databases without replacing an existing definition. Its message
can be customized in Forms Manager; the frontend does not need a special error-message implementation.

The sample applies fallback in `OnAfter` to any empty `Ok<GetExecutableFormsResponse>`, including successful results
returned by `OnBefore` or `OnException`. Non-success results and nonempty form lists remain unchanged.

Frontend settings are stored in `frontend/src/config.ts`:

- `apiUrl` - backend URL;
- `schemeCode` - workflow scheme code;
- `licenseKey` - Form Engine license;
- `tenantId` - optional Workflow API tenant identifier.

## Troubleshooting

- If Docker initialization fails, inspect `docker logs wfe-api-forms-mssql`.
- If ports are occupied, update port `41434` in the scripts and connection string, backend port `5141`, or frontend port
  `5142`. Keep the frontend URL, CORS origin, and Forms Manager URL consistent.
- For license errors, verify that the backend license includes HTTP API and Forms and that `frontend/src/config.ts` contains a valid Form
  Engine license.
- For CORS errors, ensure the exact frontend origin is present in `Cors:Origins` without a trailing slash.

## Documentation and support

- [Workflow Forms](https://docs.workflowengine.io/evaluate/workflow-engine-features/workflow-forms/)
- [Release notes](https://docs.workflowengine.io/release-notes/)
- [support@optimajet.com](mailto:support@optimajet.com)

Workflow Engine uses its commercial EULA. Forms Manager uses the FormEngine EULA; Forms Viewer is MIT-licensed.
