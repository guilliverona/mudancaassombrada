"""Blockout dos objetos de 1927 da Mansão do Espelho (LEVEL_DESIGN_FASE1_MANSAO.md §8 e §11, "Novo: época"):
os achados de época, o lampião de querosene, a cozinha (fogão, bancada, pratos), o aparador com
gaveta, os livros que caem da estante e a teia de vão (rasgável).

Rodar (fecha sozinho ao terminar; passe nomes depois de -- para gerar só alguns):
    blender --background --factory-startup --python tools/blender/build_epoca_blockout.py
    blender ... --python tools/blender/build_epoca_blockout.py -- Cofre Psiche

Saídas, uma pasta por objeto: Art/Source/Props/<Nome>/ (.blend e prévias) e
MudancaAssombrada/Assets/Art/Props/<Nome>/SM_PRP_<Nome>.fbx

Medidas (largura × profundidade × altura) da tabela §8.2. Origem na base, frente -Y.
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import blockout_common as bc  # noqa: E402

MATS = bc.Materials("MAT_PRP", {
    "Mogno": ("#5A2E22", 0.0, 0.7, 0.0),
    "MognoClaro": ("#6E3A2A", 0.0, 0.7, 0.0),
    "Madeira": ("#8A5A3A", 0.0, 0.75, 0.0),
    "MadeiraVelha": ("#9C4A2E", 0.0, 0.85, 0.0),
    "Palhinha": ("#C9A86A", 0.0, 0.9, 0.0),         # o trançado da cadeira austríaca
    "Barro": ("#A0583A", 0.0, 0.9, 0.0),
    "Dourado": ("#C9A24A", 1.0, 0.35, 0.0),
    "Latao": ("#D9B45A", 1.0, 0.3, 0.0),
    "Ferro": ("#3A3A40", 1.0, 0.5, 0.0),
    "FerroVelho": ("#5A4E48", 0.8, 0.7, 0.0),
    "Preto": ("#131118", 0.0, 0.4, 0.0),
    "Marfim": ("#EDEBE4", 0.0, 0.4, 0.0),
    "Louca": ("#E6E2D6", 0.0, 0.3, 0.0),
    "Vidro": ("#9FB4BC", 0.6, 0.05, 0.0),
    "Espelho": ("#BFD0D6", 1.0, 0.05, 0.0),
    "Tecido": ("#6B4A6E", 0.0, 0.95, 0.0),
    "TecidoBiombo": ("#7A5A3E", 0.0, 0.95, 0.0),
    "Papel": ("#D8D0BC", 0.0, 0.9, 0.0),
    "Tijolo": ("#7A3E2E", 0.0, 0.95, 0.0),
    "Lampada": ("#FFC98A", 0.0, 0.3, 2.0),          # chama do lampião (luz quente = porto seguro)
    "Lente": ("#FFE8B0", 0.0, 0.2, 1.5),
    "Teia": ("#8E8E96", 0.0, 0.9, 0.0),
    "Livro1": ("#6E3A2A", 0.0, 0.8, 0.0),
    "Livro2": ("#3F5A5E", 0.0, 0.8, 0.0),
    "Livro3": ("#4A3A2E", 0.0, 0.8, 0.0),
    "Livro4": ("#6B4A6E", 0.0, 0.8, 0.0),
})


def legs4(p, w, d, h, mat, inset=0.04, size=0.05):
    for x in (-w / 2 + inset, w / 2 - inset):
        for y in (-d / 2 + inset, d / 2 - inset):
            p.box((x, y, h / 2), (size, size, h), mat, "root")


# ---------- Achados de época (§8.2) ----------

def build_cristaleira(col):
    """Cristaleira (1,0 × 0,4 × 1,8 m, frágil): vitrine de portas de vidro com pratos e taças."""
    p = bc.Part("SM_PRP_Cristaleira", MATS)
    w, d, h = 1.0, 0.4, 1.8
    p.box((0, 0, 0.35), (w, d, 0.7), "Mogno", "root")                     # base com portas de madeira
    for x in (-0.25, 0.25):
        p.box((x, -d / 2 - 0.005, 0.38), (0.44, 0.01, 0.56), "MognoClaro", "root")
        p.box((x * 0.2, -d / 2 - 0.02, 0.45), (0.02, 0.02, 0.06), "Latao", "root")
    p.box((0, 0.05, 1.25), (w - 0.04, d - 0.1, 1.1), "Mogno", "root")      # caixa da vitrine
    p.box((0, -d / 2 + 0.04, 1.25), (w - 0.1, 0.01, 1.02), "Vidro", "root")  # portas de vidro
    p.box((0, -d / 2 + 0.035, 1.25), (0.02, 0.012, 1.02), "MognoClaro", "root")
    for z in (0.95, 1.3, 1.62):                                            # prateleiras com louça
        p.box((0, 0.05, z - 0.01), (w - 0.1, d - 0.14, 0.015), "MognoClaro", "root")
        for i in range(4):
            x = -0.33 + i * 0.22
            p.tube((x, 0.12, z), (x, 0.11, z + 0.2), 0.09, 0.09, "Louca", "root", seg=14)  # prato em pé
            p.tube((x + 0.08, 0.0, z), (x + 0.08, 0.0, z + 0.12), 0.025, 0.035, "Vidro", "root", seg=8)
    p.box((0, 0, h - 0.03), (w + 0.04, d + 0.04, 0.06), "MognoClaro", "root")  # cornija
    return p.to_static(col)


def build_telefone(col):
    """Telefone de parede de manivela (0,25 × 0,15 × 0,6 m): caixa de madeira, campainhas, fone no gancho."""
    p = bc.Part("SM_PRP_TelefoneParede", MATS)
    p.box((0, 0.02, 0.3), (0.25, 0.12, 0.6), "MognoClaro", "root")
    p.box((0, -0.045, 0.05), (0.2, 0.06, 0.1), "MognoClaro", "root")           # pé, para ficar em pé no chão
    for x in (-0.06, 0.06):
        p.ellipsoid((x, -0.05, 0.5), (0.04, 0.025, 0.04), "Latao", "root", seg=12, rings=6)  # campainhas
    p.tube((0, -0.04, 0.38), (0, -0.12, 0.34), 0.03, 0.025, "Preto", "root", seg=10)   # bocal
    p.tube((0.125, 0.0, 0.32), (0.18, 0.0, 0.32), 0.008, 0.008, "Ferro", "root", seg=6)  # manivela
    p.box((0.19, -0.02, 0.29), (0.012, 0.012, 0.07), "Ferro", "root")
    p.tube((-0.15, -0.03, 0.42), (-0.15, -0.03, 0.26), 0.022, 0.018, "Preto", "root", seg=10)  # fone
    p.box((0, -0.035, 0.2), (0.14, 0.01, 0.08), "Papel", "root")                # plaquinha do número
    return p.to_static(col)


def build_cadeira_balanco(col):
    """Cadeira de balanço de palhinha (0,6 × 0,9 × 1,0 m): os arcos no chão e o encosto alto."""
    p = bc.Part("SM_PRP_CadeiraBalanco", MATS)
    for x in (-0.26, 0.26):
        # o arco: tubos curtos seguindo uma curva rasa
        pts = [(x, -0.45 + i * 0.15, 0.03 + 0.05 * ((i - 3) / 3) ** 2) for i in range(7)]
        p.polyline(pts, 0.018, "Mogno", "root", seg=6)
        p.box((x, -0.2, 0.22), (0.035, 0.035, 0.38), "Mogno", "root")
        p.box((x, 0.18, 0.55), (0.035, 0.035, 1.0), "Mogno", "root", rot_y=0)
        p.box((x, 0.0, 0.62), (0.03, 0.42, 0.03), "Mogno", "root")                 # braço
    p.box((0, -0.02, 0.42), (0.52, 0.42, 0.03), "Palhinha", "root")               # assento
    p.box((0, 0.2, 0.8), (0.5, 0.025, 0.55), "Palhinha", "root", rot_y=0)         # encosto
    p.box((0, 0.2, 1.06), (0.56, 0.04, 0.05), "Mogno", "root")
    return p.to_static(col)


def build_rolos(col):
    """Caixa de rolos de pianola (0,5 × 0,3 × 0,3 m): oito rolos de papel em pé, tampa aberta."""
    p = bc.Part("SM_PRP_RolosPianola", MATS)
    p.box((0, 0, 0.1), (0.5, 0.3, 0.2), "MadeiraVelha", "root")
    p.box((0, 0.17, 0.3), (0.5, 0.02, 0.2), "MadeiraVelha", "root", rot_y=0)    # tampa aberta para trás
    for i in range(8):
        x = -0.19 + (i % 4) * 0.125
        y = -0.06 + (i // 4) * 0.11
        p.tube((x, y, 0.05), (x, y, 0.28), 0.045, 0.045, "Papel", "root", seg=10)
        p.tube((x, y, 0.28), (x, y, 0.3), 0.02, 0.02, "Latao", "root", seg=6)
    return p.to_static(col)


def build_cofre(col):
    """Cofre da bilheteria (0,7 × 0,6 × 0,9 m, 3,5): ferro, segredo de latão, letreiro dourado."""
    p = bc.Part("SM_PRP_Cofre", MATS)
    p.box((0, 0, 0.47), (0.7, 0.6, 0.86), "Ferro", "root")
    p.box((0, -0.305, 0.47), (0.58, 0.02, 0.72), "FerroVelho", "root")           # porta
    p.tube((0.0, -0.31, 0.55), (0.0, -0.34, 0.55), 0.06, 0.055, "Latao", "root", seg=16)  # segredo
    p.box((0.18, -0.33, 0.4), (0.03, 0.05, 0.14), "Latao", "root")                 # alavanca
    p.box((0, -0.316, 0.75), (0.4, 0.004, 0.06), "Dourado", "root")                # BILHETERIA
    for x in (-0.3, 0.3):
        for y in (-0.25, 0.25):
            p.ellipsoid((x, y, 0.03), (0.035, 0.035, 0.03), "Latao", "root", seg=8, rings=4)  # rodinhas
    return p.to_static(col)


def build_maquina_escrever(col):
    """Máquina de escrever (0,45 × 0,35 × 0,25 m): teclas redondas em degraus e o carro com a folha."""
    p = bc.Part("SM_PRP_MaquinaEscrever", MATS)
    p.box((0, 0.02, 0.06), (0.42, 0.3, 0.12), "Preto", "root")
    p.box((0, 0.1, 0.15), (0.36, 0.14, 0.1), "Preto", "root")
    for row in range(4):
        for i in range(10 - row):
            x = -0.16 + (i + row * 0.5) * 0.034
            p.tube((x, -0.1 + row * 0.03, 0.12 + row * 0.012), (x, -0.1 + row * 0.03, 0.13 + row * 0.012),
                   0.011, 0.011, "Marfim", "root", seg=8)
    p.tube((-0.24, 0.16, 0.21), (0.24, 0.16, 0.21), 0.025, 0.025, "Preto", "root", seg=10)  # o rolo
    p.box((0, 0.17, 0.27), (0.2, 0.004, 0.12), "Papel", "root", rot_y=0)                     # a folha
    p.box((-0.27, 0.16, 0.23), (0.06, 0.012, 0.012), "Latao", "root")                        # alavanca
    return p.to_static(col)


def build_lanterna_magica(col):
    """Lanterna mágica (0,35 × 0,25 × 0,4 m, frágil): caixa de latão, chaminé e a lente na frente."""
    p = bc.Part("SM_PRP_LanternaMagica", MATS)
    p.box((0, 0.02, 0.14), (0.24, 0.22, 0.26), "Latao", "root")
    p.tube((0, 0.04, 0.27), (0, 0.04, 0.4), 0.04, 0.03, "FerroVelho", "root", seg=10)      # chaminé
    p.tube((0, -0.09, 0.15), (0, -0.2, 0.15), 0.05, 0.045, "Latao", "root", seg=14)        # cano da lente
    p.tube((0, -0.2, 0.15), (0, -0.205, 0.15), 0.045, 0.045, "Lente", "root", seg=14)
    p.box((0.13, 0.0, 0.15), (0.02, 0.14, 0.08), "Vidro", "root")                          # lâmina de vidro
    return p.to_static(col)


def build_psiche(col):
    """Espelho de pé (psiché) (0,6 × 0,5 × 1,7 m, frágil): o espelho oval entre dois montantes."""
    p = bc.Part("SM_PRP_Psiche", MATS)
    for x in (-0.28, 0.28):
        p.box((x, 0, 0.75), (0.04, 0.04, 1.5), "Mogno", "root")
        p.box((x, 0, 0.03), (0.06, 0.5, 0.05), "Mogno", "root")                          # pés
    p.ellipsoid((0, 0, 1.0), (0.24, 0.03, 0.55), "Mogno", "root", seg=24, rings=10)
    p.ellipsoid((0, -0.012, 1.0), (0.21, 0.025, 0.51), "Espelho", "root", seg=24, rings=10)
    p.tube((-0.28, 0, 1.0), (0.28, 0, 1.0), 0.012, 0.012, "Latao", "root", seg=6)           # eixo
    return p.to_static(col)


def build_maquina_costura(col):
    """Máquina de costura de pedal (0,9 × 0,45 × 1,0 m): mesa, a máquina preta e o pedal de ferro."""
    p = bc.Part("SM_PRP_MaquinaCostura", MATS)
    p.box((0, 0, 0.76), (0.9, 0.45, 0.04), "MognoClaro", "root")                         # tampo
    for x in (-0.38, 0.38):                                                               # pés de ferro
        p.box((x, 0, 0.38), (0.04, 0.38, 0.74), "Ferro", "root")
    p.box((0, -0.04, 0.1), (0.5, 0.22, 0.02), "Ferro", "root", rot_y=0)                  # pedal
    p.wheel((0.36, 0.1, 0.45), 0.2, 0.03, "Ferro", "root")                               # volante
    p.box((-0.05, 0.02, 0.86), (0.4, 0.12, 0.16), "Preto", "root")                       # a máquina
    p.box((-0.22, 0.02, 0.96), (0.08, 0.12, 0.04), "Preto", "root")
    p.box((0.12, 0.02, 0.94), (0.06, 0.12, 0.2), "Preto", "root")
    p.tube((0.18, 0.02, 0.96), (0.2, 0.02, 0.96), 0.05, 0.05, "Dourado", "root", seg=12)
    return p.to_static(col)


def build_biombo(col):
    """Biombo de camarim (1,5 × 0,3 × 1,7 m): três folhas de tecido em zigue-zague."""
    p = bc.Part("SM_PRP_Biombo", MATS)
    for i, ang in enumerate((25, -25, 25)):
        x = -0.5 + i * 0.5
        y = 0.08 if i % 2 == 0 else -0.08
        p.box((x, y, 0.88), (0.5, 0.03, 1.62), "Mogno", "root", rot_z=ang)
        p.box((x, y - 0.002, 0.9), (0.42, 0.03, 1.4), "TecidoBiombo", "root", rot_z=ang)
    return p.to_static(col)


def build_talha(col):
    """Talha de barro (0,45 × 0,45 × 0,6 m, frágil): o filtro d'água da cozinha, com tampa e caneca."""
    p = bc.Part("SM_PRP_Talha", MATS)
    p.ellipsoid((0, 0, 0.3), (0.22, 0.22, 0.28), "Barro", "root", seg=18, rings=10)
    p.tube((0, 0, 0.5), (0, 0, 0.58), 0.12, 0.14, "Barro", "root", seg=16)
    p.tube((0, 0, 0.58), (0, 0, 0.6), 0.15, 0.15, "MadeiraVelha", "root", seg=16)        # tampa
    p.tube((0, -0.2, 0.12), (0, -0.25, 0.12), 0.015, 0.015, "Latao", "root", seg=6)      # torneirinha
    p.tube((0.08, 0, 0.6), (0.08, 0, 0.66), 0.035, 0.035, "Louca", "root", seg=10)        # caneca
    return p.to_static(col)


