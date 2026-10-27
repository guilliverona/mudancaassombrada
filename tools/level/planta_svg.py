# Desenha as plantas e o corte da V1.2 a partir do mansao_v1_2.py (paleta do Art Bible ENV_01).
# Rodar: python tools/level/planta_svg.py Art/Source/Environment/MansaoFase1/plantas
import os, sys, runpy
P = runpy.run_path(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mansao_v1_2.py'), run_name='lib'); R, D, ST = P['R'], P['D'], P['STAIRS']
LUZES, PILHAS = P['LUZES'], P['PILHAS']
S = 20; X0, X1, Z0, Z1 = -18, 24, -23, 21
W, H = (X1-X0)*S, (Z1-Z0)*S
px = lambda x: round((x-X0)*S, 1)
pz = lambda z: round((Z1-z)*S, 1)
FONT = 'font-family="IBM Plex Mono, monospace"'
COR = {'EXT':'#1d2024','1':'#4a2328','2':'#5a4630','3':'#4e2a33','CO':'#3a2c30','8':'#47331f','9':'#33403c','10':'#552a2c',
       '11':'#43293b','12':'#46372a','CL':'#1d4a50','14':'#263238','P':'#4e2a33','4':'#1d4a50','7':'#28432f','5':'#5a2030','6':'#5a3826','13':'#0f3d44'}
NUM = {k: k for k in ('1','2','3','4','5','6','7','8','9','10','11','12','13','14')}
NOME = {'EXT':'Calçada','1':'Entrada','2':'Grande Hall','3':'Escadaria','CO':'Corr. Oeste','8':'Sala de Jantar','9':'Cozinha','10':'Sala dos Retratos',
        '11':'Sala de Música','12':'Depósito de Props','CL':'Galeria Leste','14':'Pátio · Caminhão','P':'Patamar','4':'Galeria dos Espelhos',
        '7':'Biblioteca','5':'Salão de Baile · Palco','6':'Camarins · Figurinos','13':'Câmara'}
# espelhos: nivel, x, z, 'h'|'v', tamanho, rótulo, tipo
M = [(1,-2,11,'h',2.0,'E1','heroi'),(1,2,11,'h',2.0,'E2','heroi'),(1,7,11,'h',1.2,'E3','comum'),(0,16,-3,'v',1.2,'E4','comum'),
     (1,-6,17,'v',2.0,'E5','comum'),(1,6.5,19,'h',0.6,'E6a','comum'),(1,11,19,'h',0.6,'E6b','comum'),(0,-6,-10,'v',2.0,'E7','comum'),
     (0,9,7,'h',1.2,'E8','comum'),(1,14,15,'v',2.0,'E9','porta'),(0,-4,-14,'v',0.6,'E10','comum'),(0,12,-9,'h',1.2,'E11','comum'),
     (0,-16,2,'v',1.2,'E12','comum'),(1,20,15,'v',2.4,'PORTAL','portal')]
MC = {'heroi':'#FFFFFF','comum':'#BFD0D6','porta':'#E5687A','portal':'#7FE3C4'}
DC = {'dupla':'#FFC98A','arco':'#FFC98A','aberto':'#FFC98A','carga':'#F08A3C','portão':'#F08A3C','simples':'#EEE7D8','coxia':'#EEE7D8','espelho-porta':'#E5687A'}

def rect(x0, x1, z0, z1, fill, stroke='#C9A24A', sw=3, extra=''):
    return f'<rect x="{px(x0)}" y="{pz(z1)}" width="{(x1-x0)*S}" height="{(z1-z0)*S}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" {extra}/>'

def text(x, z, t, size=12, fill='#EEE7D8', w=400, anchor='middle', dy=0):
    return f'<text x="{px(x)}" y="{pz(z)+dy}" fill="{fill}" font-size="{size}" font-weight="{w}" text-anchor="{anchor}" {FONT}>{t}</text>'

def steps(x0, x1, z0, z1, n, fill):
    o = [rect(x0, x1, z0, z1, fill, '#E9DFC8', 1.5)]
    for i in range(1, n):
        z = z0 + (z1-z0)*i/n
        o.append(f'<line x1="{px(x0)}" y1="{pz(z)}" x2="{px(x1)}" y2="{pz(z)}" stroke="#2a1418" stroke-width="1"/>')
    return o

def grid():
    o = []
    for gx in range(X0, X1+1, 2): o.append(f'<line x1="{px(gx)}" y1="0" x2="{px(gx)}" y2="{H}" stroke="#1c1a21" stroke-width="1"/>')
    for gz in range(Z0, Z1+1, 2): o.append(f'<line x1="0" y1="{pz(gz)}" x2="{W}" y2="{pz(gz)}" stroke="#1c1a21" stroke-width="1"/>')
    return o

def doors(level):
    o = []
    for d in D:
        a = R[d[0]]
        if a[1] != level: continue
        x, z, w, t = d[3], d[4], d[2], d[5]; c = DC[t]
        vert = any(abs(x-v) < 1e-6 for v in (a[2], a[3])) and not any(abs(z-v) < 1e-6 for v in (a[4], a[5]))
        if vert: o.append(f'<line x1="{px(x)}" y1="{pz(z-w/2)}" x2="{px(x)}" y2="{pz(z+w/2)}" stroke="{c}" stroke-width="7"/>')
        else: o.append(f'<line x1="{px(x-w/2)}" y1="{pz(z)}" x2="{px(x+w/2)}" y2="{pz(z)}" stroke="{c}" stroke-width="7"/>')
    return o

def mirrors(level):
    o = []
    for lv, x, z, ori, sz, lab, t in M:
        if lv != level: continue
        c = MC[t]
        if ori == 'h': o.append(f'<line x1="{px(x-sz/2)}" y1="{pz(z)}" x2="{px(x+sz/2)}" y2="{pz(z)}" stroke="{c}" stroke-width="6"/>')
        else: o.append(f'<line x1="{px(x)}" y1="{pz(z-sz/2)}" x2="{px(x)}" y2="{pz(z+sz/2)}" stroke="{c}" stroke-width="6"/>')
        if ori == 'h': o.append(text(x, z-0.75, lab, 10, c, 600))
        else: o.append(text(x+0.35, z+sz/2+0.25, lab, 10, c, 600, 'start'))
    return o

def room_labels(level):
    o = []
    for k, r in R.items():
        if r[1] != level: continue
        cx, cz = (r[2]+r[3])/2, (r[4]+r[5])/2
        w, d = r[3]-r[2], r[5]-r[4]
        if k == 'CO':
            o.append(f'<text transform="translate({px(-5)},{pz(-4)}) rotate(-90)" fill="#EEE7D8" font-size="11" text-anchor="middle" {FONT}>Corredor Oeste · 2 m</text>'); continue
        if k == 'CL':
            o.append(f'<text transform="translate({px(15)},{pz(-4)}) rotate(-90)" fill="#EEE7D8" font-size="11" text-anchor="middle" {FONT}>Galeria Leste · 2 m</text>'); continue
        n = NUM.get(k); lab = (n+' · ' if n else '') + NOME[k]
        dz = {'3': 3.3, '2': -1.6, '4': 0.4, 'P': 0.2, '14': 2.6, '6': 1.5, '5': 0.5}.get(k, 0)
        o.append(text(cx, cz+dz, lab, 13 if w >= 8 else 11, '#EEE7D8', 700))
        o.append(text(cx, cz+dz, f'{w:g} × {d:g} m', 10, '#A79FAF', 400, dy=15))
    return o

LC = {'acesa':'#FFC98A','falha':'#F08A3C','apagada':'#6b5a3a','luar':'#8FB0E0','ciano':'#7FE3C4'}
def lights_batt(level):
    o = []
    for lv, x, z, t, n in LUZES:
        if lv != level: continue
        o.append(f'<circle cx="{px(x)}" cy="{pz(z)}" r="{18 if t!="apagada" else 7}" fill="{LC[t]}" opacity="{0.22 if t!="apagada" else 0}"/>')
        o.append(f'<circle cx="{px(x)}" cy="{pz(z)}" r="5" fill="{LC[t]}" stroke="#131118" stroke-width="1.5"/>')
    for lv, x, z, t in PILHAS:
        if lv != level: continue
        o.append(f'<rect x="{px(x)-5}" y="{pz(z)-5}" width="10" height="10" rx="2" fill="{"#8CC79A" if t=="fixa" else "none"}" stroke="#8CC79A" stroke-width="2"/>')
    return o

def plan_ground():
    o = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}">', f'<rect width="{W}" height="{H}" fill="#131118"/>'] + grid()
    o.append(rect(X0, X1, 7, Z1, '#18161c', 'none', 0))
    o.append(text(3, 15, 'ENCOSTA: o terreno sobe para Y 3,6', 14, '#4a4352', 600))
    o.append(text(3, 13.6, '(a ala do teatro fica em cima do morro)', 12, '#4a4352'))
    for k, r in R.items():
        if r[1] == 0:
            ext = k in ('EXT', '14')
            o.append(rect(r[2], r[3], r[4], r[5], COR[k], '#6b5a3a' if ext else '#C9A24A', 2 if ext else 3))
    o.append(rect(-4, 4, -11, 7, 'none', '#7FE3C4', 2, 'stroke-dasharray="6 6"'))
    o.append(text(0, -10.3, 'pé-direito duplo 6,8 m', 10, '#7FE3C4'))
    o += steps(-1.5, 1.5, -1, 5, 20, '#7A1E2E'); o.append(text(0, 1.6, '↑', 20, '#FFC98A', 700))
    o += steps(-6, -4, 1, 7, 20, '#5a3a2a'); o.append(text(-5, 3.6, '↑', 16, '#FFC98A', 700))
    o += steps(14, 16, 1, 7, 20, '#1d6066'); o.append(text(15, 3.6, '↑', 16, '#FFC98A', 700))
    o.append(f'<circle cx="{px(0)}" cy="{pz(-3)}" r="8" fill="#C9A24A"/>'); o.append(text(0.8, -3.25, 'relógio', 10, '#C9A24A', 600, 'start'))
    o.append(f'<circle cx="{px(0)}" cy="{pz(-6.5)}" r="12" fill="none" stroke="#FFC98A" stroke-width="2"/>'); o.append(text(1, -6.75, 'lustre', 10, '#FFC98A', 600, 'start'))
    o.append(rect(16.5, 19.5, -15.5, -10, '#3F5A5E', '#F08A3C', 3, 'rx="5"')); o.append(text(18, -13, 'caminhão', 10, '#EEE7D8'))
    for sx in (-1.2, 0, 1.2): o.append(f'<circle cx="{px(sx)}" cy="{pz(-19.3)}" r="6" fill="#7FE3C4"/>')
    o.append(text(0, -20.6, 'nascimento ×3', 10, '#7FE3C4'))
    o.append(rect(8, 10.5, -6.4, -5, '#0b0a0f', '#E9DFC8', 1)); o.append(text(9.25, -7.2, 'piano', 9, '#A79FAF'))
    o.append(rect(-13.5, -8.5, 1, 2.6, '#2a1a10', '#E9DFC8', 1)); o.append(text(-11, 0.2, 'mesa para 9', 9, '#A79FAF'))
    o += room_labels(0) + doors(0) + mirrors(0) + lights_batt(0)
    o.append('</svg>'); return '\n'.join(o)

