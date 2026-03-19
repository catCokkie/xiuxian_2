$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$specs = @(
    @{ Path = 'assets/origin/ui/icon_book_button.png'; Size = 128; Padding = 0.72 },
    @{ Path = 'assets/origin/ui/icon_close_button.png'; Size = 96; Padding = 0.72 },
    @{ Path = 'assets/origin/ui/icon_drag_handle.png'; Size = 96; Padding = 0.72 },
    @{ Path = 'assets/origin/ui/icon_resize_handle.png'; Size = 96; Padding = 0.72 },
    @{ Path = 'assets/origin/ui/icon_lingshi.png'; Size = 96; Padding = 0.72 },
    @{ Path = 'assets/origin/items/icon_breakthrough_pill.png'; Size = 96; Padding = 0.82 },
    @{ Path = 'assets/origin/items/icon_lingqi_shard.png'; Size = 96; Padding = 0.82 },
    @{ Path = 'assets/origin/items/icon_spirit_herb.png'; Size = 96; Padding = 0.82 },
    @{ Path = 'assets/origin/monsters/mob_001_cave_insect_idle.png'; Size = 256; Padding = 0.86 },
    @{ Path = 'assets/origin/monsters/mob_001_hit_sheet.png'; Size = 256; Padding = 0.86 },
    @{ Path = 'assets/origin/monsters/mob_002_cave_bat.png'; Size = 256; Padding = 0.86 }
)

function Get-AlphaBounds($bmp, $threshold) {
    $minX = $bmp.Width
    $minY = $bmp.Height
    $maxX = -1
    $maxY = -1

    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            $a = $bmp.GetPixel($x, $y).A
            if ($a -le $threshold) {
                continue
            }

            if ($x -lt $minX) { $minX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }

    if ($maxX -lt $minX -or $maxY -lt $minY) {
        return $null
    }

    return [System.Drawing.Rectangle]::new(
        [int]$minX,
        [int]$minY,
        [int]($maxX - $minX + 1),
        [int]($maxY - $minY + 1)
    )
}

foreach ($spec in $specs) {
    $path = $spec.Path
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host "Skip missing: $path"
        continue
    }

    $size = [int]$spec.Size
    $padding = [double]$spec.Padding
    $fullPath = (Resolve-Path -LiteralPath $path).Path
    $sourceBytes = [System.IO.File]::ReadAllBytes($fullPath)
    $sourceStream = New-Object System.IO.MemoryStream(,$sourceBytes)
    $bmp = [System.Drawing.Bitmap]::FromStream($sourceStream)
    try {
        $bounds = Get-AlphaBounds $bmp 14
        if ($null -eq $bounds) {
            Write-Host "Skip empty alpha: $path"
            continue
        }

        $crop = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $cropGfx = [System.Drawing.Graphics]::FromImage($crop)
        try {
            $cropGfx.DrawImage($bmp, 0, 0, $bounds, [System.Drawing.GraphicsUnit]::Pixel)
        }
        finally {
            $cropGfx.Dispose()
        }

        $canvas = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $gfx = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $gfx.Clear([System.Drawing.Color]::Transparent)
            $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $gfx.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

            $target = [Math]::Floor($size * $padding)
            $scale = [Math]::Min($target / [double]$crop.Width, $target / [double]$crop.Height)
            $dw = [Math]::Max(1, [int][Math]::Round($crop.Width * $scale))
            $dh = [Math]::Max(1, [int][Math]::Round($crop.Height * $scale))
            $dx = [int][Math]::Floor(($size - $dw) / 2)
            $dy = [int][Math]::Floor(($size - $dh) / 2)

            $gfx.DrawImage($crop, $dx, $dy, $dw, $dh)
            $tmpPath = "$fullPath.tmp.png"
            $canvas.Save($tmpPath, [System.Drawing.Imaging.ImageFormat]::Png)
            [System.IO.File]::Delete($fullPath)
            [System.IO.File]::Move($tmpPath, $fullPath)
            Write-Host "Normalized: $path -> ${size}x${size}"
        }
        finally {
            $gfx.Dispose()
            $canvas.Dispose()
            $crop.Dispose()
        }
    }
    finally {
        $bmp.Dispose()
        $sourceStream.Dispose()
    }
}