def build_lampiao(col):
    """Lampião de querosene (0,2 × 0,2 × 0,4 m): base de latão, chaminé de vidro e a chama.
    Ocupa as duas mãos (é carregado como um móvel) e não apaga."""
    p = bc.Part("SM_PRP_Lampiao", MATS)
    p.tube((0, 0, 0), (0, 0, 0.12), 0.09, 0.07, "Latao", "root", seg=14)                 # reservatório
    p.tube((0, 0, 0.12), (0, 0, 0.32), 0.055, 0.04, "Vidro", "root", seg=14)             # chaminé
    p.ellipsoid((0, 0, 0.18), (0.015, 0.015, 0.035), "Lampada", "root", seg=8, rings=5)  # chama
    p.tube((0, 0, 0.32), (0, 0, 0.34), 0.045, 0.045, "Latao", "root", seg=10)
    p.polyline([(-0.06, 0, 0.12), (-0.07, 0, 0.36), (0, 0, 0.42), (0.07, 0, 0.36), (0.06, 0, 0.12)],
               0.005, "Ferro", "root", seg=5)                                            # alça
    return p.to_static(col)


# ---------- Cozinha, jantar e biblioteca (cenário que cai) ----------

def build_fogao(col):
    """Fogão a lenha (1,2 × 0,7 × 0,85 m): corpo de tijolo, chapa de ferro, chaminé e portinholas."""
    p = bc.Part("SM_PRP_FogaoLenha", MATS)
    p.box((0, 0, 0.4), (1.2, 0.7, 0.8), "Tijolo", "root")
    p.box((0, 0, 0.82), (1.22, 0.72, 0.04), "Ferro", "root")
    for x in (-0.3, 0.1):
        p.tube((x, -0.05, 0.84), (x, -0.05, 0.85), 0.11, 0.11, "FerroVelho", "root", seg=14)  # bocas
    for x in (-0.3, 0.25):
        p.box((x, -0.355, 0.5), (0.26, 0.02, 0.2), "Ferro", "root")                       # portinholas
    p.tube((0.45, 0.25, 0.84), (0.45, 0.25, 2.0), 0.08, 0.08, "Ferro", "root", seg=10)   # chaminé
    return p.to_static(col)


