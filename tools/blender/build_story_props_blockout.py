"""Blockout dos objetos de história (prancha PRP do Art Bible): rádio do chefe, bilhete,
nota fiscal, as três relíquias (cartaz, partitura, diário) e a caixinha de música.

Rodar (fecha sozinho ao terminar):
    blender --background --factory-startup --python tools/blender/build_story_props_blockout.py

Saídas, uma pasta por prop: Art/Source/Props/<Nome>/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Props/<Nome>/SM_PRP_<Nome>.fbx

Origem na base (no chão), frente para -Y. Medidas pensadas para serem carregadas no jogo
(as relíquias são pesadas pela massa no prefab, não pelo tamanho).
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import blockout_common as bc  # noqa: E402

MATS = bc.Materials("MAT_PRP", {
    # paleta mestre do Art Bible
    "Papel": ("#E9DFC8", 0.0, 0.9, 0.0),         # papel bilhete
    "Tinta": ("#2B2733", 0.0, 0.8, 0.0),
    "Laranja": ("#F08A3C", 0.0, 0.6, 0.0),       # laranja Descanso
    "Ferrugem": ("#9C4A2E", 0.2, 0.8, 0.0),
    "Ecto": ("#7FE3C4", 0.0, 0.3, 4.0),          # ectoplasma (emissivo)
    "Lanterna": ("#FFC98A", 0.0, 0.3, 2.0),      # mostrador aceso
    "Dourado": ("#C9A24A", 1.0, 0.35, 0.0),
    "Veludo": ("#7A1E2E", 0.0, 0.95, 0.0),
    "Porcelana": ("#F4F0E8", 0.0, 0.4, 0.0),
    "Sombra": ("#1C1A22", 0.0, 0.8, 0.0),
    "Couro": ("#5A3424", 0.0, 0.7, 0.0),
    "CouroEscuro": ("#3A2219", 0.0, 0.75, 0.0),
    "Madeira": ("#6E3A2A", 0.0, 0.6, 0.0),
    "Grade": ("#D8CBB0", 0.0, 0.8, 0.0),
    "Aco": ("#8E8794", 0.9, 0.4, 0.0),
    "Marfim": ("#EDEBE4", 0.0, 0.5, 0.0),
    "Carimbo": ("#C8463C", 0.0, 0.8, 0.0),
    "Espelho": ("#BFD0D6", 1.0, 0.05, 0.0),
})


def text_lines(p, x0, x1, z_top, rows, gap, y, z_step_mat="Tinta", thick=0.004, depth=0.002, ragged=True):
    """Linhas de "texto" (faixas finas) numa folha em pé: o texto real entra por decal no jogo."""
    for i in range(rows):
        right = x1 - (0.04 * (i % 3) if ragged else 0.0)
        z = z_top - i * gap
        p.box(((x0 + right) / 2, y, z), (right - x0, depth, thick), z_step_mat, "root")


def build_radio(col):
    """Rádio chiado do Seu Valdemar (PRP_Radio_Valdemar): caixa de madeira com grade,
    mostrador aceso (_E) e dois botões. 0,42 × 0,2 × 0,3 m + antena."""
    p = bc.Part("SM_PRP_Radio", MATS)
    p.box((0, 0, 0.15), (0.42, 0.2, 0.28), "Madeira", "root")
    p.box((0, 0, 0.292), (0.44, 0.22, 0.02), "CouroEscuro", "root")      # tampo
    for x in (-0.17, 0.17):
        p.box((x, 0, 0.008), (0.05, 0.16, 0.016), "CouroEscuro", "root")  # pés
    # grade (lado esquerdo da frente) com frisos
    p.box((-0.07, -0.101, 0.15), (0.2, 0.004, 0.2), "Sombra", "root")
    for i in range(6):
        p.box((-0.07, -0.104, 0.07 + i * 0.032), (0.19, 0.004, 0.012), "Grade", "root")
    # mostrador (redondo, aceso) e botões
    p.tube((0.12, -0.104, 0.19), (0.12, -0.096, 0.19), 0.048, 0.048, "Lanterna", "root", seg=20, smooth=False)
    p.box((0.12, -0.106, 0.205), (0.004, 0.003, 0.04), "Ferrugem", "root")  # ponteiro
    for x in (0.09, 0.15):
        p.tube((x, -0.11, 0.08), (x, -0.1, 0.08), 0.017, 0.017, "Dourado", "root", seg=12, smooth=False)
    # antena inclinada
    p.tube((0.16, 0.06, 0.3), (0.23, 0.08, 0.58), 0.005, 0.003, "Aco", "root", seg=6)
    p.ellipsoid((0.23, 0.08, 0.585), (0.009,) * 3, "Aco", "root", seg=8, rings=5)
    return p.to_static(col)


def build_bilhete(col):
    """Bilhete do chefe (PRP_Bilhete): folha 0,21 × 0,15 m quase plana, com linhas e "— V."
    Deitada no chão: origem no meio da folha."""
    p = bc.Part("SM_PRP_Bilhete", MATS)
    p.box((0, 0, 0.001), (0.21, 0.15, 0.002), "Papel", "root")
    for i in range(5):
        w = 0.15 - 0.03 * (i % 2)
        p.box((-0.02 + (0.15 - w) / -2, 0.045 - i * 0.02, 0.0025), (w, 0.004, 0.001), "Tinta", "root")
    p.box((0.06, -0.055, 0.0025), (0.04, 0.006, 0.001), "Tinta", "root")  # assinatura "— V."
    return p.to_static(col)


def build_nota_fiscal(col):
    """Nota fiscal de "remoção de entidade": folha branca com cabeçalho laranja e TOTAL."""
    p = bc.Part("SM_PRP_NotaFiscal", MATS)
    p.box((0, 0, 0.001), (0.15, 0.22, 0.002), "Marfim", "root")
    p.box((0, 0.093, 0.0025), (0.15, 0.034, 0.001), "Laranja", "root")   # NOTA FISCAL
    for i in range(4):
        p.box((-0.01, 0.05 - i * 0.022, 0.0025), (0.11, 0.004, 0.001), "Tinta", "root")
    p.box((0, -0.075, 0.0025), (0.13, 0.002, 0.001), "Tinta", "root")
    p.box((0.035, -0.09, 0.0025), (0.06, 0.008, 0.001), "Tinta", "root")  # TOTAL [R$]
    return p.to_static(col)


def build_cartaz(col):
    """Relíquia 1 · Cartaz do teatro (PRP_Cartaz_Teatro, Fase 1): moldura dourada em pé,
    veludo vinho, máscara da comédia e "GRANDE ELENCO … VALDEMAR …". 0,7 × 1,0 m."""
    p = bc.Part("SM_PRP_CartazTeatro", MATS)
    w, h, t = 0.7, 1.0, 0.05
    zc = h / 2
    p.box((0, 0, zc), (w - 0.08, t * 0.5, h - 0.08), "Veludo", "root")  # cartaz
    for x in (-(w - 0.04) / 2, (w - 0.04) / 2):
        p.box((x, 0, zc), (0.04, t, h), "Dourado", "root")              # moldura (laterais)
    for z in (0.02, h - 0.02):
        p.box((0, 0, z), (w, t, 0.04), "Dourado", "root")               # moldura (topo/base)
    # máscara da comédia: rosto, olhos e sorriso em arco
    face = (0, -0.02, 0.66)
    p.ellipsoid(face, (0.12, 0.03, 0.15), "Porcelana", "root", seg=20, rings=10)
    for s in (1, -1):
        p.ellipsoid((0.045 * s, -0.048, 0.7), (0.022, 0.01, 0.014), "Sombra", "root", seg=10, rings=5)
    # sorriso: o meio do arco fica mais BAIXO que os cantos (sen < 0 entre 180° e 360°)
    smile = [(math.cos(a) * 0.065, -0.05, 0.62 + math.sin(a) * 0.035) for a in
             [math.radians(180 + i * 30) for i in range(7)]]
    p.polyline(smile, 0.008, "Sombra", "root", seg=6)
    # faixas de texto: "GRANDE ELENCO" e "… VALDEMAR …"
    p.box((0, -0.016, 0.38), (0.4, 0.004, 0.035), "Papel", "root")
    p.box((0, -0.016, 0.3), (0.32, 0.004, 0.028), "Dourado", "root")
    return p.to_static(col)


def build_partitura(col):
    """Relíquia 2 · Partitura da Maestrina (PRP_Partitura_Odete, Fase 2): maço de folhas
    0,3 × 0,4 m com pautas e a fita vermelha, deitado."""
    p = bc.Part("SM_PRP_PartituraOdete", MATS)
    for i, rot in enumerate((0, 4, -3, 2)):
        z = 0.004 + i * 0.006
        p.box((0.004 * i, 0.003 * i, z), (0.3, 0.4, 0.004), "Papel", "root", rot_y=0)
    top = 0.004 + 3 * 0.006 + 0.003
    # pautas (5 linhas por sistema) e algumas notas
    for sysi in range(4):
        y0 = 0.13 - sysi * 0.085
        for li in range(5):
            p.box((0.012, y0 - li * 0.009, top), (0.24, 0.0015, 0.001), "Tinta", "root")
        for k in range(5):
            p.ellipsoid((-0.08 + k * 0.045, y0 - (k % 4) * 0.009, top + 0.001), (0.006, 0.0045, 0.002),
                        "Tinta", "root", seg=8, rings=4)
    # título "Última Apresentação — O." e a fita
    p.box((0.0, 0.17, top), (0.18, 0.012, 0.001), "Tinta", "root")
    p.box((0.0, -0.1, top + 0.003), (0.32, 0.04, 0.004), "Carimbo", "root")
    return p.to_static(col)


def build_diario(col):
    """Relíquia 3 · Diário do Seu Valdemar (PRP_Diario_Valdemar, Fase 3): livrão de couro com
    "V." dourado, cantoneiras e bilhetes saindo das páginas. 0,45 × 0,6 × 0,14 m, deitado."""
    p = bc.Part("SM_PRP_DiarioValdemar", MATS)
    w, d, t = 0.45, 0.6, 0.14
    p.box((0.005, 0, t / 2), (w - 0.03, d - 0.03, t - 0.03), "Papel", "root")       # miolo
    p.box((0, 0, 0.012), (w, d, 0.024), "Couro", "root")                            # capa de baixo
    p.box((0, 0, t - 0.012), (w, d, 0.024), "Couro", "root")                        # capa de cima
    p.box((-w / 2 + 0.012, 0, t / 2), (0.024, d, t), "CouroEscuro", "root")         # lombada
    for y in (-d / 2 + 0.06, d / 2 - 0.06):
        p.box((-w / 2 + 0.03, y, t / 2), (0.03, 0.02, t + 0.004), "CouroEscuro", "root")  # nervuras
    for x, y in ((w / 2 - 0.03, d / 2 - 0.03), (w / 2 - 0.03, -d / 2 + 0.03)):
        p.box((x, y, t - 0.004), (0.05, 0.05, 0.01), "Dourado", "root")             # cantoneiras
    # "V." dourado na capa
    p.polyline([(0.0, 0.07, t + 0.001), (0.03, -0.03, t + 0.001), (0.06, 0.07, t + 0.001)],
               0.008, "Dourado", "root", seg=6)
    p.ellipsoid((0.085, -0.025, t + 0.001), (0.01, 0.01, 0.004), "Dourado", "root", seg=8, rings=4)
    # bilhetes enfiados entre as páginas
    for (x, y, z, sx, sy) in ((w / 2 + 0.04, 0.12, 0.07, 0.12, 0.08), (w / 2 + 0.02, -0.15, 0.09, 0.09, 0.07),
                               (0.05, d / 2 + 0.03, 0.06, 0.08, 0.1)):
        p.box((x - 0.03, y, z), (sx, sy, 0.003), "Papel", "root")
    return p.to_static(col)


def build_caixinha(col):
    """Caixinha de música (PRP_Caixinha, Fase 2 e final): caixa vinho com friso dourado,
    tampa aberta, bailarina girando e a manivela da corda. 0,3 × 0,2 × 0,14 m."""
    p = bc.Part("SM_PRP_Caixinha", MATS)
    w, d, h = 0.3, 0.2, 0.13
    p.box((0, 0, h / 2), (w, d, h), "Veludo", "root")
    p.box((0, 0, h + 0.004), (w - 0.02, d - 0.02, 0.008), "Sombra", "root")         # interior
    p.box((0, -d / 2 - 0.001, 0.035), (w + 0.004, 0.006, 0.014), "Dourado", "root")  # friso
    p.box((0, d / 2 + 0.02, h + 0.1), (w, 0.016, 0.2), "Veludo", "root")            # tampa aberta (em pé)
    # espelhinho. Sem ciano: o Art Bible reserva o ectoplasma para olhos, bordas e rastros de
    # fantasma; o brilho amaldiçoado da caixinha é efeito (VFX) no jogo, não material.
    p.box((0, d / 2 + 0.011, h + 0.1), (w - 0.04, 0.002, 0.16), "Espelho", "root")
    # bailarina: pino, corpo e saia
    p.tube((0, 0, h), (0, 0, h + 0.03), 0.004, 0.004, "Dourado", "root", seg=6)
    p.tube((0, 0, h + 0.03), (0, 0, h + 0.055), 0.022, 0.003, "Porcelana", "root", seg=12)  # saia
    p.tube((0, 0, h + 0.055), (0, 0, h + 0.085), 0.006, 0.005, "Porcelana", "root", seg=8)
    p.ellipsoid((0, 0, h + 0.093), (0.009,) * 3, "Porcelana", "root", seg=10, rings=6)
    p.polyline([(-0.02, 0, h + 0.1), (0, 0, h + 0.08), (0.02, 0, h + 0.1)], 0.003, "Porcelana", "root", seg=5)
    # manivela da corda (lado direito)
    p.tube((w / 2, 0, 0.07), (w / 2 + 0.03, 0, 0.07), 0.005, 0.005, "Dourado", "root", seg=8)
    p.box((w / 2 + 0.033, 0, 0.05), (0.008, 0.008, 0.05), "Dourado", "root")
    p.ellipsoid((w / 2 + 0.033, -0.01, 0.026), (0.008, 0.012, 0.008), "Dourado", "root", seg=8, rings=5)
    return p.to_static(col)


# (nome, construtor, altura, enquadramento das prévias, meio do objeto em Y)
PROPS = [
    ("Radio", build_radio, 0.6, 0.75, 0.0),
    ("Bilhete", build_bilhete, 0.05, 0.3, 0.0),
    ("NotaFiscal", build_nota_fiscal, 0.05, 0.3, 0.0),
    ("CartazTeatro", build_cartaz, 1.0, 1.2, 0.0),
    ("PartituraOdete", build_partitura, 0.05, 0.5, 0.0),
    ("DiarioValdemar", build_diario, 0.15, 0.75, 0.0),
    ("Caixinha", build_caixinha, 0.35, 0.45, 0.0),
]


def main():
    only = set(sys.argv[sys.argv.index("--") + 1:]) if "--" in sys.argv else None
    for name, build, height, frame, center_y in PROPS:
        if only and name not in only:
            continue
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
