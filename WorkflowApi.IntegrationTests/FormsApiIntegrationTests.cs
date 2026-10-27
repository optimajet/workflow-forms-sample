using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OptimaJet.Workflow.Api;
using OptimaJet.Workflow.Api.Forms;
using OptimaJet.Workflow.Api.Forms.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WorkflowApi.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class FormsApiIntegrationTests
{
    [ClassInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        _connectionString = Environment.GetEnvironmentVariable("FORMS_TEST_CONNECTION_STRING")
                            ?? "Server=localhost,41434;Database=workflow_forms_tests;User Id=SA;Password=P@ssw0rd;TrustServerCertificate=True;";
        (_app, _client) = await StartApplicationAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [TestMethod]
    public async Task MigrationsSeedDatabaseAndAreIdempotentTest()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        Assert.AreEqual(1, await ExecuteScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'WeeklyReport'"));
        Assert.AreEqual(1, await ExecuteScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM dbo.WorkflowScheme WHERE Code = 'WeeklyReportProcess'"));
        Assert.IsTrue(await ExecuteScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM dbo.WorkflowForm WHERE Name IN ('Created', 'Review', 'Rework', 'Delete', 'View')") >= 5);
        Assert.AreEqual(1, await ExecuteScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM dbo.WorkflowForm WHERE Name = 'DocumentAccessDenied'"));

        var (secondApp, secondClient) = await StartApplicationAsync();
        try
        {
            var response = await secondClient.GetAsync("/api/users/all");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            secondClient.Dispose();
            await secondApp.StopAsync();
            await secondApp.DisposeAsync();
        }

        Assert.AreEqual(1, await ExecuteScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM dbo.WorkflowScheme WHERE Code = 'WeeklyReportProcess'"));
        Assert.AreEqual(1, await ExecuteScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM dbo.WorkflowForm WHERE Name = 'DocumentAccessDenied'"));
    }

    [TestMethod]
    public async Task GetFormAndDesignerContractReturnSeededFormsTest()
    {
        var getForm = await PostJsonAsync("/workflow-api/forms/get-form", new
        {
            formKey = new { formName = "T:EmployeeFields" }
        });
        Assert.AreEqual("T:EmployeeFields", getForm["form"]?["formKey"]?["formName"]?.GetValue<string>());
        Assert.IsNotNull(getForm["form"]?["formKey"]?["formVersion"]);
        StringAssert.Contains(getForm["form"]?["formCode"]?.GetValue<string>(), "whatWasDone");

        using var designerRequest = new HttpRequestMessage(HttpMethod.Post, "/workflow-api/designer")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["operation"] = "formsplugin_getformnames"
            })
        };
        var designerResponse = await _client.SendAsync(designerRequest);
        var designerBody = await designerResponse.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, designerResponse.StatusCode, designerBody);
        StringAssert.Contains(designerBody, "Created");
        StringAssert.Contains(designerBody, "View");
    }

    [TestMethod]
    public async Task DesignerCanUpdateSharedFormInSingleTenantModeTest()
    {
        using var getFormResponse = await SendDesignerRequestAsync(new Dictionary<string, string>
        {
            ["operation"] = "formsplugin_getform",
            ["formName"] = "Created"
        });
        var form = (await ReadJsonAsync(getFormResponse)).AsObject();
        var formVersion = form["Version"]!.GetValue<int>();
        var lockValue = form["Lock"]!.GetValue<int>();

        using var updateFormResponse = await SendDesignerRequestAsync(new Dictionary<string, string>
        {
            ["operation"] = "formsplugin_updateform",
            ["formName"] = "Created",
            ["formVersion"] = formVersion.ToString(CultureInfo.InvariantCulture),
            ["lock"] = lockValue.ToString(CultureInfo.InvariantCulture),
            ["definition"] = form["Definition"]!.GetValue<string>()
        });
        var updateBody = await updateFormResponse.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, updateFormResponse.StatusCode, updateBody);
        Assert.AreEqual(lockValue + 1, JsonNode.Parse(updateBody)!.GetValue<int>());
    }

    [TestMethod]
    public async Task RawLowerCamelCasePayloadCompletesWeeklyReportWorkflowTest()
    {
        var processId = Guid.NewGuid();
        await PostJsonAsync("/workflow-api/rpc/create-instance", new
        {
            schemeCode = "WeeklyReportProcess",
            processId,
            identityId = "Paula"
        });

        var executable = await GetExecutableFormsAsync(processId, "Paula");
        var createdForm = executable["forms"]!.AsArray()
            .Select(form => form!.AsObject())
            .Single(form => form["formKey"]?["formName"]?.GetValue<string>() == "Created");
        var formKey = createdForm["formKey"]!.DeepClone();
        var initialData = createdForm["formData"]!.AsObject();
        Assert.IsTrue(initialData.ContainsKey("submittedBy"));
        Assert.IsTrue(initialData.ContainsKey("version"));
        Assert.IsFalse(initialData.ContainsKey("SubmittedBy"));

        var invalidData = initialData.DeepClone().AsObject();
        invalidData["whatWasDone"] = "";
        invalidData["encounteredProblems"] = null;
        var validationResponse = await _client.PostAsJsonAsync("/workflow-api/forms/save-form", new
        {
            processId,
            formKey,
            formData = invalidData,
            identityId = "Paula",
            conditionCheck = true
        });
        var validationBody = await ReadJsonAsync(validationResponse);
        Assert.AreEqual(HttpStatusCode.BadRequest, validationResponse.StatusCode);
        Assert.AreEqual("ValidationError", validationBody["code"]?.GetValue<string>());
        Assert.AreEqual("One or more validation errors occurred.", validationBody["message"]?.GetValue<string>());
        Assert.IsNotNull(validationBody["errors"]?["whatWasDone"]);
        Assert.IsNotNull(validationBody["errors"]?["encounteredProblems"]);

        var validData = initialData.DeepClone().AsObject();
        validData["whatWasDone"] = "Implemented the Web API sample";
        validData["encounteredProblems"] = "None";
        validData["nestedObject"] = new JsonObject { ["managerReview"] = null };
        validData["items"] = new JsonArray(
            new JsonObject { ["itemName"] = "first" },
            null,
            JsonValue.Create(7));
        var save = await PostJsonAsync("/workflow-api/forms/save-form", new
        {
            processId,
            formKey,
            formData = validData,
            identityId = "Paula",
            conditionCheck = true
        });
        var savedData = save["data"]!.AsObject();
        Assert.AreEqual("first", savedData["items"]?[0]?["itemName"]?.GetValue<string>());
        Assert.IsNull(savedData["nestedObject"]?["managerReview"]);
        Assert.IsTrue(savedData["version"]!.GetValue<int>() > initialData["version"]!.GetValue<int>());

        var staleResponse = await _client.PostAsJsonAsync("/workflow-api/forms/save-form", new
        {
            processId,
            formKey,
            formData = validData,
            identityId = "Paula",
            conditionCheck = true
        });
        var staleBody = await ReadJsonAsync(staleResponse);
        Assert.AreEqual(HttpStatusCode.BadRequest, staleResponse.StatusCode);
        Assert.AreEqual("BadRequest", staleBody["code"]?.GetValue<string>());
        StringAssert.Contains(staleBody["message"]?.GetValue<string>(), "Form version mismatch");

        var execute = await PostJsonAsync("/workflow-api/forms/execute-form", new
        {
            processId,
            formKey,
            commandName = "Submit",
            formData = savedData,
            identityId = "Paula"
        });
        Assert.IsTrue(execute["wasExecuted"]!.GetValue<bool>());

        var reviewForms = await GetExecutableFormsAsync(processId, "Peter");
        var reviewForm = reviewForms["forms"]!.AsArray()
            .Select(form => form!.AsObject())
            .Single(form => form["formKey"]?["formName"]?.GetValue<string>() == "Review");
        var reviewData = reviewForm["formData"]!.AsObject();
        Assert.AreEqual("first", reviewData["items"]?[0]?["itemName"]?.GetValue<string>());
        reviewData["managerReview"] = "Approved";
        var accept = await PostJsonAsync("/workflow-api/forms/execute-form", new
        {
            processId,
            formKey = reviewForm["formKey"],
            commandName = "Accept",
            formData = reviewData,
            identityId = "Peter"
        });
        Assert.IsTrue(accept["wasExecuted"]!.GetValue<bool>());

        var viewResponse = await GetExecutableFormsAsync(processId, "Paula");
        var view = viewResponse["forms"]!.AsArray().Single()!;
        Assert.AreEqual("View", view["formKey"]?["formName"]?.GetValue<string>());
        Assert.AreEqual(processId, view["processId"]!.GetValue<Guid>());
        Assert.IsFalse(view["allowSave"]!.GetValue<bool>());
        Assert.AreEqual(0, view["commands"]!.AsArray().Count);
        Assert.AreEqual("Accepted", view["formData"]?["stateName"]?.GetValue<string>());
        Assert.AreEqual("first", view["formData"]?["items"]?[0]?["itemName"]?.GetValue<string>());
        Assert.IsNull(view["formData"]?["nestedObject"]?["managerReview"]);
        Assert.IsNotNull(view["formData"]?["version"]);
        Assert.IsFalse(view["formData"]!.AsObject().ContainsKey("Version"));

        var reports = await _client.GetAsync("/api/reports/data/query?user=Peter&skip=0&take=10");
        var reportsBody = await ReadJsonAsync(reports);
        Assert.AreEqual(HttpStatusCode.OK, reports.StatusCode);
        var persistedReport = reportsBody.AsArray()
            .Single(report => report?["id"]?.GetValue<Guid>() == processId)!;
        Assert.AreEqual("first", persistedReport["items"]?[0]?["itemName"]?.GetValue<string>());
        Assert.IsNull(persistedReport["nestedObject"]?["managerReview"]);

        var reportCount = await _client.GetAsync("/api/reports/data/count?user=Peter");
        Assert.AreEqual(HttpStatusCode.OK, reportCount.StatusCode);
        Assert.IsTrue((await ReadJsonAsync(reportCount)).GetValue<int>() >= 1);

        var otherDivisionReports = await _client.GetAsync("/api/reports/data/query?user=Steven&skip=0&take=10");
        var otherDivisionBody = await ReadJsonAsync(otherDivisionReports);
        Assert.AreEqual(HttpStatusCode.OK, otherDivisionReports.StatusCode);
        Assert.IsFalse(otherDivisionBody.AsArray().Any(report => report?["id"]?.GetValue<Guid>() == processId));
    }

    [TestMethod]
    public async Task BusinessErrorsMalformedRequestsAndCorsHavePredictableContractTest()
    {
        var users = await _client.GetFromJsonAsync<JsonArray>("/api/users/all");
        Assert.IsNotNull(users);
        Assert.IsTrue(users.Any(user => user?["name"]?.GetValue<string>() == "Paula"));
        Assert.IsTrue(users.Any(user => user?["name"]?.GetValue<string>() == "Peter"));

        var nullRequest = await _client.PostAsync(
            "/workflow-api/forms/get-form",
            JsonContent.Create<object?>(null));
        Assert.AreEqual(HttpStatusCode.BadRequest, nullRequest.StatusCode);

        var malformed = await _client.PostAsJsonAsync("/workflow-api/forms/get-form", new
        {
            formKey = new { formName = " " }
        });
        Assert.AreEqual(HttpStatusCode.Forbidden, malformed.StatusCode);

        var invalidFormData = await _client.PostAsJsonAsync("/workflow-api/forms/save-form", new
        {
            processId = Guid.NewGuid(),
            formKey = new { formName = "Created" },
            formData = 42,
            identityId = "Paula"
        });
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidFormData.StatusCode);

        var invalidConfigurationProcessId = Guid.NewGuid();
        await PostJsonAsync("/workflow-api/rpc/create-instance", new
        {
            schemeCode = "WeeklyReportProcess",
            processId = invalidConfigurationProcessId,
            identityId = "Paula"
        });
        await PostJsonAsync("/workflow-api/rpc/set-process-parameter", new
        {
            processId = invalidConfigurationProcessId,
            name = "FormsPlugin_ProcessForms",
            value = (object?)null
        });
        var invalidConfiguration = await _client.PostAsJsonAsync("/workflow-api/forms/get-executable-forms", new
        {
            processId = invalidConfigurationProcessId,
            identityId = "Paula",
            conditionCheck = true
        });
        var invalidConfigurationBody = await ReadJsonAsync(invalidConfiguration);
        Assert.AreEqual(HttpStatusCode.InternalServerError, invalidConfiguration.StatusCode);
        Assert.AreEqual("SystemError", invalidConfigurationBody["code"]?.GetValue<string>());
        StringAssert.Contains(invalidConfigurationBody["message"]?.GetValue<string>(), "FormsPlugin_ProcessForms");

        var missingProcess = await _client.PostAsJsonAsync("/workflow-api/forms/get-executable-forms", new
        {
            processId = Guid.NewGuid(),
            identityId = "Paula",
            conditionCheck = true
        });
        var missingProcessBody = await ReadJsonAsync(missingProcess);
        Assert.AreEqual(HttpStatusCode.NotFound, missingProcess.StatusCode);
        Assert.AreEqual("NotFound", missingProcessBody["code"]?.GetValue<string>());
        Assert.IsFalse(String.IsNullOrWhiteSpace(missingProcessBody["message"]?.GetValue<string>()));

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/workflow-api/forms/get-form");
        preflight.Headers.Add("Origin", "http://localhost:5142");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type,workflow-api-tenant-id");
        var cors = await _client.SendAsync(preflight);
        Assert.IsTrue(cors.IsSuccessStatusCode);
        Assert.AreEqual("http://localhost:5142", cors.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [DataTestMethod]
    [DataRow("T:Header")]
    [DataRow("T:EmployeeFields")]
    [DataRow("T:ManagerFields")]
    [DataRow("View")]
    public async Task GetFormAllowsOnlyConfiguredTemplatesAndPreservesVersionsTest(string formName)
    {
        var latest = await PostJsonAsync("/workflow-api/forms/get-form", new { formKey = new { formName } });
        var version = latest["form"]!["formKey"]!["formVersion"]!.GetValue<int>();
        var explicitVersion = await PostJsonAsync("/workflow-api/forms/get-form", new
        {
            formKey = new { formName, formVersion = version }
        });
        Assert.IsTrue(JsonNode.DeepEquals(latest, explicitVersion));

        var missing = await _client.PostAsJsonAsync("/workflow-api/forms/get-form", new
        {
            formKey = new { formName, formVersion = Int32.MaxValue }
        });
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.AreEqual("NotFound", (await ReadJsonAsync(missing))["code"]?.GetValue<string>());
    }

    [DataTestMethod]
    [DataRow("Created")]
    [DataRow("Review")]
    [DataRow("Rework")]
    [DataRow("Delete")]
    [DataRow("DocumentAccessDenied")]
    [DataRow("T:Unlisted")]
    [DataRow("t:header")]
    public async Task GetFormRejectsFormsOutsideTheTemplateAllowlistTest(string formName)
    {
        var response = await _client.PostAsJsonAsync("/workflow-api/forms/get-form", new
        {
            formKey = new { formName }
        });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(String.Empty, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task GetProcessFormIsNotPublishedInHttpOrSwaggerTest()
    {
        var response = await _client.PostAsJsonAsync("/workflow-api/forms/get-process-form", new
        {
            processId = Guid.NewGuid(),
            formKey = new { formName = "View" },
            dataParameterName = "WeeklyReport"
        });
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var swagger = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.IsFalse(swagger.Contains("get-process-form", StringComparison.Ordinal));
        Assert.IsFalse(swagger.Contains("GetProcessFormRequest", StringComparison.Ordinal));
        Assert.IsFalse(swagger.Contains("GetProcessFormResponse", StringComparison.Ordinal));
        var paths = JsonNode.Parse(swagger)!["paths"]!.AsObject();
        string[] expectedFormsPaths =
        [
            "/workflow-api/forms/get-form",
            "/workflow-api/forms/get-executable-forms",
            "/workflow-api/forms/save-form",
            "/workflow-api/forms/execute-form"
        ];
        CollectionAssert.AreEqual(
            expectedFormsPaths,
            paths.Select(item => item.Key).Where(path => path.StartsWith("/workflow-api/forms/", StringComparison.Ordinal)).ToArray()
        );
    }

    [DataTestMethod]
    [DataRow("Paula", "View")]
    [DataRow("Peter", "View")]
    [DataRow("Margaret", "DocumentAccessDenied")]
    [DataRow("John", "DocumentAccessDenied")]
    [DataRow("Emily", "DocumentAccessDenied")]
    [DataRow("Steven", "DocumentAccessDenied")]
    [DataRow("Unknown", null)]
    [DataRow(null, null)]
    public async Task FallbackRespectsReportVisibilityTest(string? identityId, string? expectedFormName)
    {
        var processId = await CreateCompletedReportAsync();
        var response = await PostJsonAsync("/workflow-api/forms/get-executable-forms", new
        {
            processId,
            identityId,
            conditionCheck = true
        });
        var forms = response["forms"]!.AsArray();
        Assert.AreEqual(expectedFormName is null ? 0 : 1, forms.Count);

        if (expectedFormName == "View")
        {
            Assert.AreEqual("View", forms[0]!["formKey"]!["formName"]!.GetValue<string>());
            Assert.AreEqual("Paula", forms[0]!["formData"]!["submittedBy"]!.GetValue<string>());
            Assert.IsFalse(forms[0]!["allowSave"]!.GetValue<bool>());
            Assert.AreEqual(0, forms[0]!["commands"]!.AsArray().Count);
        }
        else if (expectedFormName == "DocumentAccessDenied")
        {
            AssertDocumentAccessDeniedForm(forms.Single()!, processId);
        }
    }

    [DataTestMethod]
    [DataRow("Margaret")]
    [DataRow("John")]
    [DataRow("Emily")]
    [DataRow("Steven")]
    public async Task AccessDeniedFallbackPreservesOwnerAndManagerReviewFormsTest(string identityId)
    {
        var processId = Guid.NewGuid();
        await PostJsonAsync("/workflow-api/rpc/create-instance", new
        {
            schemeCode = "WeeklyReportProcess", processId, identityId = "Paula"
        });
        var created = (await GetExecutableFormsAsync(processId, "Paula"))["forms"]!.AsArray()
            .Single(form => form!["formKey"]!["formName"]!.GetValue<string>() == "Created")!;
        var data = created["formData"]!.DeepClone();
        data["whatWasDone"] = "Verify the document access message";
        data["encounteredProblems"] = "None";
        var submitted = await PostJsonAsync("/workflow-api/forms/execute-form", new
        {
            processId,
            identityId = "Paula",
            formKey = created["formKey"],
            formData = data,
            commandName = "Submit"
        });
        Assert.IsTrue(submitted["wasExecuted"]!.GetValue<bool>());

        var ownerBefore = await GetExecutableFormsAsync(processId, "Paula");
        var denied = await GetExecutableFormsAsync(processId, identityId);
        AssertDocumentAccessDeniedForm(denied["forms"]!.AsArray().Single()!, processId);

        var ownerAfter = await GetExecutableFormsAsync(processId, "Paula");
        Assert.IsTrue(JsonNode.DeepEquals(ownerBefore, ownerAfter));
        var ownerForms = ownerAfter["forms"]!.AsArray();
        Assert.IsTrue(ownerForms.Any(form => form!["formKey"]!["formName"]!.GetValue<string>() == "Delete"));
        Assert.IsFalse(ownerForms.Any(form => form!["formKey"]!["formName"]!.GetValue<string>() == "DocumentAccessDenied"));

        var managerForms = (await GetExecutableFormsAsync(processId, "Peter"))["forms"]!.AsArray();
        Assert.IsTrue(managerForms.Any(form => form!["formKey"]!["formName"]!.GetValue<string>() == "Review"));
        Assert.IsFalse(managerForms.Any(form => form!["formKey"]!["formName"]!.GetValue<string>() == "DocumentAccessDenied"));
    }

    [TestMethod]
    public async Task FallbackConfigurationAndParameterSelectionBelongToTheServerTest()
    {
        var processId = await CreateCompletedReportAsync();
        var options = _app.Services.GetRequiredService<Configuration>().Forms.ViewFallback;
        var originalKey = options.FormKey;

        try
        {
            options.Enabled = false;
            var disabled = await GetExecutableFormsAsync(processId, "Paula");
            Assert.AreEqual(0, disabled["forms"]!.AsArray().Count);
            var deniedDisabled = await GetExecutableFormsAsync(processId, "Margaret");
            Assert.AreEqual(0, deniedDisabled["forms"]!.AsArray().Count);

            options.Enabled = true;
            options.FormKey = new FormKey("View", 0);
            var response = await PostJsonAsync("/workflow-api/forms/get-executable-forms", new
            {
                processId,
                identityId = "Paula",
                conditionCheck = true,
                dataParameterName = "FormsPlugin_ProcessForms",
                formKey = new { formName = "Created" },
                fallback = new { enabled = false }
            });
            var view = response["forms"]!.AsArray().Single()!;
            Assert.AreEqual("View", view["formKey"]!["formName"]!.GetValue<string>());
            Assert.AreEqual(0, view["formKey"]!["formVersion"]!.GetValue<int>());
            Assert.AreEqual("Paula", view["formData"]!["submittedBy"]!.GetValue<string>());

            options.FormKey = new FormKey("View", Int32.MaxValue);
            var missingVersion = await _client.PostAsJsonAsync("/workflow-api/forms/get-executable-forms", new
            {
                processId, identityId = "Paula", conditionCheck = true
            });
            Assert.AreEqual(HttpStatusCode.NotFound, missingVersion.StatusCode);
            Assert.AreEqual("NotFound", (await ReadJsonAsync(missingVersion))["code"]?.GetValue<string>());

            options.FormKey = new FormKey("MissingFallback");
            var missingForm = await _client.PostAsJsonAsync("/workflow-api/forms/get-executable-forms", new
            {
                processId, identityId = "Paula", conditionCheck = true
            });
            Assert.AreEqual(HttpStatusCode.NotFound, missingForm.StatusCode);
        }
        finally
        {
            options.Enabled = true;
            options.FormKey = originalKey;
        }
    }

    [DataTestMethod]
    [DataRow("Paula")]
    [DataRow("Margaret")]
    public async Task ReadOnlyFallbackCannotBeSavedOrExecutedTest(string identityId)
    {
        var processId = await CreateCompletedReportAsync();
        var ownerBefore = await GetExecutableFormsAsync(processId, "Paula");
        var before = await GetExecutableFormsAsync(processId, identityId);
        var view = before["forms"]!.AsArray().Single()!;
        var data = ownerBefore["forms"]!.AsArray().Single()!["formData"]!.DeepClone();
        data["whatWasDone"] = "Unauthorized modification";
        data["encounteredProblems"] = "None";
        data["managerReview"] = "Unauthorized review";
        var save = await _client.PostAsJsonAsync("/workflow-api/forms/save-form", new
        {
            processId,
            identityId,
            formKey = view["formKey"],
            formData = data,
            conditionCheck = true
        });
        Assert.IsFalse(save.IsSuccessStatusCode);

        var execute = await _client.PostAsJsonAsync("/workflow-api/forms/execute-form", new
        {
            processId,
            identityId,
            formKey = view["formKey"],
            formData = data,
            commandName = "Submit"
        });
        Assert.IsFalse(execute.IsSuccessStatusCode);
        var after = await GetExecutableFormsAsync(processId, identityId);
        Assert.IsTrue(JsonNode.DeepEquals(before, after));
        var ownerAfter = await GetExecutableFormsAsync(processId, "Paula");
        Assert.IsTrue(JsonNode.DeepEquals(ownerBefore, ownerAfter));
    }

    [TestMethod]
    public async Task NonemptyFormsDoNotLoadFallbackAndForeignProcessesRemainNotFoundTest()
    {
        var processId = Guid.NewGuid();
        await PostJsonAsync("/workflow-api/rpc/create-instance", new
        {
            schemeCode = "WeeklyReportProcess", processId, identityId = "Paula"
        });
        var options = _app.Services.GetRequiredService<Configuration>().Forms.ViewFallback;
        var originalKey = options.FormKey;

        try
        {
            options.FormKey = new FormKey("MissingFallback");
            var forms = (await GetExecutableFormsAsync(processId, "Paula"))["forms"]!.AsArray();
            Assert.IsTrue(forms.Any(form => form!["formKey"]!["formName"]!.GetValue<string>() == "Created"));
            Assert.IsTrue(forms.Any(form => form!["processId"]!.GetValue<Guid>() != processId));
        }
        finally
        {
            options.FormKey = originalKey;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("UPDATE dbo.WorkflowProcessInstance SET TenantId = @TenantId WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@TenantId", "ForeignTenant");
        command.Parameters.AddWithValue("@Id", processId);
        await command.ExecuteNonQueryAsync();
        var response = await _client.PostAsJsonAsync("/workflow-api/forms/get-executable-forms", new
        {
            processId, identityId = "Paula", conditionCheck = true
        });
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [DataTestMethod]
    [DataRow("short-circuit", HttpStatusCode.OK)]
    [DataRow("recovery", HttpStatusCode.OK)]
    [DataRow("denied", HttpStatusCode.Forbidden)]
    [DataRow("short-circuit", HttpStatusCode.NotFound)]
    [DataRow("recovery", HttpStatusCode.InternalServerError)]
    public async Task FallbackTransformsEmptySuccessfulCallbackResultsAndPreservesErrorsTest(string mode, HttpStatusCode expectedStatus)
    {
        var processId = await CreateCompletedReportAsync();
        var (app, client) = await StartApplicationAsync(services => services.ConfigureWorkflowApi(workflow =>
        {
            var operation = workflow.ConfigureOperation(api => api.Forms.GetExecutableForms);

            if (mode == "denied")
            {
                operation.OnAuthorize(_ => ValueTask.FromResult(false));
            }
            else
            {
                IResult callbackResult = expectedStatus == HttpStatusCode.OK
                    ? TypedResults.Ok(new GetExecutableFormsResponse([]))
                    : TypedResults.Json(new { message = "Test callback error" }, statusCode: (int) expectedStatus);
                operation.OnBefore(_ => mode == "recovery"
                    ? throw new InvalidOperationException("Test recovery")
                    : ValueTask.FromResult<IResult?>(callbackResult));
                operation.OnException((_, _) => ValueTask.FromResult<IResult?>(callbackResult));
            }
        }));

        try
        {
            var response = await client.PostAsJsonAsync("/workflow-api/forms/get-executable-forms", new
            {
                processId, identityId = "Paula", conditionCheck = true
            });
            Assert.AreEqual(expectedStatus, response.StatusCode);

            if (expectedStatus == HttpStatusCode.OK)
            {
                var view = (await ReadJsonAsync(response))["forms"]!.AsArray().Single()!;
                Assert.AreEqual("View", view["formKey"]!["formName"]!.GetValue<string>());
                Assert.AreEqual("Paula", view["formData"]!["submittedBy"]!.GetValue<string>());
                Assert.IsFalse(view["allowSave"]!.GetValue<bool>());
                Assert.AreEqual(0, view["commands"]!.AsArray().Count);
            }
            else if (mode != "denied")
            {
                var error = await ReadJsonAsync(response);
                Assert.AreEqual("Test callback error", error["message"]!.GetValue<string>());
                Assert.IsNull(error["forms"]);
            }
        }
        finally
        {
            client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task ManagerCanViewAnAccessibleReportWithoutExecutableCommandsTest()
    {
        var processId = Guid.NewGuid();
        await PostJsonAsync("/workflow-api/rpc/create-instance", new
        {
            schemeCode = "WeeklyReportProcess", processId, identityId = "Paula"
        });
        var forms = (await GetExecutableFormsAsync(processId, "Peter"))["forms"]!.AsArray();
        Assert.AreEqual("View", forms.Single()!["formKey"]!["formName"]!.GetValue<string>());
        Assert.AreEqual("Created", forms.Single()!["formData"]!["stateName"]!.GetValue<string>());
    }

    private static void AssertDocumentAccessDeniedForm(JsonNode form, Guid processId)
    {
        Assert.AreEqual(processId, form["processId"]!.GetValue<Guid>());
        Assert.AreEqual("DocumentAccessDenied", form["formKey"]!["formName"]!.GetValue<string>());
        Assert.IsNotNull(form["formKey"]!["formVersion"]);
        Assert.AreEqual(0, form["formData"]!.AsObject().Count);
        Assert.IsFalse(form["allowSave"]!.GetValue<bool>());
        Assert.AreEqual(0, form["commands"]!.AsArray().Count);

        var formCode = form["formCode"]!.GetValue<string>();
        Assert.IsNotNull(JsonNode.Parse(formCode));
        StringAssert.Contains(formCode, "This document is not available to the selected user.");
        Assert.IsFalse(formCode.Contains("Template:", StringComparison.Ordinal));
        Assert.IsFalse(formCode.Contains("form.data", StringComparison.Ordinal));
        Assert.IsFalse(formCode.Contains("submittedBy", StringComparison.Ordinal));
        Assert.IsFalse(formCode.Contains("whatWasDone", StringComparison.Ordinal));
        Assert.IsFalse(formCode.Contains("managerReview", StringComparison.Ordinal));
    }

    private static async Task<Guid> CreateCompletedReportAsync()
    {
        var processId = Guid.NewGuid();
        await PostJsonAsync("/workflow-api/rpc/create-instance", new
        {
            schemeCode = "WeeklyReportProcess", processId, identityId = "Paula"
        });
        await PostJsonAsync("/workflow-api/rpc/set-state-with-execution", new
        {
            processId, stateName = "Accepted", identityId = "Peter", impersonatedIdentityId = "Peter", processParameters = Array.Empty<object>()
        });
        return processId;
    }

    private static Task<JsonObject> GetExecutableFormsAsync(Guid processId, string identityId)
    {
        return PostJsonAsync("/workflow-api/forms/get-executable-forms", new
        {
            processId,
            identityId,
            conditionCheck = true
        });
    }

    private static async Task<JsonObject> PostJsonAsync(string path, object value)
    {
        var response = await _client.PostAsJsonAsync(path, value);
        var body = await ReadJsonAsync(response);
        Assert.IsTrue(response.IsSuccessStatusCode, $"{path} returned {(int) response.StatusCode}: {body}");
        return body.AsObject();
    }

    private static async Task<HttpResponseMessage> SendDesignerRequestAsync(Dictionary<string, string> values)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/workflow-api/designer")
        {
            Content = new FormUrlEncodedContent(values)
        };
        return await _client.SendAsync(request);
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return String.IsNullOrWhiteSpace(text) ? new JsonObject() : JsonNode.Parse(text) ?? new JsonObject();
    }

    private static async Task<T> ExecuteScalarAsync<T>(SqlConnection connection, string commandText)
    {
        await using var command = new SqlCommand(commandText, connection);
        return (T) Convert.ChangeType(await command.ExecuteScalarAsync() ?? default(T)!, typeof(T));
    }

    private static async Task<(WebApplication App, HttpClient Client)> StartApplicationAsync(Action<IServiceCollection>? configure = null)
    {
        var builder = new AppBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AppBuilder).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Development
        });
        builder.Builder.WebHost.UseTestServer();
        builder.Builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WorkflowTenantCreationOptions:ConnectionString"] = _connectionString,
            ["Cors:Origins:0"] = "http://localhost:5142"
        });
        builder.ConfigureServices();
        configure?.Invoke(builder.Builder.Services);
        var app = builder.Build();
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    private static WebApplication _app = null!;
    private static HttpClient _client = null!;
    private static string _connectionString = String.Empty;
}
