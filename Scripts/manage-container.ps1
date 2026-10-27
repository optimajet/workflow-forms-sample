param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("create", "remove")]
    [string]$Action,

    [string]$Name = "wfe-api-forms-mssql",
    [int]$Port = 41434,
    [string[]]$Databases = @("workflow_forms", "workflow_forms_tests"),
    [string]$User = "SA",
    [string]$Password = "P@ssw0rd"
)

$ErrorActionPreference = "Stop"

function Assert-ExitCode {
    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed with exit code $LASTEXITCODE."
    }
}

function Test-ContainerExists {
    docker container inspect $Name *> $null
    return $LASTEXITCODE -eq 0
}

if ($Action -eq "remove") {
    if (Test-ContainerExists) {
        docker rm --force $Name | Out-Null
        Assert-ExitCode
        Write-Host "Removed $Name." -ForegroundColor Green
    }
    else {
        Write-Host "Container $Name does not exist; nothing to remove." -ForegroundColor Yellow
    }

    return
}

if (Test-ContainerExists) {
    throw "Container $Name already exists. Run remove-containers.ps1 before creating a clean database."
}

docker run --name $Name -p "${Port}:1433" -e "SA_PASSWORD=$Password" -e "ACCEPT_EULA=Y" -d mcr.microsoft.com/mssql/server:2022-latest | Out-Null
Assert-ExitCode

try {
    $ready = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        docker exec $Name /opt/mssql-tools18/bin/sqlcmd -S localhost -U $User -C -P $Password -Q "SELECT 1" *> $null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }

        Write-Host "Waiting for MSSQL ($attempt/30)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }

    if (-not $ready) {
        throw "MSSQL container did not become ready."
    }

    foreach ($database in $Databases) {
        if ($database -notmatch '^[A-Za-z0-9_]+$') {
            throw "Unsafe database name: $database"
        }

        $query = "IF DB_ID(N'$database') IS NULL CREATE DATABASE [$database];"
        docker exec $Name /opt/mssql-tools18/bin/sqlcmd -S localhost -U $User -C -P $Password -Q $query
        Assert-ExitCode
    }

    Write-Host "MSSQL is ready on localhost:$Port." -ForegroundColor Green
}
catch {
    docker rm --force $Name *> $null
    throw
}
