"""Dona Cida DETALHADA: o modelo que vai para o jogo (SK_CHR_Cida.fbx).

Usa o mesmo rig, as mesmas medidas, as mesmas mãos e a mesma pega da bengala do blockout
(build_cida_blockout.py) e troca as formas "de bloco" por peças com volume. Vem por etapas:

  1. Cabelo: permanente de vó: tufos redondos com a mecha enrolada em relevo por cima,
     coque enrolado e o diapasão bem à vista, espetado nele como um grampo.
  2. Roupa: cardigã com carcela, botão trocado, barra e punhos canelados, remendo no cotovelo; golinha da blusa; colete
     justo com o método do colete do Tiago (cava, alça, vivo, zíper), crachá com o nome escrito e
     braile, chave de afinar; saia com pregas e barra.
  3. Acessórios: sapatilha de tricô com tira bordada, pochete de couro no cinto e bengala dobrável.
  4. Rosto: óculos refeitos (aro, lente abaulada com reflexo, ponte, plaquetas, haste e cordinha de pérolas), brinco de pérola em gota, sobrancelhas teimosas e ruguinhas de riso.

Rodar DEPOIS do build_cida_blockout.py (o build_all.ps1 já faz nessa ordem): o blockout também
grava o SK_CHR_Cida.fbx, e este script grava por cima. O .blend do blockout continua servindo de
base (rig e medidas) e para a comparação lado a lado.
    blender --background --factory-startup --python tools/blender/build_cida_detalhado.py

Saídas: MudancaAssombrada/Assets/Art/Characters/Cida/SK_CHR_Cida.fbx,
Art/Source/Characters/Cida/SK_CHR_Cida_Detalhado.blend e
Art/Source/Characters/Cida/previews/detalhado/ (corpo inteiro, closes e lado a lado).
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import blockout_common as bc  # noqa: E402
import build_cida_blockout as cb  # noqa: E402

d, HEAD_C, HEAD_R = cb.d, cb.HEAD_C, cb.HEAD_R

# As peças do blockout que continuam (corpo, óculos, bengala) usam a tabela dele: as cores novas
# entram nela, assim tudo sai com os mesmos nomes de material.
MATS = cb.MATS
MATS.table.update({
    "CabeloEscuro": ("#9C95A6", 0.0, 0.85, 0.0),   # o fundo entre os tufos
    "CabeloClaro": ("#E6E2E8", 0.0, 0.6, 0.0),     # cachos mais brancos, misturados
    "CardigaEscuro": ("#A97F30", 0.0, 0.9, 0.0),   # carcela, barra e punhos canelados
    "BotaoTrocado": ("#7A3F2E", 0.0, 0.5, 0.0),    # o botão que caiu e ela repôs com outro
    "Gola": ("#EFE7D8", 0.0, 0.8, 0.0),            # golinha da blusa
    "ColeteEscuro": ("#33484C", 0.0, 0.8, 0.0),    # o vivo, igual ao do Tiago
    "Ziper": ("#5E6166", 0.8, 0.4, 0.0),
    "Costura": ("#E8DFCB", 0.0, 0.7, 0.0),         # linha clara do pesponto e dos pontos do remendo
    "Plastico": ("#C9C4B6", 0.0, 0.35, 0.0),       # suporte do crachá
    "Tinta": ("#23345C", 0.0, 0.6, 0.0),           # caneta azul do crachá
    "Madeira": ("#8A5A32", 0.0, 0.6, 0.0),         # cabo da chave de afinar
    "Aco": ("#9AA0A6", 1.0, 0.35, 0.0),
    "Remendo": ("#7D5C82", 0.0, 0.9, 0.0),         # sobra do pano da saia, um tom mais claro
    "Sola": ("#2B2622", 0.0, 0.75, 0.0),           # borracha da sapatilha
    "SapatoMalha": ("#232226", 0.0, 0.9, 0.0),     # tricô preto
    "SapatoDebrum": ("#141316", 0.0, 0.7, 0.0),    # debrum da boca e tira elástica
    "Forro": ("#6B1F26", 0.0, 0.8, 0.0),           # forro vinho
    "Losango": ("#B9B4AA", 0.0, 0.8, 0.0),         # bordado da tira
    "Couro": ("#6A4429", 0.0, 0.55, 0.0),          # couro velho da pochete
    "CouroEscuro": ("#4B2F1D", 0.0, 0.55, 0.0),
    "Feltro": ("#A5322E", 0.0, 1.0, 0.0),          # feltro de afinador
    "Cordao": ("#B23A34", 0.0, 0.8, 0.0),          # cordinha de pulso da bengala
    "LenteBrilho": ("#AEB4C6", 0.0, 0.2, 0.15),    # reflexo desenhado nas lentes
    "Plaqueta": ("#E7DFC9", 0.0, 0.3, 0.0),        # apoios de nariz
    "Ruga": ("#BE8E6C", 0.0, 0.6, 0.0),            # ruguinhas: a pele um pouco mais escura
    "SaiaBarra": ("#553A58", 0.0, 0.85, 0.0),      # a barra mais escura
    "SaiaDentro": ("#2D2230", 0.0, 0.9, 0.0),
})

OUT = bc.SOURCE_ROOT / "Characters" / "Cida"


def sweep(p, pts, radii, mat, bone, seg=6):
    """Tubo contínuo ao longo dos pontos: um anel em cada ponto, sem torcer de um para o outro
    (mesma ideia do sweep do Tiago). Caminho aberto ganha tampa na ponta final, que fica à vista;
    caminho fechado (primeiro ponto = último, como a cava do colete) emenda nele mesmo."""
    if len(pts) < 2:
        return
    closed = (pts[0] - pts[-1]).length < 1e-6
    if closed:
        pts, radii = pts[:-1], radii[:-1]
    bm, rings, normal = p.bm, [], None
    n = len(pts)
    for i, pt in enumerate(pts):
        a = pts[(i - 1) % n] if closed else pts[max(i - 1, 0)]
        b = pts[(i + 1) % n] if closed else pts[min(i + 1, n - 1)]
        t = (b - a).normalized()
        normal = t.orthogonal().normalized() if normal is None else (normal - t * normal.dot(t)).normalized()
        binormal = t.cross(normal)
        rings.append([bm.verts.new(bc.scaled(pt + (normal * math.cos(k * math.tau / seg)
                                                   + binormal * math.sin(k * math.tau / seg)) * radii[i]))
                      for k in range(seg)])
    for r0, r1 in list(zip(rings, rings[1:])) + ([(rings[-1], rings[0])] if closed else []):
        for k in range(seg):
            bm.faces.new((r0[k], r0[(k + 1) % seg], r1[(k + 1) % seg], r1[k]))
    if not closed:
        bm.faces.new(rings[-1])
    p._finish([v for ring in rings for v in ring], mat, bone, True)


def frame_of(n):
    """Dois eixos deitados na superfície cuja normal é n."""
    u = n.orthogonal().normalized()
    return u, n.cross(u).normalized()


def curl(p, center, normal, radius, ball_mat, line_mat, bone="Head", turns=1.7, tube=0.0085, roll=0.0,
         n=12, seg=(8, 5), squash=1.0):
    """Um cacho: tufo redondo com a mecha enrolada em relevo por cima. A mecha corre colada na
    superfície do tufo, do meio da encosta até o miolo, meio enterrada (por isso basta uma seção de
    4 lados). Ela não desce até a borda: ali sobraria para fora do tufo, como uma aba de arame.
    Os tufos dão a silhueta fofa da ficha; o relevo diz "permanente" e custa pouco."""
    center, normal = Vector(center), Vector(normal).normalized()
    u, v = frame_of(normal)
    p.ellipsoid(center, (radius, radius, radius * squash), ball_mat, bone, seg=seg[0], rings=seg[1])
    pts, radii = [], []
    for i in range(n):
        t = i / (n - 1)
        fi = math.radians(58) * (1 - t) ** 0.85 + math.radians(4)
        a = roll + turns * math.tau * t
        side = u * math.cos(a) + v * math.sin(a)
        pts.append(center + (normal * math.cos(fi) + side * math.sin(fi)) * (radius * 0.985))
        radii.append(tube * (0.45 + 0.75 * math.sin(math.pi * t)) * (1 - 0.5 * t ** 3))
    sweep(p, pts, radii, line_mat, bone, seg=4)


def shade(i):
    """Mistura os tons: a maioria no cinza do cabelo, um a cada quatro mais branco."""
    return ("CabeloClaro", "Cabelo") if i % 4 == 1 else ("Cabelo", "CabeloClaro")


def build_hair(arm, col):
    """Permanente: base escura no fundo, tufos enrolados por cima e coque com o diapasão.
    A silhueta é a da ficha (os mesmos círculos de frente do blockout) e os tufos ficam no mesmo
    envelope das bolinhas antigas, então nada novo entra na frente da câmera dela."""
    p = bc.Part("SK_CHR_Cida_Hair", MATS)

    def on_face(q):
        # q: ponto na esfera unitária. Fora do rosto, da orelha e da parte de baixo (nuca fica).
        nape = q.y > 0.35 and q.z > -0.6
        return (q.z < -0.25 and not nape) or (q.y < -0.25 and q.z < 0.62) or (q.y < 0.1 and q.z < 0.05)

    # Base: calota maior que a cabeça, sem o rosto. Fica no fundo e tapa os vãos entre os tufos.
    faces = p.ellipsoid(HEAD_C, HEAD_R * 1.05, "CabeloEscuro", "Head", seg=16, rings=10)
    kill = []
    for f in faces:
        c = f.calc_center_median() / bc.K - HEAD_C
        q = Vector((c.x / HEAD_R.x, c.y / HEAD_R.y, c.z / HEAD_R.z)).normalized()
        if on_face(q) or (q.y < 0.35 and q.z < 0.2):   # (a lateral fica sem base: a borda dela fazia degrau)
            kill.append(f)
    bmesh.ops.delete(p.bm, geom=kill, context="FACES")

    # Tufos grandes da silhueta, nos círculos da vista de frente da ficha.
    front = [(72, 106, 26, -0.03), (92, 82, 28, -0.06), (120, 74, 30, -0.05), (148, 82, 28, -0.06),
             (168, 106, 26, -0.03), (62, 130, 14, 0.01), (178, 130, 14, 0.01)]
    for i, (x, y, r, depth) in enumerate(front):
        ex, ez = d(x, y)
        pos = Vector((ex, depth, ez))
        out = (pos - HEAD_C).normalized() + Vector((0, -0.9, 0))
        curl(p, pos, out, r / 300, *shade(i), roll=i * 1.3, tube=0.0065 + r * 0.00014, turns=1.6, n=16, seg=(12, 7))

    # As orelhas e os brincos ficam à mostra dos dois lados.
    ears = [Vector((d(120 + 62 * s, 138)[0], 0.0, d(120 + 62 * s, 138)[1])) for s in (1, -1)]

    # Cobertura: pontos de Fibonacci na calota, pulando a região do rosto.
    total = 74
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(total):
        z = 1 - 2 * (i + 0.5) / total
        rad = math.sqrt(1 - z * z)
        q = Vector((math.cos(golden * i) * rad, math.sin(golden * i) * rad, z))
        if on_face(q):
            continue
        pos = HEAD_C + Vector((q.x * HEAD_R.x, q.y * HEAD_R.y, q.z * HEAD_R.z)) * 0.95
        size = 0.064 + 0.007 * math.sin(i * 2.4)
        # (o segundo teste tira o tufo que sobrava embaixo da orelha, encostado no queixo)
        if min((pos - ear).length for ear in ears) < size + 0.05 or (q.y < 0.75 and q.z < -0.05):
            continue
        curl(p, pos, q, size, *shade(i), roll=i * 2.1, turns=1.2 + 0.2 * math.sin(i * 1.7), n=11)

    # Tufos atrás das duas orelhas, iguais dos dois lados: a distribuição deixava um vão ali.
    for k, (x, y, z, size) in enumerate(((0.178, 0.078, 1.058, 0.052), (0.152, 0.102, 0.992, 0.056),
                                         (0.105, 0.152, 0.985, 0.056))):
        for s in (1, -1):
            pos = Vector((x * s, y, z))
            curl(p, pos, pos - HEAD_C, size, *shade(k * 2 + (s > 0)), roll=k * 2.1 + s, turns=1.3, n=11)

    # Coque: tufo maior com a mecha dando voltas e um tufinho encostado, igual ao da ficha.
    bx, bz = d(120, 46)
    bun = Vector((bx, 0.05, bz))
    curl(p, bun, (0.1, 0.2, 1), 0.078, "Cabelo", "CabeloClaro", "bun_jiggle", turns=2.2, tube=0.0105,
         n=24, seg=(18, 9), squash=0.95)
    curl(p, (-0.03, 0.035, bz + 0.002), (-0.8, 0, 0.5), 0.04, "CabeloClaro", "Cabelo", "bun_jiggle", turns=1.4,
         tube=0.0065, n=10)

    # Diapasão: o mesmo desenho da ficha (duas hastes, a base e o cabo), mais grosso para ser lido
    # de longe. É a marca da Cida: tira daqui para bater (tecla Q).
    fork = [((134, 52), (148, 20)), ((144, 56), (158, 24)), ((134, 52), (140, 58)),
            ((140, 58), (144, 56)), ((139, 58), (133, 72))]
    for (x0, y0), (x1, y1) in fork:
        a, b = d(x0, y0), d(x1, y1)
        p.tube((a[0], 0.055, a[1]), (b[0], 0.055, b[1]), 0.0068, 0.0068, "Metal", "bun_jiggle", seg=6)
    return p.to_object(col, arm)


# ---------------------------------------------------------------- roupa

Z_HEM, Z_TOP = d(0, 310)[1], d(0, 212)[1]      # o tronco do cardigã
ZS = d(0, 272)[1]                              # altura da faixa refletiva
SQUASH = 0.64                                  # o tronco é uma elipse: Y = 0,64 de X

# Colete moldado no cardigã, com o mesmo método do colete do Tiago: cada altura é a elipse do tronco
# afastada VEST_GAP para fora (0,8 cm: bem justo), frente aberta, cava em volta do braço e alça por
# cima do ombro. As linhas da grade caem nas bordas da faixa refletiva, pintada na própria malha.
VEST_GAP = 0.008
VEST_OPEN = 14
VEST_ROWS = [0.565, 0.60, ZS - 0.015, ZS + 0.015, 0.70, 0.745, 0.79, Z_TOP + 0.002]


def body_r(z):
    """Raio do tubo do cardigã em z (mais largo no quadril: a Cida é o círculo da equipe)."""
    return 0.187 - 0.02 * (z - Z_HEM) / (Z_TOP - Z_HEM)


def vest_a(z):
    return body_r(z) + VEST_GAP


def vest_b(z):
    return SQUASH * body_r(z) + VEST_GAP


def vest_y(x, z):
    """Y da frente do colete em (x, z)."""
    return -vest_b(z) * math.sqrt(max(1.0 - (x / vest_a(z)) ** 2, 0.0))


def shoulder_z(x, y):
    """Altura do alto do cardigã em (x, y): a tampa do tubo do tronco ou o domo dos ombros."""
    top = Z_TOP if (x / 0.167) ** 2 + (y / 0.107) ** 2 < 1 else 0.0
    u = 1 - (x / 0.17) ** 2 - (y / 0.11) ** 2
    dome = Z_TOP - 0.02 + 0.06 * math.sqrt(u) if u > 0 else 0.0
    return max(top, dome)


def near_arm(pt, clearance=0.06):
    """Perto do braço (raio da manga 0,045 + folga)? Define a cava do colete. Os dois braços têm
    medidas um pouco diferentes: a cava é uma só, espelhada, e respeita os dois."""
    pt = Vector(pt)
    for side in cb.ARMS.values():
        a, b = Vector(side[1]), Vector(side[2])
        a.x, b.x = abs(a.x), abs(b.x)
        t = max(0.0, min(1.0, (pt - a).dot(b - a) / (b - a).length_squared))
        if (pt - (a + (b - a) * t)).length < clearance:
            return True
    return False


def armhole_angle(z):
    """Ângulo (graus, <= 0) da borda da frente da cava em z, no lado +X: anda da lateral para a
    frente até sair de perto do braço. 0 = abaixo da axila (o colete fecha na lateral)."""
    a, b = vest_a(z), vest_b(z)
    for deg in range(0, -76, -1):
        t = math.radians(deg)
        if not near_arm((a * math.cos(t), b * math.sin(t), z)):
            return float(deg)
    return -75.0


def boundary_chains(edges, label=None):
    """Junta arestas de borda em cadeias de pontos, fechadas (cava) ou abertas. label(p0, p1) divide
    a cadeia onde o rótulo muda (zíper x vivo). Devolve (pontos, rótulo). Igual à do Tiago: o script
    dele não pode ser importado aqui, porque acerta a escala global para a altura dele."""
    nbr = {}
    for e in edges:
        a, b = e.verts
        nbr.setdefault(a, []).append(b)
        nbr.setdefault(b, []).append(a)
    seen, chains = set(), []
    for start in sorted(nbr, key=lambda v: len(nbr[v])):  # pontas (1 vizinho) primeiro
        if start in seen:
            continue
        chain, prev, cur = [start], None, start
        seen.add(start)
        while True:
            nxt = [v for v in nbr[cur] if v is not prev and (v not in seen or (v is start and len(chain) > 2))]
            if not nxt:
                break
            prev, cur = cur, nxt[0]
            chain.append(cur)
            if cur is start:
                break
            seen.add(cur)
        pts = [v.co / bc.K for v in chain]
        run, run_label = [pts[0]], label(pts[0], pts[1])
        for a, b in zip(pts, pts[1:]):
            lab = label(a, b)
            if lab != run_label:
                chains.append((run, run_label))
                run, run_label = [a], lab
            run.append(b)
        chains.append((run, run_label))
    return chains


def build_vest(p, n=5):
    """Colete justo: uma grade que segue a elipse do tronco (alturas VEST_ROWS), aberta na frente, e
    mais três linhas que sobem pelo ombro e se fecham em Y = 0 (a alça). As colunas de cada linha são
    distribuídas dentro de cada painel (frente, costas, meio das costas e o espelho), então a cava e
    o decote seguem a curva, sem escadinha. Nas bordas, um vivo escuro dá espessura ao tecido."""
    bm = p.bm
    z0 = VEST_ROWS[-1]
    a0, b0 = vest_a(z0), vest_b(z0)

    def front(m, z):
        """Coluna m (0 = abertura, n = cava) do painel da frente +X numa linha do corpo."""
        t = math.radians(-90 + VEST_OPEN + (armhole_angle(z) + 90 - VEST_OPEN) * m / n)
        return vest_a(z) * math.cos(t), vest_b(z) * math.sin(t)

    def strap(m, k):
        """Coluna m na linha k (1..3) sobre o ombro: o painel afinou até a alça."""
        x = 0.078 + (0.122 - 0.078) * m / n
        y = -b0 * math.sqrt(max(1 - (x / a0) ** 2, 0.0)) * (1 - k / 3)
        return x, y

    rows = []
    for k, z in enumerate(VEST_ROWS + [None] * 3):
        over = k - len(VEST_ROWS) + 1 if z is None else 0
        pts = [strap(m, over) if over else front(m, z) for m in range(n + 1)]
        zs_ = [max(shoulder_z(x, y) + VEST_GAP, z0 + 0.01 * over) for x, y in pts] if over else [z] * len(pts)
        f_ = [(x, y, zz) for (x, y), zz in zip(pts, zs_)]                 # frente +X (abertura -> cava)
        b_ = [(x, -y, zz) for x, y, zz in reversed(f_)]                   # costas +X (cava -> meio)
        mid = (0.0, vest_b(z0 if over else z), z0 if over else z)         # meio das costas
        rows.append(f_ + b_ + [mid] + [(-x, y, zz) for x, y, zz in reversed(b_)]
                    + [(-x, y, zz) for x, y, zz in reversed(f_)])
    gaps = {n, 3 * n + 3}             # a cava, entre frente e costas, dos dois lados
    middle = {2 * n + 1, 2 * n + 2}   # o meio das costas, que não sobe pelo ombro

    verts = [[bm.verts.new(bc.scaled(pt)) for pt in row] for row in rows]
    faces = []
    for i in range(len(rows) - 1):
        for j in range(len(rows[i]) - 1):
            if j in gaps or (i >= len(VEST_ROWS) - 1 and j in middle):
                continue
            quad = (rows[i][j], rows[i][j + 1], rows[i + 1][j + 1], rows[i + 1][j])
            c = tuple(sum(q[c_] for q in quad) / 4 for c_ in range(3))
            faces.append((bm.faces.new((verts[i][j], verts[i][j + 1], verts[i + 1][j + 1], verts[i + 1][j])), c))
    used = {v for f, _ in faces for v in f.verts}
    bmesh.ops.delete(bm, geom=[v for row in verts for v in row if v not in used], context="VERTS")
    bmesh.ops.remove_doubles(bm, verts=list(used), dist=0.002 * bc.K)   # emenda a cava e a alça

    fs = [f for f, _ in faces if f.is_valid]
    p._finish(list({v for f in fs for v in f.verts}), "Colete", "Chest", True)
    faixa = p._index(p.mats, "Faixa")
    for f, (cx, cy, cz) in faces:
        # faixa refletiva e o retângulo da transportadora nas costas
        if f.is_valid and (ZS - 0.015 < cz < ZS + 0.015 or (cy > 0 and abs(cx) < 0.095 and 0.70 < cz < 0.79)):
            f.material_index = faixa

    def zipper(p0, p1):
        mx, my, mz = (p0 + p1) / 2
        return my < 0 and mz < z0 - 0.001 and abs(mx) < vest_a(mz) * math.sin(math.radians(VEST_OPEN)) + 0.004

    edges = [e for f in fs for e in f.edges if len(e.link_faces) == 1]
    for chain, is_zip in boundary_chains(edges, zipper):
        sweep(p, chain, [0.004] * len(chain), "Ziper" if is_zip else "ColeteEscuro", "Chest", seg=4)
    # puxador do zíper, pendurado embaixo do lado esquerdo
    x = vest_a(0.60) * math.sin(math.radians(VEST_OPEN))
    p.box((x, vest_y(x, 0.60) - 0.006, 0.583), (0.009, 0.004, 0.024), "Metal", "Chest")


def patch(x0, x1, z):
    """Plano de uma peça chapada no colete: a corda da curva entre x0 e x1 (origem e ângulo em Z)."""
    y0, y1 = vest_y(x0, z), vest_y(x1, z)
    return Vector(((x0 + x1) / 2, (y0 + y1) / 2, z)), math.atan2(y1 - y0, x1 - x0)


def on_patch(frame, lx, ly, lz):
    o, ang = frame
    c, s = math.cos(ang), math.sin(ang)
    return (o.x + lx * c - ly * s, o.y + lx * s + ly * c, o.z + lz)


def build_vest_details(p):
    """Dois bolsos com aba e botão abaixo da faixa, crachá, chave de afinar e pesponto."""
    for x0, x1 in ((0.075, 0.145), (-0.145, -0.075)):
        fr = patch(x0, x1, 0.598)
        deg = math.degrees(fr[1])
        p.box(on_patch(fr, 0, -0.002, -0.004), (0.07, 0.007, 0.05), "Colete", "Spine", rot_z=deg)
        p.box(on_patch(fr, 0, -0.0065, 0.02), (0.075, 0.005, 0.021), "ColeteEscuro", "Spine", rot_z=deg)
        p.ellipsoid(on_patch(fr, 0, -0.0095, 0.016), (0.006, 0.003, 0.006), "Metal", "Spine", seg=8, rings=4,
                    rot=(0, 0, deg))
    build_badge(p)
    build_tuning_key(p)

    # pesponto claro nas duas bordas da abertura (pula a faixa refletiva)
    for s in (1, -1):
        z = 0.58
        while z < Z_TOP - 0.01:
            if abs(z - ZS) > 0.024:
                x = s * (vest_a(z) * math.sin(math.radians(VEST_OPEN)) + 0.0095)
                p.tube((x, vest_y(x, z) - 0.0012, z), (x, vest_y(x, z + 0.012) - 0.0012, z + 0.012),
                       0.0012, 0.0012, "Costura", "Chest", seg=3, caps=False)
            z += 0.022


def flat_stroke(p, frame, pts, width, mat, bone, lift):
    """Traço de caneta: tiras chatas (2 triângulos por trecho) deitadas numa peça chapada. pts em
    metros no plano da peça, (lx, lz); lift: quanto o traço fica à frente da origem da peça. Texto
    com geometria tem nitidez em qualquer distância e não pede textura só para o crachá."""
    origin = Vector(on_patch(frame, 0, 0, 0))
    out = (Vector(on_patch(frame, 0, -1, 0)) - origin).normalized()
    made = []
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        t = Vector((x1 - x0, z1 - z0)).normalized()
        nx, nz = -t.y * width / 2, t.x * width / 2
        ex, ez = t.x * width * 0.35, t.y * width * 0.35     # passa um pouco da ponta, para emendar as curvas
        quad = [(x0 - ex - nx, z0 - ez - nz), (x1 + ex - nx, z1 + ez - nz),
                (x1 + ex + nx, z1 + ez + nz), (x0 - ex + nx, z0 - ez + nz)]
        cos = [Vector(on_patch(frame, x, -lift, z)) for x, z in quad]
        if (cos[1] - cos[0]).cross(cos[2] - cos[0]).dot(out) < 0:
            cos.reverse()
        vs = [p.bm.verts.new(bc.scaled(c)) for c in cos]
        p.bm.faces.new(vs)
        made += vs
    p._finish(made, mat, bone, False)


def arc(cx, cz, rx, rz, deg0, deg1, n):
    return [(cx + rx * math.cos(math.radians(deg0 + (deg1 - deg0) * i / n)),
             cz + rz * math.sin(math.radians(deg0 + (deg1 - deg0) * i / n))) for i in range(n + 1)]


def build_badge(p):
    """Crachá no peito direito: suporte de plástico, presilha, o nome à caneta com letra corrida de
    quem preencheu com pressa, e CIDA em braile embaixo (pontos C, I, D, A)."""
    fr = patch(-0.12, -0.05, 0.765)
    deg = math.degrees(fr[1])
    p.box(on_patch(fr, 0, -0.0015, 0), (0.076, 0.0035, 0.039), "Plastico", "Chest", rot_z=deg)
    p.box(on_patch(fr, 0, -0.0025, 0), (0.07, 0.004, 0.033), "Etiqueta", "Chest", rot_z=deg)
    p.box(on_patch(fr, 0, -0.003, 0.022), (0.014, 0.005, 0.013), "Metal", "Chest", rot_z=deg)

    lift = 0.0048
    ink = lambda pts, w=0.0021: flat_stroke(p, fr, pts, w, "Tinta", "Chest", lift)
    ink(arc(-0.0185, 0.0045, 0.0058, 0.0088, 55, 305, 6))                                           # C
    ink([(-0.0078, -0.0042), (-0.0072, 0.0134)])                                                    # I
    ink([(0.0002, -0.0042), (0.0006, 0.0134)])                                                      # D
    ink([(0.0002, 0.0134)] + arc(0.0006, 0.0046, 0.0086, 0.0088, 85, -88, 6))
    ink([(0.0118, -0.0042), (0.0182, 0.0136), (0.0246, -0.0046)])                                   # A
    ink([(0.0140, 0.0015), (0.0226, 0.0019)])

    # braile: duas colunas de três pontos por letra (1-2-3 à esquerda, 4-5-6 à direita)
    cells = ((1, 4), (2, 4), (1, 4, 5), (1,))
    for c, dots in enumerate(cells):
        for dot in dots:
            col, row = (0, dot - 1) if dot <= 3 else (1, dot - 4)
            x, z = -0.0148 + c * 0.0098 + col * 0.0028, -0.0088 - row * 0.0026
            flat_stroke(p, fr, [(x - 0.0006, z), (x + 0.0006, z)], 0.0016, "Tinta", "Chest", lift)


def build_tuning_key(p):
    """Chave de afinar pianos presa no peito por duas alças: o cabo de madeira em T fica à vista. É
    o que ela trouxe do ofício antigo para a transportadora."""
    fr = patch(0.055, 0.12, 0.735)
    deg = math.degrees(fr[1])
    at = lambda lx, lz, ly=-0.007: on_patch(fr, lx, ly, lz)
    p.tube(at(0, -0.04), at(0, 0.03), 0.0034, 0.0034, "Aco", "Chest", seg=6, caps=False)
    p.tube(at(0, -0.052), at(0, -0.036), 0.0062, 0.0055, "Metal", "Chest", seg=6)
    p.tube(at(-0.03, 0.036), at(0.03, 0.036), 0.0072, 0.0072, "Madeira", "Chest", seg=8)
    for lz in (0.012, -0.022):
        p.box(on_patch(fr, 0, -0.0085, lz), (0.022, 0.008, 0.009), "Madeira", "Chest", rot_z=deg)


def build_elbow_patch(p):
    """Remendo oval no cotovelo esquerdo, o braço da bengala, com pontos feitos à mão por fora."""
    el = Vector(cb.ARMS["Left"][2])
    c = el + Vector((0.008, 0.036, 0.0))
    rx, rz = 0.03, 0.038
    p.ellipsoid(c, (rx, 0.012, rz), "Remendo", "LeftLowerArm", seg=10, rings=6)
    r_sleeve = 0.042
    for k in range(10):
        a = k * math.tau / 10 + 0.2
        ox, oz = math.cos(a) * 1.1 * rx, math.sin(a) * 1.1 * rz
        # o ponto termina colado na manga, que é redonda e foge para trás nas beiradas do remendo
        side = c.x - el.x + ox
        y_out = el.y + math.sqrt(max(r_sleeve ** 2 - side ** 2, 0.0)) + 0.001
        p.tube(c + Vector((ox * 0.8, 0.0075, oz * 0.8)), Vector((c.x + ox, y_out, c.z + oz)),
               0.0013, 0.0013, "Costura", "LeftLowerArm", seg=3, caps=False)


def ribbed_band(p, c0, c1, r0, r1, n, amp, mat, bone, squash=1.0):
    """Punho ou barra canelada: um anel com o raio alternando (2n colunas). Custa só 4n triângulos,
    bem menos que uma varetinha por canelura."""
    c0, c1 = Vector(c0), Vector(c1)
    axis = (c1 - c0).normalized()
    u = Vector((1, 0, 0)) if abs(axis.x) < 0.9 else Vector((0, 0, 1))
    u = (u - axis * u.dot(axis)).normalized()
    v = axis.cross(u)
    rings = []
    for c, r in ((c0, r0), (c1, r1)):
        ring = []
        for k in range(2 * n):
            a = k * math.pi / n
            rr = r + (amp if k % 2 else -amp)
            ring.append(p.bm.verts.new(bc.scaled(c + u * (rr * math.cos(a)) + v * (rr * math.sin(a) * squash))))
        rings.append(ring)
    for k in range(2 * n):
        m = (k + 1) % (2 * n)
        p.bm.faces.new((rings[0][k], rings[0][m], rings[1][m], rings[1][k]))
    p._finish(rings[0] + rings[1], mat, bone, False)


def build_cardigan(p):
    """Cardigã mostarda: tronco, carcela com botões (um trocado, costurado por ela mesma), barra e
    punhos canelados e a golinha da blusa por cima."""
    p.tube((0, 0, Z_HEM), (0, 0, Z_TOP), 0.187, 0.167, "Cardiga", "Chest", seg=24, squash_y=SQUASH)
    p.ellipsoid((0, 0, Z_TOP - 0.02), (0.17, 0.11, 0.06), "Cardiga", "UpperChest", seg=24, rings=8)
    ribbed_band(p, (0, 0, Z_HEM - 0.005), (0, 0, 0.566), 0.189, 0.187, 24, 0.0018, "CardigaEscuro", "Hips",
                squash=0.66)

    # carcela: faixa estreita costurada na frente, seguindo a curva do tronco
    zs = (Z_HEM + 0.04, 0.62, 0.70, 0.78, Z_TOP + 0.015)
    xs = (-0.02, 0.0, 0.02)
    grid = []
    for z in zs:
        a, b = body_r(z), SQUASH * body_r(z)
        grid.append([p.bm.verts.new(bc.scaled((x, -b * math.sqrt(max(1 - (x / a) ** 2, 0)) - 0.0025, z))) for x in xs])
    for r0, r1 in zip(grid, grid[1:]):
        for j in range(len(xs) - 1):
            p.bm.faces.new((r0[j], r0[j + 1], r1[j + 1], r1[j]))
    p._finish([v for row in grid for v in row], "CardigaEscuro", "Chest", True)
    for i, z in enumerate((0.8, 0.752, 0.704, 0.656, 0.608)):
        y = -SQUASH * body_r(z) - 0.0065
        p.ellipsoid((0, y, z), (0.0095, 0.0045, 0.0095), "BotaoTrocado" if i == 3 else "Metal", "Chest", seg=10, rings=4)

    # golinha redonda da blusa, em duas abas que abraçam o pescoço por cima dos ombros
    for s in (1, -1):
        cols = [math.radians(-84 + 34 * k) for k in range(8)]    # da frente, em volta, até as costas
        rows = []
        for t in (0.0, 0.55, 1.0):
            row = []
            for k, a in enumerate(cols):
                r = 0.047 + t * (0.04 + 0.003 * math.cos(k * 3.0))
                x, y = r * math.cos(a), r * 0.95 * math.sin(a) - 0.008
                row.append(p.bm.verts.new(bc.scaled((x * s, y, shoulder_z(x, y) + 0.0145 - t * 0.003))))
            rows.append(row)
        for r0, r1 in zip(rows, rows[1:]):
            for j in range(len(cols) - 1):
                quad = (r0[j], r0[j + 1], r1[j + 1], r1[j])
                p.bm.faces.new(quad if s < 0 else quad[::-1])
        p._finish([v for row in rows for v in row], "Gola", "UpperChest", True)


def build_skirt(p, pleats=14):
    """Saia em A com pregas de faca, cada uma em três colunas: fundo da prega, encosta e crista. As
    pregas nascem quase lisas na cintura e abrem até a barra, que tem um tom mais escuro."""
    top, bottom = d(0, 300)[1], d(0, 438)[1]
    ts = (0.0, 0.28, 0.56, 0.8, 0.915, 1.0)
    cols = pleats * 3
    rows = []
    for t in ts:
        z = top + (bottom - top) * t
        r = 0.14 + 0.05 * t ** 0.85
        amp = 0.005 + 0.024 * t
        row = []
        for j in range(cols):
            a = -math.pi / 2 + math.tau * j / cols
            rr = r + amp * (1.0, 0.25, -0.75)[j % 3]
            row.append(p.bm.verts.new(bc.scaled((rr * math.cos(a), rr * 0.78 * math.sin(a), z))))
        rows.append(row)
    rows.reverse()       # de baixo para cima, para a frente das faces ficar para fora
    faces = []
    for i in range(len(rows) - 1):
        for j in range(cols):
            m = (j + 1) % cols
            faces.append((p.bm.faces.new((rows[i][j], rows[i][m], rows[i + 1][m], rows[i + 1][j])), i))
    p._finish([v for row in rows for v in row], "Saia", "Hips", True)
    barra = p._index(p.mats, "SaiaBarra")
    for f, i in faces:
        if i == 0:
            f.material_index = barra
    # forro escuro por dentro da barra: quando o pano abre, quem olha de baixo vê o avesso da saia, e
    # não um buraco. (Uma tampa inteira rasgava: ela esticava de uma perna à outra.)
    lining = [[p.bm.verts.new(Vector((v.co.x * 0.992, v.co.y * 0.992, v.co.z))) for v in row] for row in rows[:3]]
    for r0, r1 in zip(lining, lining[1:]):
        for j in range(cols):
            m = (j + 1) % cols
            p.bm.faces.new((r1[j], r1[m], r0[m], r0[j]))
    p._finish([v for row in lining for v in row], "SaiaDentro", "Hips", True)

    # A saia acompanha as pernas: da cintura para baixo, o peso passa do quadril para a coxa do mesmo
    # lado e, perto da barra, um pouco para a canela (é ela que chuta para trás ao andar). No meio
    # da frente e das costas o peso se divide: o pano abre e fecha entre as duas pernas.
    groups = {name: p._index(p.groups, name) for name in
              ("Hips", "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg")}
    z_top, z_bottom = top * bc.K, bottom * bc.K
    for row in rows + lining:
        for v in row:
            t = max(0.0, min(1.0, (v.co.z - z_top) / (z_bottom - z_top)))
            # Até a altura do joelho, o pano segue a coxa; abaixo dele, a canela. Agachada, a saia
            # sobe até o joelho e cai com a perna, em vez de abrir como um guarda-chuva. Nos lados e atrás
            # o peso é menor, senão a saia abre demais ao correr: ali o pano fica mais com o quadril.
            length = math.hypot(v.co.x, v.co.y) or 1.0
            front = max(0.0, -v.co.y / length) ** 1.5
            back = max(0.0, v.co.y / length) ** 1.5
            knee = min(1.0, t / (0.5 - 0.05 * front))   # mais cedo que isso, o pano cobre a pochete ao andar
            below = min(1.0, max(0.0, (t - 0.42) / 0.2))
            legs = (knee * knee * (3 - 2 * knee)) * (0.68 + 0.32 * front - 0.23 * back)
            shin = below * below * (3 - 2 * below) * (0.55 + 0.45 * front)
            left = min(1.0, max(0.0, (v.co.x / bc.K + 0.1) / 0.2))
            left = left * left * (3 - 2 * left)
            weights = {"Hips": 1 - legs,
                       "LeftUpperLeg": legs * (1 - shin) * left, "RightUpperLeg": legs * (1 - shin) * (1 - left),
                       "LeftLowerLeg": legs * shin * left, "RightLowerLeg": legs * shin * (1 - left)}
            layer = v[p.deform]
            for name, w in weights.items():
                layer[groups[name]] = w


# ---------------------------------------------------------------- rosto

def smooth_path(pts, sub=3):
    """Caminho suavizado (Catmull-Rom): sub pontos por trecho, passando pelos pontos dados."""
    out = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = pts[max(i - 1, 0)], pts[i], pts[i + 1], pts[min(i + 2, len(pts) - 1)]
        for k in range(sub):
            t = k / sub
            out.append(0.5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (3 * p1 - p0 - 3 * p2 + p3) * t ** 3))
    return out + [pts[-1]]


def head_y(x, z, lift=0.0):
    """Y da frente da cabeça em (x, z), com lift metros para fora: o que é pintado fica colado nela."""
    u = 1 - (x / HEAD_R.x) ** 2 - ((z - HEAD_C.z) / HEAD_R.z) ** 2
    return HEAD_C.y - HEAD_R.y * math.sqrt(max(u, 0.0)) - lift


def build_face_marks(p):
    """Expressão: sobrancelhas ralas e teimosas (a esquerda dela bem mais alta, um "ah, é?" que não
    precisa de olhos) e ruguinhas de riso, finas e coladas no rosto, ao lado da boca e dos óculos."""
    brows = {1: ((0.042, 1.178), (0.07, 1.203), (0.1, 1.211), (0.133, 1.192)),
             -1: ((-0.042, 1.171), (-0.075, 1.184), (-0.105, 1.187), (-0.133, 1.17))}
    for pts in brows.values():
        path = smooth_path([Vector((x, head_y(x, z, 0.003), z)) for x, z in pts], 3)
        n = len(path)
        radii = [0.0055 + 0.0045 * math.sin(math.pi * min(i / (n - 1) * 1.25, 1.0)) for i in range(n)]
        sweep(p, path, radii, "Sobrancelha", "Head", seg=5)

    lines = []
    for s in (1, -1):
        lines.append(((0.07 * s, 1.022), (0.077 * s, 1.0), (0.073 * s, 0.978)))            # ao lado da boca
        lines.append(((0.142 * s, 1.103), (0.158 * s, 1.11)))                              # pés de galinha
        lines.append(((0.144 * s, 1.086), (0.162 * s, 1.086)))
        lines.append(((0.142 * s, 1.069), (0.157 * s, 1.062)))
    for pts in lines:
        path = [Vector((x, head_y(x, z, 0.0006), z)) for x, z in pts]
        if len(path) > 2:
            path = smooth_path(path, 2)
        n = len(path)
        radii = [0.0016 * (0.5 + 0.5 * math.sin(math.pi * i / (n - 1))) + 0.0003 for i in range(n)]
        sweep(p, path, radii, "Ruga", "Head", seg=3)


# ---------------------------------------------------------------- óculos



def build_glasses(arm, col):
    """Óculos redondos de aro dourado: lente abaulada com reflexo desenhado (ela é cega, os óculos
    fazem o papel dos olhos), ponte em arco, plaquetas no nariz e haste que passa por cima da orelha
    e desce atrás dela. As lentes abrem um pouco para fora, acompanhando a cabeça."""
    p = bc.Part("SM_CHR_Cida_Oculos", MATS)
    cy, cz, r_lens = -0.214, 1.09, 0.058
    up = Vector((0, 0, 1))

    for s in (1, -1):
        c = Vector((0.073 * s, cy, cz))
        n = Vector((0.1 * s, -1, 0)).normalized()
        u = up.cross(n).normalized()
        if u.x < 0:
            u = -u

        # aro e lente abaulada
        ring = [c + (u * math.cos(k * math.tau / 36) + up * math.sin(k * math.tau / 36)) * (r_lens + 0.002)
                for k in range(37)]
        sweep(p, ring, [0.0042] * 37, "Metal", "Head", seg=6)
        p.ellipsoid(c, (r_lens + 0.0015, r_lens + 0.0015, 0.0065), "Lente", "Head", seg=28, rings=5,
                    rot=(90, 0, math.degrees(math.atan2(0.1 * s, 1.0))))

        # reflexo: duas riscas de luz na diagonal, iguais nas duas lentes (a luz vem do mesmo lado)
        def on_lens(a, b, lift=0.0007):
            h = 0.0065 * math.sqrt(max(1 - a * a - b * b, 0.0))
            return c + u * (a * r_lens) + up * (b * r_lens) + n * (h + lift)

        for (a0, b0), (a1, b1), w in (((-0.62, 0.05), (-0.08, 0.62), 0.085), ((-0.38, -0.2), (0.02, 0.24), 0.04)):
            ta, tb = a1 - a0, b1 - b0
            ln = math.hypot(ta, tb)
            na, nb = -tb / ln * w, ta / ln * w
            left, right = [], []
            for i in range(4):
                t = i / 3
                a, b = a0 + ta * t, b0 + tb * t
                left.append(p.bm.verts.new(bc.scaled(on_lens(a + na, b + nb))))
                right.append(p.bm.verts.new(bc.scaled(on_lens(a - na, b - nb))))
            for i in range(3):
                quad = [right[i], right[i + 1], left[i + 1], left[i]]
                normal = (quad[1].co - quad[0].co).cross(quad[2].co - quad[0].co)
                p.bm.faces.new(quad if normal.dot(n) > 0 else quad[::-1])
            p._finish(left + right, "LenteBrilho", "Head", False)

        # plaqueta no nariz, presa ao aro por um arame
        pad = Vector((0.026 * s, -0.2165, 1.066))
        sweep(p, [c + (u * -0.75 * (1 if s > 0 else -1) + up * -0.66) * (r_lens + 0.002), pad + Vector((0.002 * s, -0.003, 0))],
              [0.0016, 0.0016], "Metal", "Head", seg=3)
        p.ellipsoid(pad, (0.0035, 0.006, 0.0095), "Plaqueta", "Head", seg=6, rings=4)

        # dobradiça e haste: por cima da orelha e descendo atrás dela, afinando na ponta
        hinge = Vector((0.134 * s, -0.2085, 1.098))
        p.box(hinge, (0.012, 0.009, 0.012), "Metal", "Head", rot_z=4 * s)
        temple = [hinge, Vector((0.17 * s, -0.178, 1.104)), Vector((0.2 * s, -0.1, 1.115)), Vector((0.213 * s, -0.03, 1.128)),
                  Vector((0.216 * s, -0.002, 1.133)), Vector((0.21 * s, 0.027, 1.122)), Vector((0.199 * s, 0.045, 1.098))]
        sweep(p, temple, [0.0034] * 5 + [0.0042, 0.0036], "Metal", "Head", seg=4)

    # cordinha de pérolas: sai das pontas das hastes, passa pelos lados da cabeça e cai atrás do
    # pescoço. A linha dourada é fina; as pérolas, espaçadas, dão a leitura de longe.
    side = [Vector(q) for q in ((0.199, 0.047, 1.096), (0.2, 0.07, 1.03), (0.165, 0.092, 0.96),
                                (0.11, 0.1, 0.9), (0.055, 0.09, 0.868))]
    keys = side + [Vector((0.0, 0.08, 0.858))] + [Vector((-q.x, q.y, q.z)) for q in reversed(side)]
    cord = smooth_path(keys, 2)
    sweep(p, cord, [0.0012] * len(cord), "Metal", "Head", seg=3)
    for q in smooth_path(keys, 3)[1:-1]:
        p.ellipsoid(q, (0.0062,) * 3, "Perola", "Head", seg=8, rings=5)

    # ponte em arco, um pouco acima do meio das lentes
    bridge = [Vector((x, cy - 0.003 - 0.004 * (1 - (x / 0.014) ** 2), 1.103 + 0.008 * (1 - (x / 0.014) ** 2)))
              for x in (-0.014, -0.009, -0.004, 0.0, 0.004, 0.009, 0.014)]
    sweep(p, bridge, [0.0034] * 7, "Metal", "Head", seg=4)
    return p.to_object(col, arm)


# ---------------------------------------------------------------- sapatos, pochete e bengala

SHOE_N = 24        # colunas em volta do pé


def shoe_outline(xc, s, th, grow=1.0):
    """Contorno da sola no ângulo th (ao redor do pé): calcanhar redondo, frente mais larga e bico
    redondo, com os dedos abrindo um pouco para fora. s = 1 no pé esquerdo, -1 no direito."""
    sn = math.sin(th)
    x = 0.05 * math.cos(th) * (1 - 0.13 * sn) * grow + s * 0.006 * max(0.0, -sn) ** 2
    # como no tênis do Tiago: bico longe da perna e calcanhar rente a ela (1,5 cm atrás do tornozelo)
    return Vector((xc + x, -0.03 + (0.094 if sn < 0 else 0.066) * sn * grow, 0.0))


def shoe_collar(xc, th):
    """Boca da sapatilha, no mesmo ângulo: baixa nos lados, alta no calcanhar e na frente."""
    sn = math.sin(th)
    z = 0.032 + (0.019 if sn > 0 else 0.012) * sn * sn
    return Vector((xc + 0.033 * math.cos(th), -0.0195 + 0.0485 * sn, z))


def ring_band(p, r0, r1, mat, bone, smooth=True):
    """Liga dois anéis de vértices de mesmo tamanho (r0 embaixo: a frente fica para fora)."""
    n = len(r0)
    for j in range(n):
        m = (j + 1) % n
        p.bm.faces.new((r0[j], r0[m], r1[m], r1[j]))
    p._finish(r0 + r1, mat, bone, smooth)


def build_shoe(p, xc, s, bone):
    """Sapatilha de tricô (referência do guilliverona): sola baixa de borracha, cabedal de malha
    preta, forro vinho na boca e tira elástica sobre o peito do pé, bordada de losangos."""
    ths = [k * math.tau / SHOE_N for k in range(SHOE_N)]
    new = lambda pts: [p.bm.verts.new(bc.scaled(q)) for q in pts]

    # sola: de baixo para cima, com a borda arredondada, e fechada por baixo
    z_sole = 0.013
    rows = [new([shoe_outline(xc, s, t, g) + Vector((0, 0, z)) for t in ths])
            for g, z in ((0.965, 0.0), (1.0, 0.003), (1.0, z_sole))]
    p.bm.faces.new(list(reversed(rows[0])))
    ring_band(p, rows[0], rows[1], "Sola", bone)
    ring_band(p, rows[1], rows[2], "Sola", bone)

    # cabedal: do contorno da sola até a boca; sobe primeiro e depois fecha, como um sapato de pano
    rows = []
    for t in (0.0, 0.18, 0.38, 0.6, 0.8, 1.0):
        row = []
        for th in ths:
            a, b = shoe_outline(xc, s, th, 0.975), shoe_collar(xc, th)
            q = a.lerp(b, t ** 1.7)
            q.z = z_sole + (b.z - z_sole) * math.sin(t * math.pi / 2) ** 0.7
            row.append(q)
        rows.append(new(row))
    for r0, r1 in zip(rows, rows[1:]):
        ring_band(p, r0, r1, "SapatoMalha", bone)

    # debrum da boca e forro vinho descendo por dentro
    collar = [shoe_collar(xc, t) for t in ths]
    sweep(p, collar + [collar[0]], [0.0033] * (SHOE_N + 1), "SapatoDebrum", bone, seg=4)
    inner = new([q.lerp(Vector((xc, q.y, q.z)), 0.06) for q in collar])
    floor = new([Vector((q.x, q.y, 0.018)).lerp(Vector((xc, -0.0195, 0.018)), 0.12) for q in collar])
    ring_band(p, list(reversed(floor)), list(reversed(inner)), "Forro", bone)
    p.bm.faces.new(floor)
    p._finish(floor, "Forro", bone, False)

    # peito do pé, de meia-calça, aparecendo na boca
    p.ellipsoid((xc, -0.032, 0.03), (0.03, 0.06, 0.0175), "Pele", bone, seg=10, rings=6)

    # tira elástica, arqueada sobre o peito do pé, de um lado ao outro da boca
    y0, half, w = -0.034, 0.037, 0.0095
    z_of = lambda x: 0.036 + 0.0135 * math.sqrt(max(1 - (x / half) ** 2, 0.0))
    xs = [half * (i / 3 - 1) * 0.999 for i in range(7)]
    front = new([(xc + x, y0 - w, z_of(x)) for x in xs])
    back = new([(xc + x, y0 + w, z_of(x)) for x in xs])
    for j in range(6):
        p.bm.faces.new((front[j], front[j + 1], back[j + 1], back[j]))
    p._finish(front + back, "SapatoDebrum", bone, True)
    # losangos bordados na tira
    for x in (-0.024, -0.012, 0.0, 0.012, 0.024):
        z = z_of(x) + 0.0012
        d_ = 0.0062
        quad = new([(xc + x, y0 - d_, z), (xc + x + d_, y0, z), (xc + x, y0 + d_, z), (xc + x - d_, y0, z)])
        p.bm.faces.new(quad)
        p._finish(quad, "Losango", bone, False)


def build_pouch(p):
    """Pochete de couro velho no cinto: bolsa macia acompanhando a barriga, aba com costura clara e
    fivela, bolsinho na frente, tira de feltro de afinador escapando e o cinto em volta da cintura."""
    # cinto
    p.tube((0, 0, 0.525), (0, 0, 0.547), 0.1955, 0.1945, "Couro", "Hips", seg=28, caps=False, squash_y=0.665)

    def bend(faces):
        # a pochete abraça a barriga: as pontas voltam para trás
        for v in {v for f in faces for v in f.verts}:
            v.co.y += 1.6 * (v.co.x / bc.K) ** 2 * bc.K

    bend(p.cloth_box((0, -0.156, 0.495), (0.2, 0.05, 0.09), "Couro", "Hips", cuts=3, radius=0.02))
    bend(p.cloth_box((0, -0.1625, 0.526), (0.206, 0.051, 0.05), "CouroEscuro", "Hips", cuts=3, radius=0.012))
    bend(p.cloth_box((0.045, -0.1845, 0.47), (0.082, 0.012, 0.042), "CouroEscuro", "Hips", cuts=1, radius=0.006))

    # costura clara na boca da aba e no bolsinho
    y_at = lambda x, y: y + 1.6 * x * x
    for k in range(13):
        x0 = -0.09 + k * 0.015
        p.tube((x0, y_at(x0, -0.1885), 0.5065), (x0 + 0.009, y_at(x0 + 0.009, -0.1885), 0.5065), 0.0011, 0.0011,
               "Costura", "Hips", seg=3, caps=False)

    # fivela da aba: tira que desce, moldura e pino
    p.box((-0.03, y_at(-0.03, -0.189), 0.497), (0.018, 0.004, 0.05), "Couro", "Hips")
    p.box((-0.03, y_at(-0.03, -0.1915), 0.492), (0.03, 0.003, 0.024), "Metal", "Hips")
    p.box((-0.03, y_at(-0.03, -0.1932), 0.492), (0.018, 0.003, 0.014), "CouroEscuro", "Hips")

    # chapinha da transportadora no bolsinho e tira de feltro vermelho escapando da pochete
    p.box((0.045, y_at(0.045, -0.1915), 0.47), (0.03, 0.002, 0.012), "Faixa", "Hips")
    p.box((0.085, y_at(0.085, -0.163), 0.541), (0.016, 0.004, 0.03), "Feltro", "Hips", rot_y=12)


def build_cane(col, socket, arm):
    """Bengala branca dobrável: mesma pega e mesma ponta do blockout (ver cb.build_cane), com punho
    de borracha, juntas de metal, fita isolante de conserto, faixa da transportadora, ponteira
    gasta e a cordinha de pulso."""
    cb.pose_hold(arm)
    pb = arm.pose.bones
    center = sum(((pb[f"Left{f}Proximal"].head + pb[f"Left{f}Intermediate"].tail) / 2
                  for f in cb.GRIP_FINGERS), Vector()) / len(cb.GRIP_FINGERS)
    top = center + Vector((0, 0, 0.035))
    tip = Vector((center.x, center.y, 0.0))
    p0, c, p2 = top, top + Vector((0, 0, 0.075)), top + Vector((0, -0.048, 0.048))   # gancho para a frente
    hook = [(1 - t) ** 2 * p0 + 2 * (1 - t) * t * c + t ** 2 * p2 for t in [i / 8 for i in range(9)]]
    to_socket = socket.matrix_world.inverted()
    local = lambda v: to_socket @ v + Vector(cb.GRIP)
    at = lambda z: local(Vector((tip.x, tip.y, z)))     # ponto do tubo na altura z

    p = bc.Part("SM_PRP_Bengala", MATS)
    h = top.z
    band_z = d(0, 440)[1]
    sweep(p, [local(v) for v in hook], [0.0105] * len(hook), "Borracha", "cane", seg=8)
    p.ellipsoid(local(hook[-1]), (0.0105,) * 3, "Borracha", "cane", seg=8, rings=4)
    p.tube(at(h), at(h - 0.14), 0.0125, 0.0125, "Borracha", "cane", seg=10)           # punho de borracha
    p.tube(at(h - 0.14), at(band_z), 0.01, 0.01, "Bengala", "cane", seg=10, caps=False)
    p.tube(at(band_z), at(0.03), 0.0103, 0.0103, "BengalaFaixa", "cane", seg=10, caps=False)
    # juntas de metal da bengala dobrável
    for z in (0.28, 0.54):
        p.tube(at(z - 0.009), at(z + 0.009), 0.0118, 0.0118, "Aco", "cane", seg=10)
    # conserto com fita isolante (uma ponta solta) e a faixa laranja da empresa
    p.tube(at(0.39), at(0.425), 0.0112, 0.0112, "Borracha", "cane", seg=10)
    p.box(at(0.38) + Vector((0.0, -0.011, 0.0)), (0.009, 0.0012, 0.03), "Borracha", "cane", rot_z=0)
    p.tube(at(h - 0.165), at(h - 0.19), 0.0106, 0.0106, "Faixa", "cane", seg=10)
    # ponteira de metal e bola gasta de borracha, encostando no chão
    p.tube(at(0.045), at(0.024), 0.0115, 0.0105, "Aco", "cane", seg=10)
    p.ellipsoid(at(0.015), (0.0145, 0.0145, 0.015), "Borracha", "cane", seg=10, rings=6)
    # cordinha de pulso, saindo do punho e pendurada num laço
    hang = Vector((tip.x, tip.y - 0.012, h - 0.145))
    loop = [local(hang + Vector((0.016 * math.sin(a), -0.008 * (1 - math.cos(a)), -0.05 * (1 - math.cos(a)))))
            for a in (i * math.tau / 12 for i in range(13))]
    sweep(p, loop, [0.0022] * len(loop), "Cordao", "cane", seg=3)

    tip_local = to_socket @ tip
    cb.clear_pose(arm)
    print("PEGA centro", tuple(round(x, 3) for x in center), "ponta", tuple(round(x, 3) for x in tip))
    return p.to_prop(col, socket, cb.GRIP), tip_local


def build_body(arm, col):
    p = bc.Part("SK_CHR_Cida", MATS)

    # pernas (meia-calça) e sapatilhas
    for s, side in ((1, "Left"), (-1, "Right")):
        x = 0.067 * s
        p.tube((x, 0, 0.53), (x, 0, 0.29), 0.035, 0.03, "Pele", f"{side}UpperLeg", seg=10)
        p.tube((x, 0, 0.29), (x, 0, 0.045), 0.026, 0.021, "Pele", f"{side}LowerLeg", seg=10)
        build_shoe(p, x, s, f"{side}Foot")

    build_skirt(p)
    build_cardigan(p)
    build_vest(p)
    build_vest_details(p)
    build_elbow_patch(p)

    build_pouch(p)

    # braços (mangas do cardigã), punhos canelados, mãos
    for side, (_, sh, el, wr, _) in cb.ARMS.items():
        p.limb([sh, el, wr], (0.045, 0.042, 0.038), "Cardiga", [f"{side}UpperArm", f"{side}LowerArm"])
        wr, el = Vector(wr), Vector(el)
        cuff = (wr - el).normalized()
        ribbed_band(p, wr - cuff * 0.04, wr + cuff * 0.002, 0.0455, 0.044, 12, 0.0014, "CardigaEscuro", f"{side}LowerArm")
        p.tube(wr - cuff * 0.04, wr + cuff * 0.002, 0.0435, 0.0425, "CardigaEscuro", f"{side}LowerArm", seg=12)
        p.hand(side, cb.ARMS[side], cb.HAND, "Pele", nail="Unha")
    # aliança no anelar esquerdo, relógio no pulso direito
    ring = bc.hand_chains("Left", cb.ARMS["Left"], cb.HAND)["Ring"]
    mid = ring[0].lerp(ring[1], 0.45)
    along = (ring[1] - ring[0]).normalized() * 0.0018
    r = cb.HAND["radius"]["Ring"] * 1.12
    p.tube(mid - along, mid + along, r, r, "Metal", "LeftRingProximal", seg=10)
    watch = Vector(cb.ARMS["Right"][3]) + Vector((0, 0, -0.006))
    p.tube(watch + Vector((0, 0, 0.007)), watch - Vector((0, 0, 0.007)), 0.04, 0.04, "Metal", "RightHand", seg=12)

    # pescoço, cabeça, orelhas, brincos, nariz, sobrancelhas e sorriso: ainda os do blockout
    p.tube((0, -0.01, 0.84), (0, -0.015, 0.96), 0.042, 0.04, "Pele", "Neck", seg=12)
    p.ellipsoid(HEAD_C, HEAD_R, "Pele", "Head", seg=28, rings=16)
    for s in (1, -1):
        ex, ez = d(120 + 62 * s, 138)
        p.ellipsoid((ex, 0.0, ez), (0.037, 0.022, 0.037), "Pele", "Head", seg=12, rings=8)
        # brinco: tarraxa dourada no lóbulo, elo, capinha e a pérola em gota
        p.ellipsoid((ex, -0.005, ez - 0.034), (0.0058, 0.0058, 0.0042), "Metal", "Head", seg=10, rings=5)
        p.tube((ex, -0.005, ez - 0.036), (ex, -0.005, ez - 0.047), 0.0016, 0.0016, "Metal", "Head", seg=3, caps=False)
        p.ellipsoid((ex, -0.005, ez - 0.0485), (0.0072, 0.0072, 0.0038), "Metal", "Head", seg=10, rings=5)
        p.ellipsoid((ex, -0.005, ez - 0.0615), (0.0122, 0.0122, 0.0155), "Perola", "Head", seg=14, rings=9)
    nx, nz = d(120, 155)
    p.ellipsoid((nx, -0.212, nz), (0.022, 0.03, 0.028), "Pele", "Head", seg=12, rings=8)
    p.polyline([(-0.053, -0.197, 0.997), (-0.025, -0.205, 0.975), (0.0, -0.207, 0.968),
                (0.03, -0.205, 0.978), (0.053, -0.197, 1.0)], 0.0065, "Boca", "Head", seg=6)
    build_face_marks(p)
    return p.to_object(col, arm)


# ---------------------------------------------------------------- fotos

def shot(scene, col, name, target, offset, lens=85, size=(900, 900), ortho=None):
    cam_data = bpy.data.cameras.new(f"CAM_{name}")
    if ortho:
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = ortho
    else:
        cam_data.lens = lens
    cam = bpy.data.objects.new(f"CAM_{name}", cam_data)
    col.objects.link(cam)
    t = Vector(target) * bc.K
    cam.location = t + Vector(offset)
    cam.rotation_euler = (t - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.filepath = str(OUT / "previews" / "detalhado" / f"{name}.png")
    bpy.ops.render.render(write_still=True)


def closeups(scene, col):
    head = (0, 0, 1.2)
    shot(scene, col, "close_cabeca_frente", head, (0, -2.2, 0.05))
    shot(scene, col, "close_cabeca_tres_quartos", head, (1.35, -1.7, 0.25))
    shot(scene, col, "close_cabeca_perfil", head, (-2.2, 0, 0.05))
    shot(scene, col, "close_cabeca_costas", head, (0.7, 2.1, 0.45))
    shot(scene, col, "close_tronco_frente", (0, -0.1, 0.68), (0.0, -2.3, 0.1))
    shot(scene, col, "close_tronco_tres_quartos", (0, -0.05, 0.68), (1.4, -1.9, 0.3))
    shot(scene, col, "close_tronco_costas", (0, 0.1, 0.68), (0.8, 2.2, 0.3))
    shot(scene, col, "close_saia", (0, 0, 0.3), (1.1, -2.1, 0.35))
    shot(scene, col, "close_bengala", (0.2, -0.3, 0.42), (1.2, -2.3, 0.2), lens=70, size=(800, 1100))
    shot(scene, col, "close_pega", (0.2, -0.3, 0.6), (0.7, -1.1, 0.1))
    shot(scene, col, "close_cracha", (-0.085, -0.12, 0.765), (-0.05, -0.55, 0.02), size=(1100, 700))


def side_by_side(scene, col):
    """Blockout (à esquerda) e detalhada (à direita), de frente e de 3/4."""
    src = OUT / "SK_CHR_Cida_Blockout.blend"
    with bpy.data.libraries.load(str(src)) as (data_from, data_to):
        data_to.objects = [n for n in data_from.objects if not n.startswith("CAM_")]
    tmp = bpy.data.collections.new("_Comparacao")
    scene.collection.children.link(tmp)
    for o in data_to.objects:
        if o:
            tmp.objects.link(o)
            if o.parent is None:
                o.location.x -= 0.4
    for o in bpy.data.objects:
        if o.parent is None and o.type == "ARMATURE" and o.name not in tmp.objects:
            o.location.x += 0.4
    bpy.context.view_layer.update()
    h = cb.HEIGHT
    shot(scene, col, "lado_a_lado_frente", (0, 0, h / 2), (0, -8, 0), size=(1300, 900), ortho=h * 1.15)
    shot(scene, col, "lado_a_lado_tres_quartos", (0, 0, h / 2), (3.0, -6.2, 0.7), lens=60, size=(1300, 900))


def main():
    scene, cols = bc.new_scene("Cida")
    arm = cb.build_rig(cols["Rig"])
    body = build_body(arm, cols["Geo"])
    bc.metric_uvs(body, long_side_u=False)   # UV em metros: o grão de tricô do cardigã (Malha) e da saia
    hair = build_hair(arm, cols["Geo"])
    glasses = build_glasses(arm, cols["Geo"])

    # Encaixes iguais aos do blockout: a ficha e o avatar Humanoid continuam valendo.
    soc = cols["Sockets"]
    soc_l = bc.add_socket("SOC_Hand_L", soc, arm, "LeftHand", cb.GRIP)
    bc.add_socket("SOC_Hand_R", soc, arm, "RightHand", (-0.2, -0.01, 0.515))
    bc.add_socket("SOC_Bun", soc, arm, "bun_jiggle", (0.0, 0.05, d(0, 46)[1]))
    cane, tip = build_cane(cols["Props"], soc_l, arm)
    bc.add_child_empty("SOC_CaneTip", soc, cane, tip)

    bc.report_tris((body, hair, glasses, cane))
    fbx = bc.UNITY_ROOT / "Characters" / "Cida" / "SK_CHR_Cida.fbx"
    bc.export_fbx(fbx)   # sai em repouso; as fotos e o .blend mostram a Cida segurando a bengala
    cb.pose_hold(arm)
    out = OUT / "previews" / "detalhado"
    bc.render_previews(scene, cols["Preview"], out, "cida_detalhada", cb.HEIGHT)
    closeups(scene, cols["Preview"])
    bc.save_blend(OUT / "SK_CHR_Cida_Detalhado.blend")
    side_by_side(scene, cols["Preview"])
    print("SAVED", OUT / "SK_CHR_Cida_Detalhado.blend", fbx)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        traceback.print_exc()
        sys.exit(1)
