param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot "IntuitiveMedia.exe"),
    [switch]$Unregister
)

$ErrorActionPreference = "Stop"

$applicationName = "IntuitiveMedia"
$applicationExe = [System.IO.Path]::GetFileName($ExecutablePath)
$progIdPrefix = "IntuitiveMedia"
$extensions = @(
    ".3g2", ".3gp", ".asf", ".avi", ".m2t", ".m2ts", ".m4v", ".mkv",
    ".mov", ".mp2", ".mp4", ".mpe", ".mpeg", ".mpg", ".mpv2", ".mts",
    ".mxf", ".ogv", ".qt", ".ts", ".vob", ".webm", ".wmv"
)

function Set-UserRegistryString {
    param(
        [string]$SubKey,
        [string]$Name,
        [string]$Value
    )

    $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($SubKey)
    if ($null -eq $key) {
        throw "Não foi possível criar a chave do Registro '$SubKey'."
    }

    try {
        $key.SetValue($Name, $Value, [Microsoft.Win32.RegistryValueKind]::String)
    }
    finally {
        $key.Dispose()
    }
}

function Remove-UserRegistryValue {
    param(
        [string]$SubKey,
        [string]$Name
    )

    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($SubKey, $true)
    if ($null -eq $key) {
        return
    }

    try {
        $key.DeleteValue($Name, $false)
    }
    finally {
        $key.Dispose()
    }
}

function Remove-UserRegistryTree {
    param([string]$SubKey)

    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($SubKey, $false)
}

$applicationKey = "Software\Classes\Applications\$applicationExe"

if ($Unregister) {
    Remove-UserRegistryTree $applicationKey

    foreach ($extension in $extensions) {
        $progId = "$progIdPrefix$extension"
        Remove-UserRegistryValue "Software\Classes\$extension\OpenWithProgids" $progId
        Remove-UserRegistryTree "Software\Classes\$progId"
    }

    Write-Host "IntuitiveMedia foi removido do menu Open With deste usuário."
    exit 0
}

if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
    throw "Executável não encontrado: '$ExecutablePath'. Extraia o aplicativo e execute este script ao lado de IntuitiveMedia.exe."
}

$resolvedExecutablePath = (Resolve-Path -LiteralPath $ExecutablePath).Path
$quotedExecutablePath = '"' + $resolvedExecutablePath + '"'
$openCommand = "$quotedExecutablePath %*"

Set-UserRegistryString "$applicationKey\shell\open\command" "" $openCommand
Set-UserRegistryString "$applicationKey\shell\open" "MultiSelectModel" "Player"
Set-UserRegistryString "$applicationKey" "FriendlyAppName" $applicationName

foreach ($extension in $extensions) {
    $progId = "$progIdPrefix$extension"
    $progIdKey = "Software\Classes\$progId"

    Set-UserRegistryString "$applicationKey\SupportedTypes" $extension ""
    Set-UserRegistryString $progIdKey "" "$applicationName $extension"
    Set-UserRegistryString "$progIdKey\DefaultIcon" "" "$quotedExecutablePath,0"
    Set-UserRegistryString "$progIdKey\shell\open\command" "" $openCommand
    Set-UserRegistryString "$progIdKey\shell\open" "MultiSelectModel" "Player"
    Set-UserRegistryString "Software\Classes\$extension\OpenWithProgids" $progId ""
}

Write-Host "IntuitiveMedia foi registrado no menu Open With deste usuário."
Write-Host "Executável: $resolvedExecutablePath"
Write-Host "O registro não altera o aplicativo padrão de nenhum formato."
