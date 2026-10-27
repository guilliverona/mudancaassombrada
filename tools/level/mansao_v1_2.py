# Mansão do Espelho V1.2 (Fase 1), fiel à arte de referência: casa na encosta.
# Térreo em Y 0; os fundos (Z >= 7) ficam em cima do morro, no nível Y 3,6.
# Rodar: python tools/level/mansao_v1_2.py  → confere sobreposição, portas na parede comum,
# apoio do nível de cima e mede a distância andando de cada cômodo até o caminhão.
# Ver LEVEL_DESIGN_FASE1_MANSAO.md.
import itertools, heapq, math, json, sys
HILL_Z = 7
R = {  # id: (nome, nivel, x0, x1, z0, z1)
 'EXT': ('Calçada / escadaria da frente', 0, -6, 22, -21, -17),
 '1':  ('Entrada / Spawn', 0, -4, 4, -17, -11),
 '2':  ('Grande Hall + Relógio', 0, -4, 4, -11, -1),
 '3':  ('Escadaria (pé)', 0, -4, 4, -1, 7),
 'CO': ('Corredor Oeste + escada de serviço', 0, -6, -4, -11, 7),
 '8':  ('Sala de Jantar', 0, -16, -6, -3, 7),
 '9':  ('Cozinha', 0, -16, -6, -13, -3),
 '10': ('Sala dos Retratos', 0, 4, 14, -1, 7),
 '11': ('Sala de Música', 0, 4, 14, -9, -1),
 '12': ('Depósito de Props', 0, 4, 14, -17, -9),
 'CL': ('Galeria Leste (desce) + corredor de saída', 0, 14, 16, -9, 7),
 '14': ('Saída / Caminhão', 0, 14, 22, -17, -9),
 'P':  ('Patamar', 1, -4, 4, 5, 7),
 '4':  ('Galeria dos Espelhos', 1, -6, 16, 7, 11),
 '7':  ('Biblioteca', 1, -16, -6, 7, 15),
 '5':  ('Salão de Baile / Palco', 1, -6, 4, 11, 19),
 '6':  ('Camarins / Figurinos', 1, 4, 14, 11, 19),
 '13': ('Câmara Secreta do Espelho', 1, 14, 20, 11, 19),
}
D = [  # (a, b, largura, x, z, tipo)
 ('EXT','1',1.6,0,-17,'dupla'), ('1','2',4.0,0,-11,'arco'), ('1','12',1.6,4,-14,'dupla'),
 ('2','3',8.0,0,-1,'aberto'), ('2','CO',1.6,-4,-6,'dupla'), ('3','CO',1.2,-4,3,'simples'),
 ('CO','8',1.6,-6,-1,'dupla'), ('CO','9',1.2,-6,-7,'simples'), ('8','9',1.6,-11,-3,'dupla'),
 ('2','11',1.6,4,-5,'dupla'), ('3','10',1.2,4,3,'simples'), ('10','11',1.2,9,-1,'simples'),
 ('11','12',1.6,9,-9,'dupla'), ('12','14',2.4,14,-13,'carga'), ('CL','14',1.6,15,-9,'dupla'),
 ('CL','11',1.2,14,-5,'simples'), ('EXT','14',4.0,18,-17,'portão'),
 ('P','4',8.0,0,7,'aberto'), ('4','7',1.6,-6,9,'dupla'), ('4','5',1.6,-4,11,'dupla'),
 ('4','6',1.6,9,11,'dupla'), ('7','5',1.2,-6,13,'simples'), ('5','6',1.2,4,15,'coxia'),
 ('6','13',1.2,14,15,'espelho-porta'),
]
STAIRS = [  # (nome, de(sala, x, z), até(sala, x, z), largura, avanço)
 ('Escadaria principal', ('3', 0, -1), ('P', 0, 5), 3.0, 6.0),
 ('Escada de serviço oeste', ('CO', -5, 1), ('4', -5, 7), 2.0, 6.0),
 ('Galeria leste (desce)', ('CL', 15, 1), ('4', 15, 7), 2.0, 6.0),
]
# Luzes fixas da casa (o resto é escuro: a lanterna é a luz da noite). Velas são só chama: não iluminam.
# (nível, x, z, tipo, nome)   tipo: acesa | falha | apagada (só acende num evento) | luar | ciano
LUZES = [
 (0, -2.6, -17.6, 'acesa', 'lampião do portão'), (0, 2.6, -17.6, 'acesa', 'lampião do portão'),
 (0, 18, -9.6, 'acesa', 'faróis do caminhão'), (0, -3.7, -8, 'falha', 'arandela do hall'),
 (0, 0, -6.5, 'apagada', 'lustre (só na aparição)'), (0, 13.7, 3, 'falha', 'arandela dos retratos'),
 (0, 9, -13, 'falha', 'lâmpada nua do depósito'), (0, -15.7, 2, 'luar', 'janela do jantar'),
 (1, 12, 7.3, 'falha', 'arandela da galeria (ruído nº 3)'), (1, 0, 6, 'luar', 'clarabóia'),
 (1, -11, 10, 'apagada', 'luminária verde (clique)'), (1, -1, 17.6, 'apagada', 'refletor do palco (só na aparição)'),
(1, 6.5, 18.6, 'acesa', 'lâmpada de camarim'),
 (1, 11, 18.6, 'falha', 'lâmpada de camarim'), (1, 19.6, 15, 'ciano', 'portal'),
]
# Pilhas: fixas (à vista) e escondidas (caem de algo que tomba ou estão numa gaveta).
PILHAS = [
 (0, -14.6, -8, 'fixa'), (0, -5, -9.5, 'fixa'), (0, 5, 5.5, 'fixa'), (0, 12.5, -15, 'fixa'),
 (1, -3, 18.2, 'fixa'), (0, 15, -6, 'fixa'),
 (0, -15.4, 0, 'escondida'), (0, 8, -4.2, 'escondida'), (1, -14.5, 12, 'escondida'), (1, 13, 13, 'escondida'),
]

