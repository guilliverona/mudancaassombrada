# Gera todos os blockouts: personagens, fantasmas e props (fecha o Blender ao terminar cada um).
# Uso: .\tools\blender\build_all.ps1            (ou passe outro caminho: -Blender "C:\...\blender.exe")
param([string]$Blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe")

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
foreach ($c in "cida", "tiago", "marcelo", "bibliotecario", "props") {
    Write-Host "== $c"
    & $Blender --background --factory-startup --python (Join-Path $here "build_${c}_blockout.py") 2>&1 |
        Select-String -Pattern "TRIS|SAVED|Traceback|Error"
}
