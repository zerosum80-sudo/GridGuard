function Get-LiveValidationFileAggregate {
    param([Parameter(Mandatory = $true)][string[]]$Roots)
    $incremental = [Security.Cryptography.IncrementalHash]::CreateHash(
        [Security.Cryptography.HashAlgorithmName]::SHA256)
    $count = 0
    $errors = 0
    try {
        $files = foreach ($root in $Roots) {
            if (Test-Path -LiteralPath $root) {
                Get-ChildItem -LiteralPath $root -File -Recurse -Force `
                    -ErrorAction SilentlyContinue
            }
        }
        foreach ($file in $files | Sort-Object FullName) {
            try {
                $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
                $value = "$($file.FullName)|$($file.Length)|$hash`n"
                $incremental.AppendData([Text.Encoding]::UTF8.GetBytes($value))
                $count++
            }
            catch {
                $errors++
            }
        }
        [ordered]@{
            Count = $count
            Errors = $errors
            Sha256 = [BitConverter]::ToString(
                $incremental.GetHashAndReset()).Replace(
                    '-', '').ToLowerInvariant()
        }
    }
    finally {
        $incremental.Dispose()
    }
}

function Get-LiveValidationFilebogoSnapshot {
    $filebogoKey = 'HKLM:\SYSTEM\CurrentControlSet\Services\FilebogoLauncher'
    $entry = Get-ItemProperty -LiteralPath $filebogoKey -ErrorAction SilentlyContinue
    $service = Get-Service -Name FilebogoLauncher -ErrorAction SilentlyContinue
    $executable = if ($entry) {
        [Environment]::ExpandEnvironmentVariables(
            $entry.ImagePath.Trim().Trim('"'))
    } else {
        $null
    }
    $directory = if ($executable) { Split-Path -Parent $executable } else { $null }
    [ordered]@{
        FilebogoServicePresent = $null -ne $entry
        FilebogoServiceState = if ($service) {
            $service.Status.ToString()
        } else {
            'ABSENT'
        }
        FilebogoServiceStart = if ($entry) { $entry.Start } else { $null }
        FilebogoServiceImagePath = if ($entry) { $entry.ImagePath } else { $null }
        FilebogoProcessCount = @(
            Get-Process -Name FilebogoLauncher -ErrorAction SilentlyContinue
        ).Count
        FilebogoExecutableSha256 = if (
            $executable -and (Test-Path -LiteralPath $executable -PathType Leaf)) {
            (Get-FileHash -LiteralPath $executable -Algorithm SHA256).
                Hash.ToLowerInvariant()
        } else {
            $null
        }
        FilebogoFiles = if ($directory) {
            Get-LiveValidationFileAggregate -Roots @($directory)
        } else {
            $null
        }
    }
}

function Get-LiveValidationProtectedSnapshot {
    $userRoots = @(
        [Environment]::GetFolderPath('Desktop'),
        [Environment]::GetFolderPath('MyDocuments'),
        (Join-Path $env:USERPROFILE 'Downloads')
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
    $snapshot = Get-LiveValidationFilebogoSnapshot
    [ordered]@{
        FilebogoServicePresent = $snapshot.FilebogoServicePresent
        FilebogoServiceState = $snapshot.FilebogoServiceState
        FilebogoServiceStart = $snapshot.FilebogoServiceStart
        FilebogoServiceImagePath = $snapshot.FilebogoServiceImagePath
        FilebogoProcessCount = $snapshot.FilebogoProcessCount
        FilebogoExecutableSha256 = $snapshot.FilebogoExecutableSha256
        FilebogoFiles = $snapshot.FilebogoFiles
        UserFiles = Get-LiveValidationFileAggregate -Roots $userRoots
    }
}

function Test-LiveValidationSnapshotEqual {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )
    ($Before | ConvertTo-Json -Depth 8 -Compress) -ceq
        ($After | ConvertTo-Json -Depth 8 -Compress)
}