def plan_upper():
    o = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}">', f'<rect width="{W}" height="{H}" fill="#131118"/>'] + grid()
    for k, r in R.items():
        if r[1] == 0 and k != 'EXT': o.append(rect(r[2], r[3], r[4], r[5], 'none', '#2e2a36', 1.5, 'stroke-dasharray="4 4"'))
    o.append(rect(-4, 4, -11, 5, '#0b0a0f', '#7FE3C4', 2, 'stroke-dasharray="6 6"'))
    o.append(text(0, -4, 'VÃO DO HALL', 12, '#7FE3C4', 700)); o.append(text(0, -5.2, '6,8 m até o forro', 10, '#7FE3C4'))
    o.append(text(0, -6.4, 'o lustre pende aqui', 10, '#7FE3C4'))
    for k, r in R.items():
        if r[1] == 1: o.append(rect(r[2], r[3], r[4], r[5], COR[k]))
    o += steps(-1.5, 1.5, -1, 5, 20, '#3a1018'); o.append(text(0, 1.6, 'escadaria', 10, '#E9DFC8'))
    o += steps(-6, -4, 1, 7, 20, '#2a1d15'); o += steps(14, 16, 1, 7, 20, '#123a3e')
    o.append(text(-6.4, 4, 'serviço ↓', 10, '#FFC98A', 600, 'end')); o.append(text(16.4, 4, '↓ saída', 10, '#FFC98A', 600, 'start'))
    for x0, x1 in ((-4, -1.5), (1.5, 4)):
        o.append(f'<line x1="{px(x0)}" y1="{pz(5)}" x2="{px(x1)}" y2="{pz(5)}" stroke="#E9DFC8" stroke-width="4" stroke-dasharray="2 3"/>')
    o.append(text(-2.75, 4.2, 'peitoril', 9, '#E9DFC8'))
    o.append(rect(-5, 3, 17.4, 19, '#7A1E2E', '#E9DFC8', 1)); o.append(text(-1, 16.6, 'palco + cortinas', 9, '#E9DFC8'))
    o.append(rect(15.6, 18.4, 11.8, 13.2, '#0b0a0f', '#7FE3C4', 1)); o.append(text(17, 12.25, 'cartaz', 9, '#7FE3C4'))
    o += room_labels(1) + doors(1) + mirrors(1) + lights_batt(1)
    o.append('</svg>'); return '\n'.join(o)

