"""Texturas de DETALHE que se repetem (veio de madeira, trama de tecido, papel de parede...).

Por que existem: os modelos são de cor lisa, e cor lisa sob a lanterna parece massinha. O Unity
põe estas texturas por cima da cor de cada material (o "Detail" do URP Lit, ver
Assets/Editor/ArtImportSettings.cs), com o UV em metros que o blockout_common grava em todo
móvel. Assim a cor continua a do Art Bible; a textura só dá o grão.

Tudo é gerado por ruído periódico (FFT), então a textura emenda nas bordas sem costura.
Cada família gera duas imagens em Assets/Art/Textures/Detail/:
    T_Detail_<Nome>.png     cinza em volta de 128 (128 = não muda a cor; mais claro/escuro = grão)
    T_Detail_<Nome>_N.png   mapa de normal (relevo)

Uso: blender --background --factory-startup --python tools/blender/build_detail_textures.py
"""

from pathlib import Path

import bpy
import numpy as np

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "MudancaAssombrada/Assets/Art/Textures/Detail"
SIZE = 512


# ---------------------------------------------------------------- ruído

def noise(beta, seed, stretch=(1.0, 1.0)):
    """Ruído periódico 0..1: espectro 1/f^beta (beta alto = manchas grandes e suaves).
    stretch alonga num eixo (veio de madeira, riscos de metal): (u, v)."""
    rng = np.random.default_rng(seed)
    f = np.fft.fftfreq(SIZE)
    fu, fv = np.meshgrid(f * stretch[0], f * stretch[1])
    r = np.sqrt(fu ** 2 + fv ** 2)
    r[0, 0] = 1.0
    spectrum = (rng.normal(size=(SIZE, SIZE)) + 1j * rng.normal(size=(SIZE, SIZE))) / r ** beta
    spectrum[0, 0] = 0
    n = np.real(np.fft.ifft2(spectrum))
    return (n - n.min()) / (n.max() - n.min())


def grid():
    t = np.arange(SIZE) / SIZE
    return np.meshgrid(t, t)  # u, v em 0..1 (uma volta = a textura inteira)


# ---------------------------------------------------------------- famílias
# Cada uma devolve (altura 0..1 para o relevo, cor 0..1 em volta de 0.5, força do relevo).

def wood():
    u, v = grid()
    warp = noise(2.2, 1)
    # veio: faixas ao longo de U, onduladas pelo ruído grande; fibras finas esticadas em U
    # faixas de largura desigual (o veio não é um código de barras): o seno passa por uma curva
    # Fino e fraco: o veio forte e largo parecia curva de nível de mapa, não madeira (foto da Mansão).
    rings = (0.5 + 0.5 * np.sin(2 * np.pi * (v * 22 + warp * 4.0))) ** 2.5
    fibers = noise(1.1, 2, stretch=(0.05, 1.0))
    h = 0.35 * rings + 0.65 * fibers
    color = 0.5 + (h - h.mean()) * 0.2 + (noise(2.5, 3) - 0.5) * 0.12
    return h, color, 2.0


def fabric():
    u, v = grid()
    cells = 96  # fios por volta
    cu, cv = np.floor(u * cells), np.floor(v * cells)
    over = ((cu + cv) % 2) == 0  # trama: o fio passa por cima, depois por baixo
    thread_u = np.abs(np.sin(np.pi * u * cells * 2))
    thread_v = np.abs(np.sin(np.pi * v * cells * 2))
    h = np.where(over, thread_u, thread_v) * 0.7 + noise(1.0, 4) * 0.3
    wear = noise(2.4, 5)  # manchas de uso (mais claras/escuras), sem relevo
    color = 0.5 + (h - h.mean()) * 0.25 + (wear - 0.5) * 0.3
    return h, color, 1.5


