$ErrorActionPreference = 'Stop'

$patterns = @('*.tscn', '*.tres', '*.gdshader')
$files = Get-ChildItem -Path . -Recurse -File -Include $patterns
$bad = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $stream = [System.IO.File]::OpenRead($file.FullName)
    try {
        if ($stream.Length -lt 3) {
            continue
        }

        $b0 = $stream.ReadByte()
        $b1 = $stream.ReadByte()
        $b2 = $stream.ReadByte()
        if ($b0 -eq 0xEF -and $b1 -eq 0xBB -and $b2 -eq 0xBF) {
            $bad.Add($file.FullName)
        }
    }
    finally {
        $stream.Dispose()
    }
}

if ($bad.Count -gt 0) {
    Write-Host 'Found UTF-8 BOM in scene resource files:' -ForegroundColor Red
    foreach ($path in $bad) {
        Write-Host " - $path"
    }
    exit 1
}

Write-Host 'No UTF-8 BOM found in *.tscn/*.tres/*.gdshader.' -ForegroundColor Green
