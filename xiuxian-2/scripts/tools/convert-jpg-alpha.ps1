$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$targets = @(
    'assets/origin/ui/icon_book_button.jpg',
    'assets/origin/ui/icon_close_button.jpg',
    'assets/origin/ui/icon_drag_handle.jpg',
    'assets/origin/ui/icon_resize_handle.jpg',
    'assets/origin/ui/icon_lingshi.jpg',
    'assets/origin/items/icon_breakthrough_pill.jpg',
    'assets/origin/items/icon_lingqi_shard.jpg',
    'assets/origin/items/icon_spirit_herb.jpg',
    'assets/origin/monsters/mob_001_cave_insect_idle.jpg',
    'assets/origin/monsters/mob_001_hit_sheet.jpg',
    'assets/origin/monsters/mob_002_cave_bat.jpg',
    'assets/origin/spirit_pet/pet_mood_low.jpg',
    'assets/origin/spirit_pet/pet_mood_high.jpg'
)

function Get-KeyColor($bmp) {
    $w = $bmp.Width
    $h = $bmp.Height
    $c1 = $bmp.GetPixel(0, 0)
    $c2 = $bmp.GetPixel($w - 1, 0)
    $c3 = $bmp.GetPixel(0, $h - 1)
    $c4 = $bmp.GetPixel($w - 1, $h - 1)
    $r = [int](($c1.R + $c2.R + $c3.R + $c4.R) / 4)
    $g = [int](($c1.G + $c2.G + $c3.G + $c4.G) / 4)
    $b = [int](($c1.B + $c2.B + $c3.B + $c4.B) / 4)
    return @($r, $g, $b)
}

foreach ($relativePath in $targets) {
    if (-not (Test-Path -LiteralPath $relativePath)) {
        Write-Host "Skip missing file: $relativePath"
        continue
    }

    $src = Resolve-Path -LiteralPath $relativePath
    $dst = [System.IO.Path]::ChangeExtension($src, '.png')

    $sourceBmp = [System.Drawing.Bitmap]::FromFile($src)
    try {
        $workingBmp = $sourceBmp
        $workingOwned = $false
        $maxDim = [Math]::Max($sourceBmp.Width, $sourceBmp.Height)
        if ($maxDim -gt 512) {
            $scale = 512.0 / [double]$maxDim
            $tw = [Math]::Max(1, [int][Math]::Round($sourceBmp.Width * $scale))
            $th = [Math]::Max(1, [int][Math]::Round($sourceBmp.Height * $scale))
            $resized = New-Object System.Drawing.Bitmap($tw, $th, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
            $gfx = [System.Drawing.Graphics]::FromImage($resized)
            try {
                $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $gfx.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $gfx.DrawImage($sourceBmp, 0, 0, $tw, $th)
            }
            finally {
                $gfx.Dispose()
            }
            $workingBmp = $resized
            $workingOwned = $true
        }

        try {
            $w = $workingBmp.Width
            $h = $workingBmp.Height
            $key = Get-KeyColor $workingBmp
            $keyR = $key[0]
            $keyG = $key[1]
            $keyB = $key[2]

            $out = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                for ($y = 0; $y -lt $h; $y++) {
                    for ($x = 0; $x -lt $w; $x++) {
                        $p = $workingBmp.GetPixel($x, $y)
                        $dr = $p.R - $keyR
                        $dg = $p.G - $keyG
                        $db = $p.B - $keyB
                        $dist = [Math]::Sqrt(($dr * $dr) + ($dg * $dg) + ($db * $db))

                        if ($dist -lt 34) {
                            $a = 0
                        }
                        elseif ($dist -lt 82) {
                            $a = [int](255 * (($dist - 34) / 48))
                        }
                        else {
                            $a = 255
                        }

                        $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($a, $p.R, $p.G, $p.B))
                    }
                }

                $out.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)
                Write-Host "Generated alpha PNG: $dst"
            }
            finally {
                $out.Dispose()
            }
        }
        finally {
            if ($workingOwned) {
                $workingBmp.Dispose()
            }
        }
    }
    finally {
        $sourceBmp.Dispose()
    }
}
