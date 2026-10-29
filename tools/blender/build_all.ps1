# Gera todos os blockouts: personagens, fantasmas, o chefe, props, mobília e o kit de cenário
# (fecha o Blender ao terminar cada um).
# Uso: .\tools\blender\build_all.ps1            (ou passe outro caminho: -Blender "C:\...\blender.exe")
param([string]$Blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe")

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$scripts = "cida", "tiago", "marcelo",                    # jogáveis
           "bibliotecario", "imitador", "maestrina",       # fantasmas
           "valdemar",                                     # o chefe
           "props", "story_props", "furniture", "epoca",   # objetos, mobília e os objetos de 1927 (Mansão)
           "env_kit"                                       # paredes, portas, pisos, molduras
# As texturas de detalhe primeiro (os modelos dependem delas no Unity).
Write-Host "== texturas de detalhe"
& $Blender --background --factory-startup --python (Join-Path $here "build_detail_textures.py") 2>&1 |
    Select-String -Pattern "SAVED|Traceback|Error"

foreach ($c in $scripts) {
    Write-Host "== $c"
    & $Blender --background --factory-startup --python (Join-Path $here "build_${c}_blockout.py") 2>&1 |
        Select-String -Pattern "TRIS|SAVED|Traceback|Error"
}

# Personagens com versão detalhada: grava o FBX do jogo por cima do blockout (rodar depois dele).
Write-Host "== marcelo_detalhado"
& $Blender --background --factory-startup --python (Join-Path $here "build_marcelo_detalhado.py") 2>&1 |
    Select-String -Pattern "TRIS|SAVED|Traceback|Error"

# Poses de mão feitas sobre o rig de um personagem (clipes em Assets/Art/Animations).
Write-Host "== pega_lanterna"
& $Blender --background --factory-startup --python (Join-Path $here "build_pega_lanterna.py") 2>&1 |
    Select-String -Pattern "PUNHO|SAVED|Traceback|Error"
