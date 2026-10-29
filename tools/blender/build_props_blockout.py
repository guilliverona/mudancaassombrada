"""Blockout dos props do protótipo (prancha PRP do Art Bible): armário, cadeira, caixa e caminhão.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_props_blockout.py

Saídas, uma pasta por prop: Art/Source/Props/<Nome>/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Props/<Nome>/SM_PRP_<Nome>.fbx

As medidas seguem os prefabs do jogo (Assets/Editor/LevelPrefabsBuilder.cs): o modelo ocupa
a mesma caixa que o cubo de hoje, então peso, colisão e o "bake" do NavMesh não mudam.
Origem na base (no chão). Frente para -Y, como os personagens.
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import blockout_common as bc  # noqa: E402

# Quinas com bisel de 6 mm em todo móvel (pegam um fio de luz; sem isso parecem de massinha).
bc.DEFAULT_BEVEL = 0.006

MATS = bc.Materials("MAT_PRP", {
    "Mogno": ("#5A2E22", 0.0, 0.7, 0.0),
    "MognoClaro": ("#6E3A2A", 0.0, 0.7, 0.0),
    "Madeira": ("#8A5A3A", 0.0, 0.75, 0.0),
    "Veludo": ("#7A1E2E", 0.0, 0.95, 0.0),
    "Dourado": ("#C9A24A", 1.0, 0.35, 0.0),
    "Sombra": ("#1C1A22", 0.0, 0.8, 0.0),
    "Papelao": ("#B88B5A", 0.0, 0.95, 0.0),
    "Fita": ("#E9DFC8", 0.0, 0.5, 0.0),
    "Carimbo": ("#C8463C", 0.0, 0.8, 0.0),
    "Petroleo": ("#3F5A5E", 0.2, 0.55, 0.0),
    "Faixa": ("#F08A3C", 0.0, 0.5, 0.4),
    "Vidro": ("#22303A", 0.6, 0.15, 0.0),
    "Pneu": ("#131118", 0.0, 0.9, 0.0),
    "Aco": ("#8E8794", 0.9, 0.4, 0.0),
    "Farol": ("#FFC98A", 0.0, 0.3, 2.0),
    "Ferrugem": ("#9C4A2E", 0.3, 0.8, 0.0),
    "MognoVelho": ("#432820", 0.0, 0.85, 0.0),
    "Lasca": ("#9A6A48", 0.0, 0.95, 0.0),
    "Ferro": ("#3A3634", 0.7, 0.6, 0.0),
    "LataoVelho": ("#8C7440", 0.8, 0.6, 0.0),
})


def build_armario(col):
    """Armário de figurino: 1,0 × 0,6 × 1,5 m, peso 3 no jogo (dois jogadores).

    Antigo e maltratado: cornija lascada, pé quebrado calçado com um tijolo, porta direita
    entreaberta (com uma manga de veludo escapando), puxador faltando e rachaduras.
    O corpo continua na caixa do prefab; só a porta aberta avança uns 15 cm para a frente.
    """
    p = bc.Part("SM_PRP_Armario", MATS)
    p.box((0, 0, 0.78), (0.96, 0.56, 1.38), "MognoVelho", "root")    # corpo

    # Cornija em degraus, com o canto da frente-esquerda arrancado (madeira crua à mostra).
    p.box((0.05, 0, 1.475), (0.9, 0.6, 0.05), "Mogno", "root")
    p.box((-0.45, 0.06, 1.475), (0.1, 0.48, 0.05), "Mogno", "root")
    p.box((-0.44, -0.25, 1.462), (0.08, 0.06, 0.024), "Lasca", "root")
    p.box((0.02, 0, 1.44), (0.94, 0.58, 0.03), "MognoClaro", "root")
    p.box((0.03, -0.27, 1.495), (0.5, 0.04, 0.03), "MognoClaro", "root", rot_y=-1.5)  # frontão torto

    # Pés torneados; o da frente-direita quebrou e está calçado com um tijolo.
    for x, y in ((-0.44, -0.24), (-0.44, 0.24), (0.44, 0.24)):
        p.tube((x, y, 0.0), (x, y, 0.09), 0.03, 0.04, "Mogno", "root", seg=10)
    p.tube((0.44, -0.24, 0.06), (0.44, -0.24, 0.09), 0.033, 0.04, "Mogno", "root", seg=10)
    p.box((0.44, -0.24, 0.055), (0.05, 0.05, 0.012), "Lasca", "root", rot_y=8)  # ponta quebrada
    p.box((0.44, -0.235, 0.024), (0.12, 0.07, 0.048), "Ferrugem", "root", rot_y=2)  # tijolo

    # Porta esquerda: fechada, com duas almofadas, uma rachadura e o puxador que restou.
    p.box((-0.236, -0.283, 0.8), (0.42, 0.012, 1.22), "MognoClaro", "root")
    for z, h in ((1.1, 0.5), (0.5, 0.55)):
        p.box((-0.236, -0.29, z), (0.32, 0.006, h), "Mogno", "root")
    for (x, z, ang, ln) in ((-0.3, 0.98, 70, 0.22), (-0.26, 0.83, 100, 0.12), (-0.33, 0.62, 80, 0.18)):
        p.box((x, -0.294, z), (0.004, 0.003, ln), "Sombra", "root", rot_y=ang - 90)
    p.ellipsoid((-0.04, -0.3, 0.82), (0.018,) * 3, "LataoVelho", "root", seg=10, rings=6)
    p.box((-0.04, -0.291, 0.74), (0.03, 0.004, 0.05), "Ferro", "root")   # espelho da fechadura
    p.box((-0.04, -0.294, 0.735), (0.006, 0.003, 0.018), "Sombra", "root")

    # Porta direita: entreaberta 20° na dobradiça (escuridão por dentro) e sem puxador.
    p.box((0.22, -0.2815, 0.8), (0.46, 0.004, 1.24), "Sombra", "root")
    ang = math.radians(20)
    hx, hy = 0.446, -0.29
    cx, cy = hx - 0.21 * math.cos(ang), hy - 0.21 * math.sin(ang)
    p.box((cx, cy, 0.8), (0.42, 0.012, 1.22), "MognoClaro", "root", rot_z=20)
    for z, h in ((1.1, 0.5), (0.5, 0.55)):
        p.box((cx - 0.007 * math.sin(ang), cy - 0.007, z), (0.32, 0.006, h), "Mogno", "root", rot_z=20)
    fx, fy = hx - 0.4 * math.cos(ang), hy - 0.4 * math.sin(ang)
    p.ellipsoid((fx, fy - 0.012, 0.82), (0.008,) * 3, "Ferro", "root", seg=6, rings=4)  # parafuso do puxador perdido
    for z in (0.3, 1.3):
        p.box((0.47, -0.285, z), (0.02, 0.025, 0.08), "Ferro", "root")   # dobradiças
    # Manga de veludo do figurino escapando pela fresta.
    p.polyline([(0.08, -0.29, 0.75), (0.05, -0.33, 0.6), (0.06, -0.34, 0.45), (0.03, -0.335, 0.36)],
               0.03, "Veludo", "root")

    # Rachadura na lateral direita e uma tábua do fundo solta, torta.
    p.box((0.481, 0.05, 0.9), (0.003, 0.004, 0.5), "Sombra", "root")
    p.box((0.1, 0.285, 0.55), (0.2, 0.012, 0.9), "Mogno", "root", rot_y=6)
    return p.to_static(col)


def build_cadeira(col):
    """Cadeira de auditório: 0,5 × 0,5 × 0,6 m (assento baixo, encosto curto), peso 1."""
    p = bc.Part("SM_PRP_Cadeira", MATS)
    for x in (-0.19, 0.19):
        for y in (-0.19, 0.19):
            top = 0.6 if y > 0 else 0.3  # as pernas de trás sobem e viram o encosto
            p.tube((x, y, 0.0), (x, y, top), 0.018, 0.016, "Madeira", "root", seg=8)
    p.box((0, 0, 0.32), (0.44, 0.44, 0.04), "Madeira", "root")
    p.box((0, -0.01, 0.35), (0.4, 0.38, 0.03), "Veludo", "root")      # almofada
    p.box((0, 0.2, 0.52), (0.4, 0.03, 0.12), "Madeira", "root")       # encosto
    p.box((0, 0.19, 0.25), (0.38, 0.02, 0.03), "Madeira", "root")     # travessa
    return p.to_static(col)


def build_caixa(col):
    """Caixa de mudança: cubo de 0,5 m, com fita, carimbo de FRÁGIL e o logo da empresa."""
    p = bc.Part("SM_PRP_Caixa", MATS)
    p.box((0, 0, 0.25), (0.5, 0.5, 0.5), "Papelao", "root")
    p.box((0, 0, 0.503), (0.505, 0.09, 0.006), "Fita", "root")        # fita por cima
    for y in (-0.253, 0.253):
        p.box((0, y, 0.43), (0.09, 0.006, 0.14), "Fita", "root")      # a fita desce na frente e atrás
    p.box((0, 0, 0.504), (0.5, 0.004, 0.004), "Sombra", "root")       # junta das abas
    p.box((0.07, -0.252, 0.22), (0.22, 0.004, 0.08), "Carimbo", "root")
    p.box((-0.252, 0, 0.25), (0.004, 0.16, 0.12), "Petroleo", "root")  # logo na lateral
    return p.to_static(col)


def build_caminhao(col):
    """Caminhão da Transportes Descanso Eterno, no mesmo formato do prefab:
    caçamba baixa (3 × 4 m) na origem e cabine (3 × 2 × 2,5 m) 3,5 m à frente (-Y aqui, +Z no Unity).
    """
    p = bc.Part("SM_PRP_Caminhao", MATS)
    # caçamba rente ao chão (os jogadores sobem nela) e postes nos cantos
    p.box((0, 0, 0.05), (3.0, 4.0, 0.1), "Aco", "root")
    p.box((0, 0, 0.102), (2.9, 3.9, 0.004), "Madeira", "root")
    for x in (-1.45, 1.45):
        for y in (-1.95, 1.95):
            p.box((x, y, 0.3), (0.08, 0.08, 0.5), "Petroleo", "root")
        p.box((x, 0, 0.12), (0.06, 4.0, 0.06), "Faixa", "root")
        for y in (-1.2, 1.2):
            p.wheel((x + 0.12 * (1 if x > 0 else -1), y, 0.35), 0.35, 0.28, "Pneu")
            p.box((x + 0.12 * (1 if x > 0 else -1), y, 0.74), (0.32, 0.9, 0.06), "Petroleo", "root")  # para-lama
    # cabine: corpo, cabine de cima, vidros, faróis, para-choque, faixa e logo
    p.box((0, -3.5, 0.85), (3.0, 2.0, 1.1), "Petroleo", "root")
    p.box((0, -3.4, 1.95), (2.8, 1.7, 1.1), "Petroleo", "root")
    p.box((0, -4.26, 2.0), (2.4, 0.02, 0.7), "Vidro", "root")
    for x in (-1.41, 1.41):
        p.box((x, -3.6, 2.05), (0.02, 1.0, 0.55), "Vidro", "root")
        p.box((x * 1.065, -3.5, 1.15), (0.006, 1.2, 0.12), "Faixa", "root")
        p.box((x * 1.065, -3.2, 0.75), (0.006, 0.9, 0.42), "Faixa", "root")  # placa do logo
        p.wheel((x + 0.12 * (1 if x > 0 else -1), -3.9, 0.4), 0.4, 0.3, "Pneu")
        p.box((x * 0.78, -4.505, 0.9), (0.3, 0.01, 0.18), "Farol", "root")
    p.box((0, -4.505, 0.75), (1.2, 0.01, 0.45), "Sombra", "root")     # grade
    p.box((0, -4.55, 0.35), (3.0, 0.1, 0.18), "Aco", "root")          # para-choque
    for x, y in ((-0.9, -2.9), (0.6, -3.1), (1.2, -2.7)):
        p.ellipsoid((x * 1.05, y, 0.55), (0.1, 0.1, 0.08), "Ferrugem", "root", seg=8, rings=5)
    return p.to_static(col)


# (nome, construtor, altura, enquadramento das prévias, meio do objeto em Y)
PROPS = [
    ("Armario", build_armario, 1.5, 1.6, 0.0),
    ("Cadeira", build_cadeira, 0.6, 0.75, 0.0),
    ("Caixa", build_caixa, 0.5, 0.7, 0.0),
    ("Caminhao", build_caminhao, 2.5, 7.0, -1.25),
]


def main():
    for name, build, height, frame, center_y in PROPS:
        scene, cols = bc.new_scene(name, kind="Props")
        obj = build(cols["Geo"])
        bc.report_tris((obj,))
        bc.finish(scene, cols, name, height, kind="Props", frame=frame, center_y=center_y)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