def area(r): return (r[3]-r[2])*(r[5]-r[4])
def edge(r, x, z):
    inside = r[2]-1e-6 <= x <= r[3]+1e-6 and r[4]-1e-6 <= z <= r[5]+1e-6
    return inside and (min(abs(x-r[2]), abs(x-r[3]), abs(z-r[4]), abs(z-r[5])) < 1e-6)
ok = True
for a, b in itertools.combinations(R, 2):
    A, B = R[a], R[b]
    if A[1] != B[1]: continue
    if min(A[3],B[3])-max(A[2],B[2]) > 1e-6 and min(A[5],B[5])-max(A[4],B[4]) > 1e-6: print('SOBREPÕE', a, b); ok = False
for d in D:
    if not (edge(R[d[0]], d[3], d[4]) and edge(R[d[1]], d[3], d[4])): print('PORTA FORA DA PAREDE COMUM', d); ok = False
# apoio do nível de cima: sobre o morro (Z >= 7) ou sobre um cômodo do térreo
for k, r in R.items():
    if r[1] != 1: continue
    sup = max(0, r[5]-max(r[4], HILL_Z))*(r[3]-r[2])
    for g in R.values():
        if g[1] == 0:
            ox = min(r[3],g[3])-max(r[2],g[2]); oz = min(r[5],g[5],HILL_Z)-max(r[4],g[4])
            if ox > 0 and oz > 0: sup += ox*oz
    if sup + 1e-6 < area(r): print('SEM APOIO', k, sup, area(r)); ok = False
# térreo não pode entrar no morro
for k, r in R.items():
    if r[1] == 0 and r[5] > HILL_Z + 1e-6: print('TÉRREO DENTRO DO MORRO', k); ok = False
print('geometria ok' if ok else 'PROBLEMAS')
g0 = sum(area(r) for k, r in R.items() if r[1]==0 and k not in ('EXT','14'))
g1 = sum(area(r) for r in R.values() if r[1]==1)
print(f'área interna: térreo {g0:.0f} m², nível de cima {g1:.0f} m², total {g0+g1:.0f} m²')
for s in STAIRS: print(f"{s[0]}: {math.degrees(math.atan(3.6/s[4])):.1f}°, {round(3.6/0.18)} degraus, largura {s[3]} m")
def cen(k): r = R[k]; return ((r[2]+r[3])/2, (r[4]+r[5])/2)
adj = {}
def link(p, q, w): adj.setdefault(p, []).append((q, w)); adj.setdefault(q, []).append((p, w))
pts = {}
for i, d in enumerate(D):
    for k in d[:2]: link(('r', k), ('d', i), math.dist(cen(k), d[3:5])); pts.setdefault(k, []).append((('d', i), d[3:5]))
for i, s in enumerate(STAIRS):
    (ka, xa, za), (kb, xb, zb) = s[1], s[2]
    link(('s', i, 0), ('s', i, 1), math.hypot(s[4], 3.6))
    for k, p, n in ((ka, (xa, za), ('s', i, 0)), (kb, (xb, zb), ('s', i, 1))):
        link(('r', k), n, math.dist(cen(k), p)); pts.setdefault(k, []).append((n, p))
for k, lst in pts.items():
    for (n1, p1), (n2, p2) in itertools.combinations(lst, 2): link(n1, n2, math.dist(p1, p2))
def dij(src):
    dist = {src: 0}; pq = [(0, src)]
    while pq:
        d0, u = heapq.heappop(pq)
        if d0 > dist[u]: continue
        for v, w in adj.get(u, []):
            if d0 + w < dist.get(v, 1e9): dist[v] = d0 + w; heapq.heappush(pq, (dist[v], v))
    return dist
dt = dij(('r', '14'))
print('\ndistância andando até o caminhão (centro do cômodo):')
for k in R:
    if k != '14': print(f"  {k:>3} {R[k][0]:42} {dt.get(('r',k), float('nan')):5.0f} m")
