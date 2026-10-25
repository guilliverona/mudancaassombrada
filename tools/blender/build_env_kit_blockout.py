"""Blockout do kit modular de cenário (ENV_01 a ENV_04 do Art Bible).

Rodar (fecha sozinho ao terminar; passe nomes depois de -- para gerar só alguns):
    blender --background --factory-startup --python tools/blender/build_env_kit_blockout.py

Saídas: Art/Source/Environment/<Nome>/ e MudancaAssombrada/Assets/Art/Environment/<Nome>/SM_ENV_<Nome>.fbx

Medidas do Art Bible: pé-direito 2,80 m · porta 0,90 × 2,10 m · módulo de parede 2 × 2,8 m ·
piso 2 × 2 m · molduras de espelho S/M/L = 0,6 / 1,2 / 2,0 m (âncoras do Layer Mirror).
Os módulos encaixam a cada 2 m: origem no meio da base, a parede corre em X e a frente
(o lado de dentro da sala) é -Y.
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import blockout_common as bc  # noqa: E402

WALL_W, WALL_H, WALL_T = 2.0, 2.8, 0.15
DOOR_W, DOOR_H = 0.9, 2.1

MATS = bc.Materials("MAT_ENV", {
    "PapelVinho": ("#5A1E2A", 0.0, 0.9, 0.0),      # papel de parede vinho (Casa do Espelho)
    "PapelVinhoEscuro": ("#3E1520", 0.0, 0.9, 0.0),
    "Reboco": ("#3A3442", 0.0, 0.95, 0.0),         # cenários vivem entre #131118 e #3A3442
    "Rodape": ("#2A1A16", 0.0, 0.7, 0.0),
    "Mogno": ("#5A2E22", 0.0, 0.7, 0.0),
    "MognoClaro": ("#6E3A2A", 0.0, 0.7, 0.0),
    "Dourado": ("#C9A24A", 1.0, 0.35, 0.0),
    "Espelho": ("#BFD0D6", 1.0, 0.05, 0.0),
    "XadrezClaro": ("#D8CBB0", 0.0, 0.6, 0.0),
    "XadrezEscuro": ("#131118", 0.0, 0.6, 0.0),
    "Tabua": ("#4A3A2E", 0.0, 0.8, 0.0),
    "TabuaClara": ("#5C4838", 0.0, 0.8, 0.0),
    "Latao": ("#D9B45A", 1.0, 0.3, 0.0),
})


def wall_body(p, x0, x1, z0, z1, mat):
    """Um pedaço retangular de parede (para montar paredes com vão de porta)."""
    if x1 - x0 <= 0 or z1 - z0 <= 0:
        return
    p.box(((x0 + x1) / 2, 0, (z0 + z1) / 2), (x1 - x0, WALL_T, z1 - z0), mat, "root")


def trims(p, x0, x1):
    """Rodapé e moldura de teto (os dois lados), entre x0 e x1."""
    if x1 - x0 <= 0:
        return
    for y in (-WALL_T / 2 - 0.01, WALL_T / 2 + 0.01):
        p.box(((x0 + x1) / 2, y, 0.06), (x1 - x0, 0.02, 0.12), "Rodape", "root")
        p.box(((x0 + x1) / 2, y, WALL_H - 0.04), (x1 - x0, 0.02, 0.08), "Mogno", "root")


def wallpaper_stripes(p, x0, x1):
    """Listras finas no papel de parede (só do lado de dentro, -Y): dá leitura no eco."""
    x = x0 + 0.1
    while x < x1 - 0.05:
        p.box((x, -WALL_T / 2 - 0.003, 1.45), (0.03, 0.004, 2.5), "PapelVinhoEscuro", "root")
        x += 0.25


def build_parede_lisa(col):
    p = bc.Part("SM_ENV_ParedeLisa", MATS)
    wall_body(p, -WALL_W / 2, WALL_W / 2, 0, WALL_H, "PapelVinho")
    wallpaper_stripes(p, -WALL_W / 2, WALL_W / 2)
    trims(p, -WALL_W / 2, WALL_W / 2)
    return p.to_static(col)


def build_parede_porta(col):
    """Parede de 2 m com o vão da porta (0,9 × 2,1 m) no meio e a guarnição de madeira."""
    p = bc.Part("SM_ENV_ParedePorta", MATS)
    half = DOOR_W / 2
    wall_body(p, -WALL_W / 2, -half, 0, WALL_H, "PapelVinho")
    wall_body(p, half, WALL_W / 2, 0, WALL_H, "PapelVinho")
    wall_body(p, -half, half, DOOR_H, WALL_H, "PapelVinho")
    wallpaper_stripes(p, -WALL_W / 2, -half)
    wallpaper_stripes(p, half, WALL_W / 2)
    trims(p, -WALL_W / 2, -half)
    trims(p, half, WALL_W / 2)
    # guarnição (batente) dos dois lados
    for y in (-WALL_T / 2 - 0.015, WALL_T / 2 + 0.015):
        for x in (-half - 0.04, half + 0.04):
            p.box((x, y, DOOR_H / 2), (0.08, 0.03, DOOR_H), "MognoClaro", "root")
        p.box((0, y, DOOR_H + 0.05), (DOOR_W + 0.16, 0.03, 0.1), "MognoClaro", "root")
    return p.to_static(col)


def build_porta(col):
    """Folha da porta (0,9 × 2,1 m) com almofadas e maçaneta. Origem na DOBRADIÇA (x = 0),
    para girar ao abrir; a porta corre para +X."""
    p = bc.Part("SM_ENV_Porta", MATS)
    w, h, t = DOOR_W - 0.02, DOOR_H - 0.01, 0.045
    p.box((w / 2, 0, h / 2), (w, t, h), "Mogno", "root")
    for z in (0.55, 1.45):
        p.box((w / 2, -t / 2 - 0.005, z), (w - 0.2, 0.01, 0.7), "MognoClaro", "root")  # almofadas
    p.ellipsoid((w - 0.08, -t / 2 - 0.03, 1.0), (0.025,) * 3, "Latao", "root", seg=10, rings=6)
    p.ellipsoid((w - 0.08, t / 2 + 0.03, 1.0), (0.025,) * 3, "Latao", "root", seg=10, rings=6)
    p.box((w - 0.08, -t / 2 - 0.004, 0.92), (0.03, 0.008, 0.08), "Latao", "root")       # espelho da fechadura
    return p.to_static(col)


def build_piso_xadrez(col):
    """Piso xadrez 2 × 2 m (Casa do Espelho): ladrilhos de 25 cm. Origem no meio, topo em z = 0."""
    p = bc.Part("SM_ENV_PisoXadrez", MATS)
    n, s = 8, 0.25
    for i in range(n):
        for j in range(n):
            mat = "XadrezClaro" if (i + j) % 2 == 0 else "XadrezEscuro"
            p.box((-1 + s / 2 + i * s, -1 + s / 2 + j * s, -0.01), (s, s, 0.02), mat, "root")
    return p.to_static(col)


def build_piso_tabuas(col):
    """Piso de tábuas 2 × 2 m (Biblioteca e Sobrado): madeira que range. Tábuas de 16 cm."""
    p = bc.Part("SM_ENV_PisoTabuas", MATS)
    w = 0.16
    n = int(2.0 / w)
    for i in range(n):
        mat = "Tabua" if i % 2 == 0 else "TabuaClara"
        # emendas desencontradas em cada tábua
        cut = -0.3 + (i * 37 % 7) * 0.12
        x = -1 + w / 2 + i * w
        p.box((x, (-1 + cut) / 2, -0.01), (w - 0.006, cut + 1 - 0.006, 0.02), mat, "root")
        p.box((x, (cut + 1) / 2, -0.01), (w - 0.006, 1 - cut - 0.006, 0.02), mat, "root")
    return p.to_static(col)


def build_moldura(name, height, col):
    """Moldura de espelho (SM_Moldura_S/M/L): dourada e entalhada, com o espelho. Proporção 2:3.
    Origem no meio da base, encostada na parede (o espelho olha para -Y)."""
    p = bc.Part(f"SM_ENV_{name}", MATS)
    h = height
    w = h * 0.62
    b = max(0.05, h * 0.07)        # largura da moldura
    t = 0.05 + h * 0.01
    zc = h / 2
    p.box((0, -t / 2 + 0.01, zc), (w - b, 0.01, h - b), "Espelho", "root")
    for x in (-(w - b) / 2, (w - b) / 2):
        p.box((x, 0, zc), (b, t, h), "Dourado", "root")
    for z in (b / 2, h - b / 2):
        p.box((0, 0, z), (w, t, b), "Dourado", "root")
    # entalhes: bolinhas nos cantos e um "brasão" no topo
    for x in (-(w - b) / 2, (w - b) / 2):
        for z in (b / 2, h - b / 2):
            p.ellipsoid((x, -t / 2, z), (b * 0.45, b * 0.25, b * 0.45), "Dourado", "root", seg=10, rings=6)
    p.ellipsoid((0, -t / 2, h + b * 0.3), (b * 1.2, b * 0.3, b * 0.7), "Dourado", "root", seg=12, rings=6)
    return p.to_static(col)


# (nome, construtor, altura, enquadramento das prévias, meio do objeto em Y)
PIECES = [
    ("ParedeLisa", build_parede_lisa, 2.8, 3.0, 0.0),
    ("ParedePorta", build_parede_porta, 2.8, 3.0, 0.0),
    ("Porta", build_porta, 2.1, 2.3, 0.0),
    ("PisoXadrez", build_piso_xadrez, 0.1, 2.4, 0.0),
    ("PisoTabuas", build_piso_tabuas, 0.1, 2.4, 0.0),
    ("MolduraS", lambda c: build_moldura("MolduraS", 0.6, c), 0.7, 0.8, 0.0),
    ("MolduraM", lambda c: build_moldura("MolduraM", 1.2, c), 1.3, 1.4, 0.0),
    ("MolduraL", lambda c: build_moldura("MolduraL", 2.0, c), 2.2, 2.3, 0.0),
]


def main():
    only = set(sys.argv[sys.argv.index("--") + 1:]) if "--" in sys.argv else None
    for name, build, height, frame, center_y in PIECES:
        if only and name not in only:
            continue
        scene, cols = bc.new_scene(name, kind="Environment")
        obj = build(cols["Geo"])
        bc.report_tris((obj,))
        bc.finish(scene, cols, name, height, kind="Environment", frame=frame, center_y=center_y)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
