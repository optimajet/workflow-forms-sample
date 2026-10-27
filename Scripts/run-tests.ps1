$ErrorActionPreference = "Stop"
$createContainers = Join-Path $PSScriptRoot "create-containers.ps1"
$removeContainers = Join-Path $PSScriptRoot "remove-containers.ps1"
$testProjectDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../WorkflowApi.IntegrationTests"))
$testProject = Join-Path $testProjectDirectory "WorkflowApi.IntegrationTests.csproj"
$resultsDirectory = [System.IO.Path]::GetFullPath((Join-Path $testProjectDirectory "TestResults/run-tests"))
$expectedPrefix = $testProjectDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

if (-not $resultsDirectory.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The test results directory must be inside the integration test project."
}

function Assert-TestsExecuted {
    $trx = Get-ChildItem $resultsDirectory -Filter *.trx -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -eq $trx) {
        throw "No trx result was generated."
    }

    [xml]$result = Get-Content -Raw $trx.FullName
    if ([int]$result.TestRun.ResultSummary.Counters.executed -eq 0) {
        throw "No integration tests were executed."
    }
}

& $removeContainers

try {
    & $createContainers

    if (Test-Path -LiteralPath $resultsDirectory) {
        Remove-Item -LiteralPath $resultsDirectory -Recurse -Force
    }

    $env:FORMS_TEST_CONNECTION_STRING = "Server=localhost,41434;Database=workflow_forms_tests;User Id=SA;Password=P@ssw0rd;TrustServerCertificate=True;"
    dotnet test $testProject -c Release --results-directory $resultsDirectory --logger "console;verbosity=normal" --logger trx
    if ($LASTEXITCODE -ne 0) {
        throw "Integration tests failed with exit code $LASTEXITCODE."
    }

    Assert-TestsExecuted
}
finally {
    & $removeContainers
}