def build_bancada(col):
    """Bancada da cozinha (1,6 × 0,6 × 0,9 m): onde os pratos estão empilhados."""
    p = bc.Part("SM_PRP_Bancada", MATS)
    p.box((0, 0, 0.88), (1.6, 0.6, 0.04), "MadeiraVelha", "root")
    p.box((0, 0.05, 0.43), (1.5, 0.48, 0.82), "Madeira", "root")
    for x in (-0.5, 0.0, 0.5):
        p.box((x, -0.19, 0.5), (0.44, 0.01, 0.56), "MadeiraVelha", "root")
        p.box((x + 0.15, -0.2, 0.7), (0.03, 0.02, 0.03), "Latao", "root")
    return p.to_static(col)


def build_pratos(col):
    """Pratos empilhados (0,26 × 0,26 × 0,2 m): a pilha que cai quando um móvel bate na bancada."""
    p = bc.Part("SM_PRP_PratosEmpilhados", MATS)
    for i in range(9):
        z = i * 0.02
        dx = (i * 7 % 5 - 2) * 0.004
        p.tube((dx, 0, z), (dx, 0, z + 0.016), 0.12, 0.13, "Louca", "root", seg=16)
    p.tube((0.02, 0, 0.18), (0.02, 0, 0.2), 0.06, 0.07, "Louca", "root", seg=12)            # uma xícara em cima
    return p.to_static(col)