def section():
    s = 22; ZA, ZB, YA, YB = -23, 21, -1.5, 8.5
    w, h = (ZB-ZA)*s, (YB-YA)*s
    qx = lambda z: round((z-ZA)*s, 1)
    qy = lambda y: round((YB-y)*s, 1)
    o = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">', f'<rect width="{w}" height="{h}" fill="#131118"/>']
    o.append(f'<polygon points="{qx(ZA)},{qy(0)} {qx(7)},{qy(0)} {qx(7)},{qy(3.6)} {qx(ZB)},{qy(3.6)} {qx(ZB)},{qy(YA)} {qx(ZA)},{qy(YA)}" fill="#24201c"/>')
    o.append(f'<text x="{qx(15)}" y="{qy(2.4)}" fill="#6a5e54" font-size="12" text-anchor="middle" {FONT}>morro (encosta)</text>')
    def box(z0, z1, y0, y1, fill, lab=''):
        o.append(f'<rect x="{qx(z0)}" y="{qy(y1)}" width="{(z1-z0)*s}" height="{(y1-y0)*s}" fill="{fill}" stroke="#C9A24A" stroke-width="2"/>')
        if lab: o.append(f'<text x="{qx((z0+z1)/2)}" y="{qy(y1)+16}" fill="#EEE7D8" font-size="11" text-anchor="middle" {FONT}>{lab}</text>')
    box(-17, -11, 0, 3.2, '#4a2328', '1 Entrada · 3,2 m')
    box(-11, 7, 0, 6.8, '#3a3028', '2 + 3 · Hall e Escadaria · 6,8 m')
    box(5, 7, 3.2, 3.6, '#C9A24A')
    box(7, 11, 3.6, 6.8, '#1d4a50', '4 Galeria')
    box(11, 19, 3.6, 6.8, '#5a2030', '5 Salão · Palco')
    for i in range(20):
        z = -1 + i*0.3; y = i*0.18
        o.append(f'<polyline points="{qx(z)},{qy(y)} {qx(z)},{qy(y+0.18)} {qx(z+0.3)},{qy(y+0.18)}" fill="none" stroke="#E9DFC8" stroke-width="1.2"/>')
    o.append(f'<text x="{qx(5.6)}" y="{qy(1.2)}" fill="#FFC98A" font-size="11" text-anchor="start" {FONT}>escadaria: 20 × 0,18 / 0,30 · 31°</text>')
    o.append(f'<line x1="{qx(-6.5)}" y1="{qy(6.8)}" x2="{qx(-6.5)}" y2="{qy(5.3)}" stroke="#FFC98A" stroke-width="2"/>')
    o.append(f'<circle cx="{qx(-6.5)}" cy="{qy(5.0)}" r="9" fill="none" stroke="#FFC98A" stroke-width="2"/>')
    o.append(f'<text x="{qx(-6.5)}" y="{qy(4.2)}" fill="#FFC98A" font-size="10" text-anchor="middle" {FONT}>lustre</text>')
    o.append(f'<rect x="{qx(-3.2)}" y="{qy(2.1)}" width="{0.32*s}" height="{2.1*s}" fill="#C9A24A"/>')
    o.append(f'<text x="{qx(-3.1)}" y="{qy(2.1)-8}" fill="#C9A24A" font-size="10" text-anchor="middle" {FONT}>relógio 2,1</text>')
    o.append(f'<line x1="{qx(5)}" y1="{qy(4.6)}" x2="{qx(7)}" y2="{qy(4.6)}" stroke="#E9DFC8" stroke-width="3"/>')
    o.append(f'<rect x="{qx(11)-3}" y="{qy(3.6+2.2)}" width="5" height="{2*s}" fill="#FFFFFF"/>')
    o.append(f'<text x="{qx(9)}" y="{qy(7.4)}" fill="#FFFFFF" font-size="10" text-anchor="middle" {FONT}>E1/E2 olham a escada</text>')
    for z, hgt, c in ((-14.6, 1.52, '#9B7BB8'), (-13.8, 1.74, '#E5C46A'), (-13.0, 1.98, '#7FB58A')):
        o.append(f'<rect x="{qx(z)-4}" y="{qy(hgt)}" width="8" height="{hgt*s}" rx="4" fill="{c}"/>')
    o.append(f'<text x="{qx(-13.8)}" y="{qy(-0.9)}" fill="#A79FAF" font-size="9" text-anchor="middle" {FONT}>Cida · Tiago · Marcelo</text>')
    o.append(f'<text x="{qx(-22.6)}" y="{qy(0)+16}" fill="#A79FAF" font-size="11" {FONT}>rua (sul)</text>')
    for y, l in ((0, 'Y 0'), (3.6, 'Y 3,6'), (6.8, 'Y 6,8')):
        o.append(f'<line x1="{qx(19.2)}" y1="{qy(y)}" x2="{w}" y2="{qy(y)}" stroke="#3a3442" stroke-dasharray="3 3"/>')
        o.append(f'<text x="{w-6}" y="{qy(y)-4}" fill="#A79FAF" font-size="10" text-anchor="end" {FONT}>{l}</text>')
    o.append(f'<text x="8" y="18" fill="#FFC98A" font-size="12" font-weight="700" {FONT}>CORTE NORTE–SUL em X = 0 · rua à esquerda, fundos (morro) à direita</text>')
    o.append('</svg>'); return '\n'.join(o)

OUT = sys.argv[1] if len(sys.argv) > 1 else '.'
open(os.path.join(OUT, 'planta_terreo.svg'), 'w', encoding='utf-8').write(plan_ground())
open(os.path.join(OUT, 'planta_cima.svg'), 'w', encoding='utf-8').write(plan_upper())
open(os.path.join(OUT, 'corte_norte_sul.svg'), 'w', encoding='utf-8').write(section())
print('ok')
