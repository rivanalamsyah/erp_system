<#
.SYNOPSIS
    Automated Restore Script for Enterprise ERP PostgreSQL Database
.DESCRIPTION
    Restores the Enterprise ERP database from a compressed custom-format pg_dump backup file (.dump).
.PARAMETER BackupFilePath
    Path to the .dump file to restore. If omitted, prompts with the latest backup in ./backups.
.PARAMETER Hostname
    PostgreSQL server host (Default: 127.0.0.1)
.PARAMETER Port
    PostgreSQL server port (Default: 5433)
.PARAMETER DatabaseName
    Target database name (Default: enterprise_erp)
.PARAMETER Username
    PostgreSQL user (Default: erp_admin)
.PARAMETER CleanBeforeRestore
    Clean (drop) database objects prior to restoring (Default: true)
#>
param(
    [string]$BackupFilePath = "",
    [string]$Hostname = "127.0.0.1",
    [int]$Port = 5433,
    [string]$DatabaseName = "enterprise_erp",
    [string]$Username = "erp_admin",
    [switch]$CleanBeforeRestore = $true
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Enterprise ERP Database Restore Utility" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# If no backup file provided, find latest backup
if ([string]::IsNullOrWhiteSpace($BackupFilePath)) {
    $BackupsDir = "d:\erp_system\backups"
    if (Test-Path $BackupsDir) {
        $Latest = Get-ChildItem -Path $BackupsDir -Filter "${DatabaseName}_backup_*.dump" | Sort-Object CreationTime -Descending | Select-Object -First 1
        if ($Latest) {
            $BackupFilePath = $Latest.FullName
            Write-Host "[INFO] Auto-selected latest backup: $($Latest.Name)" -ForegroundColor Yellow
        }
    }
}

if (-not (Test-Path -Path $BackupFilePath)) {
    throw "[ERROR] Backup file not found: '$BackupFilePath'. Please specify a valid .dump file."
}

Write-Host "Server     : ${Hostname}:${Port}"
Write-Host "Database   : ${DatabaseName}"
Write-Host "User       : ${Username}"
Write-Host "Source File: ${BackupFilePath}"
Write-Host "----------------------------------------------------------"

# Resolve pg_restore executable
$PgRestore = Get-Command "pg_restore" -ErrorAction SilentlyContinue
if (-not $PgRestore) {
    $Pg18Path = "C:\Program Files\PostgreSQL\18\bin\pg_restore.exe"
    $Pg17Path = "C:\Program Files\PostgreSQL\17\bin\pg_restore.exe"
    if (Test-Path $Pg18Path) { $PgRestore = $Pg18Path }
    elseif (Test-Path $Pg17Path) { $PgRestore = $Pg17Path }
    else {
        throw "[ERROR] pg_restore executable not found in PATH or standard installation directories."
    }
} else {
    $PgRestore = $PgRestore.Source
}

Write-Host "[INFO] Executing pg_restore via: $PgRestore" -ForegroundColor Yellow

$RestoreArgs = @(
    "-h", $Hostname,
    "-p", $Port,
    "-U", $Username,
    "-d", $DatabaseName,
    "-v",
    "--no-owner",
    "--no-privileges"
)

if ($CleanBeforeRestore) {
    $RestoreArgs += "--clean"
    $RestoreArgs += "--if-exists"
}

$RestoreArgs += $BackupFilePath

$Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

& "$PgRestore" $RestoreArgs

$Stopwatch.Stop()

Write-Host "[SUCCESS] Restoration completed in $($Stopwatch.Elapsed.TotalSeconds.ToString('F2')) seconds." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Database Restore Completed" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