def build_aparador(col):
    """Aparador do jantar (1,4 × 0,45 × 0,85 m) com o vão da gaveta do meio vazio: a gaveta é
    outra peça (GavetaAparador), que abre sozinha."""
    p = bc.Part("SM_PRP_Aparador", MATS)
    p.box((0, 0, 0.83), (1.4, 0.45, 0.04), "MognoClaro", "root")
    p.box((0, 0.2, 0.47), (1.36, 0.04, 0.7), "Mogno", "root")                              # fundo
    for x in (-0.66, -0.26, 0.26, 0.66):
        p.box((x, 0, 0.47), (0.04, 0.43, 0.7), "Mogno", "root")
    p.box((0, 0, 0.13), (1.36, 0.43, 0.02), "Mogno", "root")                               # fundo de baixo
    p.box((0, 0, 0.62), (0.48, 0.43, 0.02), "Mogno", "root")                               # chão da gaveta
    for x in (-0.46, 0.46):
        p.box((x, -0.205, 0.47), (0.38, 0.02, 0.66), "MognoClaro", "root")                 # portas
        p.box((x * 0.6, -0.22, 0.5), (0.02, 0.02, 0.06), "Latao", "root")
    p.box((0, -0.205, 0.38), (0.48, 0.02, 0.48), "MognoClaro", "root")                     # porta do meio
    legs4(p, 1.36, 0.42, 0.12, "Mogno")
    return p.to_static(col)


