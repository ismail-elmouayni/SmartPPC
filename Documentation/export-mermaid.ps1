<#
.SYNOPSIS
  Exporte toutes les définitions Mermaid (.mmd) du dossier courant (et optionnellement sous-dossiers) en PNG et/ou SVG.

.PARAMETER Recursive
  Parcours récursivement les sous-dossiers.

.PARAMETER Svg
  Génère aussi un export SVG.

.PARAMETER Png
  Génère aussi un export PNG (activé par défaut si aucun format n'est précisé).

.PARAMETER Config
  Fichier de configuration Mermaid JSON (ex: mermaid-config.json).

.EXAMPLE
  pwsh ./export-mermaid.ps1 -Svg

.EXAMPLE
  pwsh ./export-mermaid.ps1 -Recursive -Config mermaid-config.json

.PRÉREQUIS
  Node.js installé.
  Installer le CLI Mermaid localement :
    npm init -y
    npm install @mermaid-js/mermaid-cli --save-dev
  (Ou globalement) : npm install -g @mermaid-js/mermaid-cli

  Utilisation sans installation globale : npx mmdc ... (géré automatiquement par ce script)
#>
param(
  [switch]$Recursive,
  [switch]$Svg,
  [switch]$Png,
  [string]$Config
)

if(-not ($Svg -or $Png)) { $Png = $true }

if($Recursive){ $searchParams = @{'Recurse'=$true} } else { $searchParams = @{} }

# Vérification Node
if(-not (Get-Command node -ErrorAction SilentlyContinue)) {
  Write-Error "Node.js n'est pas installé ou pas dans le PATH. Installer depuis https://nodejs.org/ puis relancer."
  exit 1
}

# Détermination commande mmdc (global ou via npx)
$useNpx = $false
if(Get-Command mmdc -ErrorAction SilentlyContinue) {
  $mmdcCmd = 'mmdc'
}
else {
  if(Test-Path './node_modules/.bin/mmdc') {
    $mmdcCmd = 'npx mmdc'
    $useNpx = $true
  } else {
    Write-Host "Mermaid CLI non trouvé. Installation locale dev (@mermaid-js/mermaid-cli) en cours..."
    npm install @mermaid-js/mermaid-cli --save-dev | Out-Null
    if(-not (Test-Path './node_modules/.bin/mmdc')) {
      Write-Error "Installation Mermaid CLI échouée."
      exit 2
    }
    $mmdcCmd = 'npx mmdc'
    $useNpx = $true
  }
}

Write-Host "Utilisation de: $mmdcCmd" -ForegroundColor Cyan

$files = Get-ChildItem -Filter '*.mmd' @searchParams | Where-Object { -not $_.PSIsContainer }
if(-not $files) {
  Write-Warning 'Aucun fichier .mmd trouvé.'
  exit 0
}

$confArg = ''
if($Config) {
  if(Test-Path $Config) { $confArg = "-c `"$Config`"" } else { Write-Warning "Fichier config introuvable: $Config" }
}

foreach($f in $files) {
  Write-Host "Traitement: $($f.Name)" -ForegroundColor Yellow
  $base = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
  if($Png) {
    $pngOut = "$base.png"
    $cmd = "$mmdcCmd -i `"$($f.FullName)`" -o `"$pngOut`" --backgroundColor white $confArg"
    Write-Host "  -> PNG : $pngOut"; iex $cmd
  }
  if($Svg) {
    $svgOut = "$base.svg"
    $cmd = "$mmdcCmd -i `"$($f.FullName)`" -o `"$svgOut`" --backgroundColor white $confArg"
    Write-Host "  -> SVG : $svgOut"; iex $cmd
  }
}

Write-Host "Export terminé." -ForegroundColor Green