def wallpaper():
    u, v = grid()
    # um losango de damasco: duas faixas diagonais finas e um ornamento no meio de cada célula
    # linhas finas nas duas diagonais (o losango) e, no meio de cada um, uma flor de 4 pétalas
    a = np.sin(np.pi * (4 * u + 4 * v))
    b = np.sin(np.pi * (4 * u - 4 * v))
    lattice = np.maximum(np.exp(-(a / 0.08) ** 2), np.exp(-(b / 0.08) ** 2))
    du, dv = (u * 4 % 1) - 0.5, (v * 4 % 1) - 0.5
    r, ang = np.hypot(du, dv), np.arctan2(dv, du)
    petals = 0.16 + 0.07 * np.cos(4 * ang)
    ornament = np.exp(-((r - petals) / 0.02) ** 2) + np.exp(-(r / 0.035) ** 2)
    motif = np.clip(np.maximum(lattice, ornament), 0, 1)
    stains = noise(2.6, 6)  # infiltração: manchas grandes
    fiber = noise(0.9, 7)
    h = motif * 0.6 + fiber * 0.4
    color = 0.5 + motif * 0.12 - (stains - 0.5) * 0.35 + (fiber - 0.5) * 0.06
    return h, color, 0.8


def metal():
    scratches = noise(1.2, 8, stretch=(0.03, 1.0))
    scratches = np.clip((scratches - 0.55) * 3, 0, 1)
    tarnish = noise(2.3, 9)  # escurecido de oxidação
    pits = noise(0.7, 10)
    h = scratches * 0.5 + pits * 0.5
    color = 0.5 + scratches * 0.18 - (tarnish - 0.5) * 0.4
    return h, color, 0.6


def plaster():
    h = noise(1.7, 11) * 0.6 + noise(0.8, 12) * 0.4
    color = 0.5 + (h - h.mean()) * 0.35 + (noise(2.6, 13) - 0.5) * 0.3
    return h, color, 2.5


def tile():
    u, v = grid()
    veins = np.abs(np.sin(2 * np.pi * (u * 2 + noise(2.0, 14) * 3))) ** 10  # veios de mármore
    h = noise(1.0, 15) * 0.5
    color = 0.5 - veins * 0.2 + (noise(2.4, 16) - 0.5) * 0.15
    return h, color, 0.4


def paper():
    h = noise(0.9, 17)
    color = 0.5 + (h - h.mean()) * 0.12 + (noise(2.5, 18) - 0.5) * 0.15
    return h, color, 0.5


def cardboard():
    u, v = grid()
    flutes = 0.5 + 0.5 * np.sin(2 * np.pi * v * 48)  # as ondas do papelão, aparecendo de leve
    h = flutes * 0.4 + noise(1.0, 19) * 0.6
    color = 0.5 + (h - h.mean()) * 0.2 + (noise(2.4, 20) - 0.5) * 0.25
    return h, color, 1.0


FAMILIES = {
    "Madeira": wood,
    "Tecido": fabric,
    "PapelParede": wallpaper,
    "Metal": metal,
    "Reboco": plaster,
    "Piso": tile,
    "Papel": paper,
    "Papelao": cardboard,
}


# ---------------------------------------------------------------- saída

def normal_map(h, strength):
    """Normal a partir da altura, com vizinhos que dão a volta (emenda sem costura)."""
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * strength * SIZE / 64
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * strength * SIZE / 64
    n = np.dstack((-dx, -dy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def save(name, rgb):
    img = bpy.data.images.new(name, SIZE, SIZE, alpha=False)
    rgba = np.dstack((rgb, np.ones((SIZE, SIZE))))
    img.pixels.foreach_set(rgba.astype(np.float32).ravel())
    img.filepath_raw = str(OUT / f"{name}.png")
    img.file_format = "PNG"
    img.save()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, build in FAMILIES.items():
        h, color, strength = build()
        # média em 0,5 exato: em média a textura não clareia nem escurece a cor do Art Bible
        gray = np.clip(color - color.mean() + 0.5, 0, 1)
        save(f"T_Detail_{name}", np.dstack((gray, gray, gray)))
        save(f"T_Detail_{name}_N", normal_map(h, strength))
        print("SAVED", name, "media da cor", round(float(gray.mean()), 3))


if __name__ == "__main__":
    main()