def build_gaveta(col):
    """A gaveta do aparador (0,46 × 0,4 × 0,16 m): abre 25 cm e larga uma pilha escondida."""
    p = bc.Part("SM_PRP_GavetaAparador", MATS)
    p.box((0, -0.19, 0.08), (0.46, 0.02, 0.16), "MognoClaro", "root")                      # frente
    p.box((0, 0, 0.01), (0.42, 0.38, 0.02), "Mogno", "root")
    for x in (-0.205, 0.205):
        p.box((x, 0, 0.06), (0.02, 0.38, 0.1), "Mogno", "root")
    p.box((0, 0.185, 0.06), (0.42, 0.02, 0.1), "Mogno", "root")
    p.tube((0, -0.2, 0.09), (0, -0.23, 0.09), 0.015, 0.02, "Latao", "root", seg=8)         # puxador
    return p.to_static(col)


def build_livros_soltos(col):
    """Cinco livros soltos (0,4 × 0,22 × 0,3 m), em pé, para a borda de uma prateleira: são eles que caem."""
    p = bc.Part("SM_PRP_LivrosSoltos", MATS)
    x = -0.2
    for i, (w, h) in enumerate([(0.07, 0.28), (0.06, 0.25), (0.09, 0.3), (0.05, 0.22), (0.08, 0.27)]):
        p.box((x + w / 2, 0, h / 2), (w, 0.2, h), f"Livro{i % 4 + 1}", "root", rot_y=6 if i == 3 else 0)
        x += w + 0.01
    return p.to_static(col)


