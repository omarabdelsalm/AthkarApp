$files = @(
    "AthkarApp\Resources\Raw\adhan_egypt.mp3",
    "AthkarApp\Resources\Raw\adhan_madina.mp3",
    "AthkarApp\Resources\Raw\om.wav",
    "AthkarApp\Platforms\Android\Resources\raw\adhan_egypt.mp3",
    "AthkarApp\Platforms\Android\Resources\raw\adhan_madina.mp3"
)

foreach ($f in $files) {
    $path = Join-Path $PSScriptRoot $f
    if (Test-Path $path) {
        $bytes = [System.IO.File]::ReadAllBytes($path)
        $size = $bytes.Length
        $hex = ($bytes[0..15] | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
        $ascii = -join ($bytes[0..15] | ForEach-Object { if ($_ -ge 32 -and $_ -le 126) { [char]$_ } else { '.' } })
        Write-Host "$f => Size: $size, Hex: $hex, ASCII: $ascii"
    } else {
        Write-Host "$f => NOT FOUND"
    }
}
