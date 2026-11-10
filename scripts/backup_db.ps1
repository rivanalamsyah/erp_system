<#
.SYNOPSIS
    Automated Backup Script for Enterprise ERP PostgreSQL Database
.DESCRIPTION
    Creates a compressed, timestamped binary dump of the Enterprise ERP database.
    Includes automated retention management to prune backups older than specified days.
.PARAMETER Hostname
    PostgreSQL server host (Default: 127.0.0.1)
.PARAMETER Port
    PostgreSQL server port (Default: 5433)
.PARAMETER DatabaseName
    Target database name (Default: enterprise_erp)
.PARAMETER Username
    PostgreSQL user (Default: erp_admin)
.PARAMETER OutputDirectory
    Directory to save backup files (Default: ./backups)
.PARAMETER RetentionDays
    Number of days to keep backups (Default: 30)
#>
param(
    [string]$Hostname = "127.0.0.1",
    [int]$Port = 5433,
    [string]$DatabaseName = "enterprise_erp",
    [string]$Username = "erp_admin",
    [string]$OutputDirectory = "d:\erp_system\backups",
    [int]$RetentionDays = 30
)

$ErrorActionPreference = "Stop"

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = Join-Path -Path $OutputDirectory -ChildPath "${DatabaseName}_backup_${Timestamp}.dump"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Enterprise ERP Database Backup Utility" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Server     : ${Hostname}:${Port}"
Write-Host "Database   : ${DatabaseName}"
Write-Host "User       : ${Username}"
Write-Host "Destination: ${BackupFile}"
Write-Host "----------------------------------------------------------"

if (-not (Test-Path -Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    Write-Host "[INFO] Created backup directory: ${OutputDirectory}" -ForegroundColor Gray
}

# Resolve pg_dump executable
$PgDump = Get-Command "pg_dump" -ErrorAction SilentlyContinue
if (-not $PgDump) {
    $Pg18Path = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
    $Pg17Path = "C:\Program Files\PostgreSQL\17\bin\pg_dump.exe"
    if (Test-Path $Pg18Path) { $PgDump = $Pg18Path }
    elseif (Test-Path $Pg17Path) { $PgDump = $Pg17Path }
    else {
        throw "[ERROR] pg_dump executable not found in PATH or standard installation directories."
    }
} else {
    $PgDump = $PgDump.Source
}

Write-Host "[INFO] Executing pg_dump via: $PgDump" -ForegroundColor Yellow

$Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Execute pg_dump (Custom format -Fc includes built-in compression and granular table restoration support)
& "$PgDump" -h $Hostname -p $Port -U $Username -F c -b -v -f "$BackupFile" $DatabaseName

$Stopwatch.Stop()

if (Test-Path -Path $BackupFile) {
    $SizeMB = [Math]::Round((Get-Item -Path $BackupFile).Length / 1MB, 2)
    Write-Host "[SUCCESS] Backup completed in $($Stopwatch.Elapsed.TotalSeconds.ToString('F2')) seconds." -ForegroundColor Green
    Write-Host "[SUCCESS] File Size: ${SizeMB} MB" -ForegroundColor Green
} else {
    throw "[ERROR] Backup failed. Output file was not generated."
}

# Retention Pruning
Write-Host "[INFO] Checking retention policy (Retention: ${RetentionDays} days)..." -ForegroundColor Gray
$CutoffDate = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem -Path $OutputDirectory -Filter "${DatabaseName}_backup_*.dump" | Where-Object {
    $_.CreationTime -lt $CutoffDate
} | ForEach-Object {
    Write-Host "[PRUNE] Removing old backup: $($_.Name)" -ForegroundColor DarkYellow
    Remove-Item $_.FullName -Force
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Backup Finished Successfully" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