def build_teia_vao(col):
    """Teia de vão (1,0 × 2,0 m, no plano XZ, origem embaixo no meio): fecha uma porta pouco usada.
    Quem passa rasga; o Imitador flutua e atravessa sem rasgar (a pista). Escala em X para portas
    de 1,2 e 1,6 m."""
    p = bc.Part("SM_PRP_TeiaVao", MATS)
    cx, cz = 0.0, 1.45                            # o centro da teia fica alto, perto da verga
    for i in range(10):
        a = math.radians(i * 36 + 8)
        ex, ez = cx + math.cos(a) * 0.55, cz + math.sin(a) * 0.62
        ez = max(0.6, min(2.0, ez))
        length = math.hypot(ex - cx, ez - cz)
        ang = math.degrees(math.atan2(-(ez - cz), ex - cx))
        p.box(((cx + ex) / 2, 0, (cz + ez) / 2), (length, 0.003, 0.003), "Teia", "root", rot_y=ang)
    for r in (0.12, 0.24, 0.36, 0.48):
        for i in range(10):
            a0, a1 = math.radians(i * 36 + 8), math.radians((i + 1) * 36 + 8)
            x0, z0 = cx + math.cos(a0) * r, cz + math.sin(a0) * r * 1.1
            x1, z1 = cx + math.cos(a1) * r * 0.94, cz + math.sin(a1) * r * 1.04
            seg = math.hypot(x1 - x0, z1 - z0)
            ang = math.degrees(math.atan2(-(z1 - z0), x1 - x0))
            p.box(((x0 + x1) / 2, 0, (z0 + z1) / 2), (seg, 0.002, 0.002), "Teia", "root", rot_y=ang)
    # fios presos aos batentes e à verga
    for x0, z0 in [(-0.5, 2.0), (0.5, 2.0), (-0.5, 1.0), (0.5, 1.1)]:
        length = math.hypot(x0 - cx, z0 - cz)
        ang = math.degrees(math.atan2(-(z0 - cz), x0 - cx))
        p.box(((cx + x0) / 2, 0, (cz + z0) / 2), (length, 0.002, 0.002), "Teia", "root", rot_y=ang)
    return p.to_static(col)


# (nome, construtor, altura, enquadramento das prévias, meio do objeto em Y)
PROPS = [
    ("Cristaleira", build_cristaleira, 1.8, 2.0, 0.0),
    ("TelefoneParede", build_telefone, 0.6, 0.75, 0.0),
    ("CadeiraBalanco", build_cadeira_balanco, 1.08, 1.3, 0.0),
    ("RolosPianola", build_rolos, 0.4, 0.6, 0.0),
    ("Cofre", build_cofre, 0.9, 1.1, 0.0),
    ("MaquinaEscrever", build_maquina_escrever, 0.33, 0.5, 0.0),
    ("LanternaMagica", build_lanterna_magica, 0.4, 0.55, 0.0),
    ("Psiche", build_psiche, 1.55, 1.8, 0.0),
    ("MaquinaCostura", build_maquina_costura, 1.06, 1.2, 0.0),
    ("Biombo", build_biombo, 1.7, 1.9, 0.0),
    ("Talha", build_talha, 0.66, 0.8, 0.0),
    ("Lampiao", build_lampiao, 0.42, 0.55, 0.0),
    ("FogaoLenha", build_fogao, 2.0, 2.2, 0.0),
    ("Bancada", build_bancada, 0.9, 1.7, 0.0),
    ("PratosEmpilhados", build_pratos, 0.2, 0.35, 0.0),
    ("Aparador", build_aparador, 0.85, 1.5, 0.0),
    ("GavetaAparador", build_gaveta, 0.16, 0.5, 0.0),
    ("LivrosSoltos", build_livros_soltos, 0.3, 0.45, 0.0),
    ("TeiaVao", build_teia_vao, 2.0, 2.2, 0.0),
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
