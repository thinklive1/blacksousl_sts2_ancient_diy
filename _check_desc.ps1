$c = Get-Content "D:\game\bsmod\bs_ancient\bs_ancient\localization\zhs\relics.json" -Raw -Encoding UTF8
$lines = $c -split "`n"
$results = @()
foreach ($l in $lines) {
    if ($l -match '"description"' -and $l -notmatch 'details\.|breeding\.') {
        $key = ($l -split '"')[1]
        $val = ($l -split '": "')[1] -replace '",?$', ''
        $pure = $val -replace '\[.+?\]', '' -replace '\{[^}]+\}', ''
        $results += [PSCustomObject]@{Len = $pure.Length; Key = $key; Text = $val }
    }
}
$results | Sort-Object Len -Descending | Select-Object -First 25 | ForEach-Object {
    Write-Host "[$($_.Len)] $($_.Key)"
    Write-Host "  $($_.Text)"
    Write-Host ""
}
