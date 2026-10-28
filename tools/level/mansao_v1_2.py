# Mansão do Espelho V1.2 (Fase 1), fiel à arte de referência: casa na encosta.
# Térreo em Y 0; os fundos (Z >= 7) ficam em cima do morro, no nível Y 3,6.
#
# Rodar: python tools/level/mansao_v1_2.py [--json]
#   → confere sobreposição, portas na parede comum, apoio do nível de cima e mede a distância
#     andando de cada cômodo até o caminhão.
#   --json → grava tools/level/mansao_v1_2.json, que o construtor do Unity lê
#     (MirrorHouseBuilder.Mansao.cs): cômodos, paredes módulo a módulo, pisos, lajes, escadas,
#     luzes, pilhas e móveis. Mudou a planta aqui → rode de novo e recrie a cena.
# Ver LEVEL_DESIGN_FASE1_MANSAO.md.
import itertools, heapq, math, json, os, sys

HILL_Z = 7                 # do Z 7 para o norte, o chão é o morro (Y 3,6)
UP_Y = 3.6                 # piso do nível de cima: pé-direito 3,2 + laje 0,4
WALL_H = 3.2

R = {  # id: (nome, nivel, x0, x1, z0, z1)
 'EXT': ('Calçada / escadaria da frente', 0, -6, 22, -21, -17),
 '1':   ('Entrada / Spawn', 0, -4, 4, -17, -11),
 '2':   ('Grande Hall + Relógio', 0, -4, 4, -11, -1),
 '3':   ('Escadaria', 0, -4, 4, -1, 7),
 'CO':  ('Corredor Oeste', 0, -6, -4, -11, 1),
 'COE': ('Escada de Serviço', 0, -6, -4, 1, 7),
 '8':   ('Sala de Jantar', 0, -16, -6, -3, 7),
 '9':   ('Cozinha', 0, -16, -6, -13, -3),
 '10':  ('Sala dos Retratos', 0, 4, 14, -1, 7),
 '11':  ('Sala de Música', 0, 4, 14, -9, -1),
 '12':  ('Depósito de Props', 0, 4, 14, -17, -9),
 'CL':  ('Corredor de Saída', 0, 14, 16, -9, 1),
 'CLE': ('Galeria Leste (descida)', 0, 14, 16, 1, 7),
 '14':  ('Saída / Caminhão', 0, 14, 22, -17, -9),
 'P':   ('Patamar', 1, -4, 4, 5, 7),
 '4':   ('Galeria dos Espelhos', 1, -6, 16, 7, 11),
 '7':   ('Biblioteca', 1, -16, -6, 7, 15),
 '5':   ('Salão de Baile / Palco', 1, -6, 4, 11, 19),
 '6':   ('Camarins / Figurinos', 1, 4, 14, 11, 19),
 '13':  ('Câmara Secreta do Espelho', 1, 14, 20, 11, 19),
}
EXTERIOR = {'EXT', '14'}            # sem paredes nem forro
PLATFORM = {'P'}                    # piso dentro do volume de outro cômodo (sem paredes)
DOUBLE = {'2', '3', 'COE', 'CLE'}   # pé-direito duplo (6,8 m): ocupam os dois níveis
FLOOR = {'1': 'xadrez', '2': 'xadrez', '3': 'xadrez', '9': 'xadrez'}  # o resto é tábua

D = [  # (a, b, largura, x, z, tipo)   tipo "aberto"/"arco"/"vao": vão sem parede
 ('EXT','1',1.6,0,-17,'dupla'), ('1','2',4.0,0,-11,'arco'), ('1','12',1.6,4,-14,'dupla'),
 ('2','3',8.0,0,-1,'aberto'), ('2','CO',1.6,-4,-6,'dupla'), ('3','CO',1.2,-4,0,'simples'),
 ('CO','COE',2.0,-5,1,'aberto'), ('CO','8',1.6,-6,-1,'dupla'), ('CO','9',1.2,-6,-7,'simples'),
 ('8','9',1.6,-11,-3,'dupla'), ('2','11',1.6,4,-5,'dupla'), ('3','10',1.2,4,3,'simples'),
 ('10','11',1.2,9,-1,'simples'), ('11','12',1.6,9,-9,'dupla'), ('12','14',2.0,14,-13,'vao'),
 ('CL','14',1.6,15,-9,'dupla'), ('CL','11',1.2,14,-5,'simples'), ('CL','CLE',2.0,15,1,'aberto'),
 ('EXT','14',4.0,18,-17,'vao'),
 ('3','4',8.0,0,7,'aberto'), ('P','4',8.0,0,7,'aberto'), ('COE','4',2.0,-5,7,'aberto'),
 ('CLE','4',2.0,15,7,'aberto'), ('4','7',1.6,-6,9,'dupla'), ('4','5',1.6,-4,11,'dupla'),
 ('4','6',1.6,9,11,'dupla'), ('7','5',1.2,-6,13,'simples'), ('5','6',1.2,4,15,'simples'),
 ('6','13',1.2,14,15,'espelho-porta'),
]
OPEN_KINDS = {'aberto', 'arco', 'vao'}
STAIRS = [  # (nome, de(sala, x, z), até(sala, x, z), largura, avanço)  todas sobem para +Z
 ('Escadaria principal', ('3', 0, -1), ('P', 0, 5), 3.0, 6.0),
 ('Escada de serviço oeste', ('COE', -5, 1), ('4', -5, 7), 2.0, 6.0),
 ('Galeria leste (desce)', ('CLE', 15, 1), ('4', 15, 7), 2.0, 6.0),
]
# Luzes fixas da casa (o resto é escuro: a lanterna é a luz da noite). Velas são só chama: não iluminam.
# (nível, x, z, tipo, nome)   tipo: acesa | falha | apagada (só acende num evento) | luar | ciano
LUZES = [
 (0, -2.6, -17.6, 'acesa', 'lampião do portão'), (0, 2.6, -17.6, 'acesa', 'lampião do portão'),
 (0, 18, -9.6, 'acesa', 'faróis do caminhão'), (0, -3.7, -8, 'falha', 'arandela do hall'),
 (0, 0, -6.5, 'apagada', 'lustre (só na aparição)'), (0, 13.7, 3, 'falha', 'arandela dos retratos'),
 (0, 9, -13, 'falha', 'lâmpada nua do depósito'), (0, -15.7, 2, 'luar', 'janela do jantar'),
 (1, 12, 7.3, 'falha', 'arandela da galeria (ruído nº 3)'), (1, 0, 6, 'luar', 'clarabóia'),
 (1, -11, 10, 'apagada', 'luminária verde (clique)'), (1, 0, 6.6, 'apagada', 'refletor do espetáculo (só na aparição)'),
 (1, 6.5, 18.6, 'acesa', 'lâmpada de camarim'),
 (1, 11, 18.6, 'falha', 'lâmpada de camarim'), (1, 19.6, 15, 'ciano', 'portal'),
]
# Pilhas: fixas (à vista) e escondidas (caem de algo que tomba ou estão numa gaveta).
# (nível, x, z, tipo[, altura]): sem altura, ficam no chão ou no que estiver embaixo.
# As escondidas aparecem com uma QUEDA (a lista QUEDAS diz qual: o campo "pilha").
PILHAS = [
 (0, -14.6, -8, 'fixa'), (0, -5, -9.5, 'fixa'), (0, 5, 5.5, 'fixa'), (0, 12.5, -15, 'fixa'),
 (1, -3, 18.2, 'fixa'), (0, 15, -6, 'fixa'),
 (0, -15.25, 0, 'escondida', 0.66), (0, 8, -4.2, 'escondida'), (1, -14.8, 10.9, 'escondida'), (1, 13, 13, 'escondida'),
]
# A lista do chefe: (nome, prefab, nível, x, z, giro, etapa). O prefab é um nome do construtor do Unity.
MOVEIS = [
 ('Cadeira Austríaca A', 'cadeira', 0, -14.0, -1.8, 0, 0), ('Cadeira Austríaca B', 'cadeira', 0, -8.5, -1.8, 0, 0),
 ('Talha de Barro', 'talha', 0, -12.0, -10.5, 0, 0),
 ('Caixa de Figurinos', 'caixaP', 0, 6.5, -16.0, 0, 0), ('Caixa de Cenário', 'caixaG', 0, 9.0, -16.0, 0, 0),
 ('Baú-camarim de Turnê', 'bau', 0, 11.8, -16.0, 0, 0), ('Poltrona Coberta', 'poltrona', 0, 11.0, 4.5, 200, 0),
 ('Banqueta da Pianola', 'cadeira', 0, 8.4, -4.6, 0, 0),
 ('Mesa de Leitura', 'mesaLeitura', 1, -11.0, 12.0, 0, 1), ('Cadeira de Plateia', 'cadeira', 1, 0.0, 14.0, 180, 1),
 ('Armário de Figurino', 'armario', 1, 9.0, 18.2, 180, 1), ('Penteadeira de Camarim', 'penteadeira', 1, 6.5, 18.4, 180, 1),
 ('Arara de Figurino', 'arara', 1, 12.4, 14.0, 90, 1),
 ('Cartaz de 1927', 'cartaz', 1, 17.0, 12.5, 180, 2),
]
# Espelhos (§5.3): (id, nível, x, z, giro, tamanho, tipo). Ficam na linha da parede; o giro é para
# onde o vidro olha (0 = +Z). Tamanho P/M/G (0,6 / 1,2 / 2,0 m), "porta" (o E9) ou "portal".
# tipo: heroi | comum | porta | portal. Regra: pelo menos 4 m entre espelhos do mesmo nível (R09).
ESPELHOS = [
 ('E1', 1, -2.0, 11, 180, 'G', 'heroi'), ('E2', 1, 2.0, 11, 180, 'G', 'heroi'),
 ('E3', 1, 7.0, 11, 180, 'M', 'comum'), ('E4', 0, 16, -3.0, -90, 'M', 'comum'),
 ('E5', 1, -6, 17.0, 90, 'G', 'comum'), ('E6a', 1, 6.5, 19, 180, 'P', 'comum'),
 ('E6b', 1, 11.0, 19, 180, 'P', 'comum'), ('E7', 0, -6, -10.0, 90, 'G', 'comum'),
 ('E8', 0, 9.0, 7, 180, 'M', 'comum'), ('E9', 1, 14, 15.0, -90, 'porta', 'porta'),
 ('E10', 0, -4, -14.0, 90, 'P', 'comum'), ('E11', 0, 12.0, -9, 0, 'M', 'comum'),
 ('E12', 0, -11.0, 7, 180, 'M', 'comum'), ('PORTAL', 1, 20, 15.0, -90, 'portal', 'portal'),
]
# O Imitador começa no E1 (a aparição sai dele e volta pelo E2); o palco é debaixo do lustre.
IMITADOR = {'espelho': 'E1', 'saidaDaAparicao': 'E2', 'palco': (0.0, -6.5)}
# Fontes de ruído (a Cida afina): (prefab, nível, x, z, giro)
RUIDO = [('piano', 0, 9.25, -5.7, 0), ('gramofone', 1, -5.3, 12.0, 45)]
# Âncoras sonoras (§5.2): (nome, som, nível, x, altura, z). O relógio, o piano, o gramofone e a
# arandela da galeria trazem o próprio som; o rádio fica na calçada.
ANCORAS = [
 ('Torneira da Cozinha', 'torneira_pingando', 0, -15.4, 1.0, -8.0),
 ('Lustre da Sala de Jantar', 'lustre_tilintando', 0, -11.0, 2.5, 1.8),
 ('Vento na Clarabóia', 'vento_janelas', 1, 0.0, 3.0, 6.0),
 ('Vento na Janela dos Retratos', 'vento_janelas', 0, 6.0, 1.6, 6.8),
 ('Cabides dos Camarins', 'cabides_metal', 1, 12.4, 1.6, 14.0),
 ('Caminhão em Marcha Lenta', 'caminhao_motor', 0, 18.5, 1.0, -15.5),
 ('Mecanismo da Pianola', 'pianola_mecanismo', 0, 9.25, 0.6, -5.5),
]
# Piso que range (passo vira rangido de 7 m): (nome, nível, x, z, largura, profundidade)
RANGE = [('Biblioteca', 1, -11, 11, 10, 8), ('Corredor de Saída', 0, 15, -4, 2, 6), ('Coxia', 1, 4, 15, 2, 2)]
# Cenário e abandono: (prefab, nível, x, z, giro, altura, colide). "colide" = tem colisão
# (não pode ficar na frente de porta). Teias de canto vêm sozinhas: uma em dois cantos de cada cômodo.
CENARIO = [
 # Jantar: a mesa posta para 9 (duas mesas emendadas) e as cadeiras que ficam
 ('mesaJantar', 0, -11.0, 2.5, 90, 0, True), ('mesaJantar', 0, -11.0, 0.5, 90, 0, True),
 ('cadeiraJantar', 0, -11.75, 3.0, 90, 0, True), ('cadeiraJantar', 0, -10.25, 3.0, -90, 0, True),
 ('cadeiraJantar', 0, -11.75, 0.0, 90, 0, True), ('cadeiraJantar', 0, -10.25, 0.0, -90, 0, True),
 ('cadeiraTombada', 0, -9.6, 1.4, 20, 0, False), ('cadeiraJantar', 0, -11.0, 4.1, 180, 0, True),
 ('papeis', 0, -14.0, 5.5, 60, 0, False),
 # Cozinha: a pia, o armário encostado, caixotes
 ('pia', 0, -15.6, -8.0, 90, 0, True), ('estante', 0, -15.6, -11.5, 90, 0, True),
 ('caixote', 0, -7.6, -12.4, 10, 0, True), ('caixote', 0, -7.6, -12.4, -15, 0.4, False),
 ('fogao', 0, -11.0, -12.6, 0, 0, True), ('bancada', 0, -14.4, -3.35, 180, 0, True),
 ('aparador', 0, -15.55, 0.0, 90, 0, True),
 # Retratos: a família na parede, a cômoda coberta
 ('retratoG', 0, 4, 0.8, 90, 1.2, False), ('retratoM', 0, 4, 5.6, 90, 1.4, False),
 ('retratoP', 0, 6.0, -1, 0, 1.6, False), ('retratoM', 0, 14, 0.6, -90, 1.4, False),
 ('retratoG', 0, 14, 5.6, -90, 1.2, False), ('comodaCoberta', 0, 8.0, 6.4, 180, 0, True),
 ('janela', 0, 6.0, 7, 180, 1.0, False),
 # Música e Depósito
 ('papeis', 0, 6.0, -7.5, 0, 0, False), ('comodaCoberta', 0, 4.6, -12.0, 90, 0, True),
 ('caixote', 0, 13.4, -10.0, 30, 0, True), ('entulho', 0, 6.5, -10.2, 15, 0, False),
 # Galeria Leste: os retratos de rosto raspado
 ('retratoM', 0, 16, -6.0, -90, 1.4, False), ('retratoP', 0, 16, 0.0, -90, 1.6, False),
 # Entrada e hall
 ('papeis', 0, 1.2, -13.5, 0, 0, False), ('papelDescascando', 0, 2.8, -11, 0, 0.9, False),
 # Galeria dos Espelhos: os manequins (o do meio tomba aos 2:40) e a caliça
 ('manequimB', 1, 5.0, 10.55, 180, 0, True), ('manequimQueTomba', 1, 11.6, 10.55, 180, 0, True),
 ('manequimC', 1, 13.6, 10.55, 180, 0, True), ('entulho', 1, -2.5, 7.6, -10, 0, False),
 # Patamar: velas na frente dos espelhos-herói (só chama)
 ('velas', 1, -3.0, 6.4, 0, 0, False), ('velas', 1, 3.0, 6.4, 0, 0, False),
 # Biblioteca: estantes nas paredes, pilhas de livros
 ('estante', 1, -14.6, 14.6, 180, 0, True), ('estante', 1, -13.0, 14.6, 180, 0, True),
 ('estante', 1, -11.4, 14.6, 180, 0, True), ('estante', 1, -9.8, 14.6, 180, 0, True),
 ('estante', 1, -15.6, 9.0, 90, 0, True), ('estante', 1, -15.6, 10.6, 90, 0, True),
 ('caixote', 1, -11.0, 10.0, 0, 0, True),  # a mesinha da luminária verde
 ('pilhaLivros', 1, -8.2, 8.0, 20, 0, False), ('pilhaLivros', 1, -13.5, 9.5, -40, 0, False),
 # Salão: os 4 manequins com figurino e a plateia abandonada
 ('manequimA', 1, -4.6, 16.6, 180, 0, True), ('manequimB', 1, -3.8, 16.6, 180, 0, True),
 ('manequimA', 1, 1.6, 16.6, 180, 0, True), ('manequimB', 1, 2.4, 16.6, 180, 0, True),
 ('poltronaCoberta', 1, 2.6, 12.0, 200, 0, True), ('velas', 1, -1.0, 16.9, 0, 0, False),
 # Camarins e Câmara
 ('comodaCoberta', 1, 13.4, 12.0, -90, 0, True), ('papeis', 1, 7.0, 13.0, 120, 0, False),
 ('velas', 1, 19.0, 13.2, 0, 0, False), ('papeis', 1, 16.0, 17.6, 30, 0, False),
]
# ---------- Etapa 3: atmosfera e época (§6 a §9) ----------
# Achados de época (§8.2, D10): bônus fora da lista do chefe. (nome, prefab, nível, x, z, giro)
# O refletor e o gramofone ficam para depois; o espelho de pé ainda não é esconderijo (D11).
ACHADOS = [
 ('Cristaleira', 'cristaleira', 0, -6.45, 3.5, -90), ('Telefone de Parede', 'telefone', 0, 3.7, -15.6, -90),
 ('Cadeira de Balanço', 'cadeiraBalanco', 0, 12.6, 1.6, -110), ('Rolos de Pianola', 'rolos', 0, 10.6, -6.0, 0),
 ('Cofre da Bilheteria', 'cofre', 0, 13.4, -11.2, -90), ('Máquina de Escrever', 'maquinaEscrever', 1, -8.0, 14.2, 180),
 ('Lanterna Mágica', 'lanternaMagica', 1, -13.8, 7.8, 30), ('Espelho de Pé', 'psiche', 1, 3.45, 18.4, 180),
 ('Máquina de Costura', 'maquinaCostura', 1, 4.8, 13.0, 90), ('Biombo de Camarim', 'biombo', 1, 13.0, 17.6, 200),
]
# Lampião de querosene (D12): luz de 4 m que ocupa as mãos. (nível, x, z)
LAMPIOES = [(0, -7.4, -9.6), (0, 5.2, -11.2)]
# A casa reage (§9): o que cai, toca ou balança. Campos:
#   modelo   o que se mexe: um prefab de cenário, "achado:<nome>" (o modelo do achado) ou "" (só som)
#   gatilho  correndo | movel (um móvel carregado perto) | perto (passar perto, com chance) | segundo | ninguemOlhando
#   mov      drop (cai no chão) | slide (gaveta) | swing (balança) | "" ; dir = para onde, no espaço do modelo
#   pilha    (x, z) da pilha escondida que esta queda larga; susto False = não é susto (só revela)
QUEDAS = [
 dict(nome='Livros da estante (oeste)', modelo='livrosSoltos', nivel=1, x=-15.45, z=10.6, giro=90, altura=1.25,
      gatilho='correndo movel', raio=1.8, som='livros_caem', alcance=10, mov='drop', dir=(0, 0, 0.5),
      pilha=(-14.8, 10.9), aviso='Livros caíram da estante na Biblioteca.'),
 dict(nome='Livros da estante (norte)', modelo='livrosSoltos', nivel=1, x=-13.0, z=14.45, giro=180, altura=1.25,
      gatilho='correndo movel', raio=1.8, som='livros_caem', alcance=10, mov='drop', dir=(0, 0, 0.5),
      aviso='Livros caíram da estante na Biblioteca.'),
 dict(nome='Pilha de pratos', modelo='pratos', nivel=0, x=-14.4, z=-3.4, giro=180, altura=0.9,
      gatilho='movel', raio=1.8, som='pratos_quebram', alcance=12, mov='drop', dir=(0, 0, 0.5),
      aviso='Pratos se espatifaram na Cozinha.'),
 dict(nome='Retrato da Galeria Leste', modelo='retratoM', nivel=0, x=16, z=4.0, giro=-90, altura=3.2,
      gatilho='movel', raio=2.5, som='quadro_cai', alcance=9, mov='drop', dir=(0, 0, 0.6),
      aviso='Um retrato despencou na Galeria Leste.'),
 dict(nome='Caixa do alto da pilha', modelo='caixote', nivel=0, x=13.4, z=-10.0, giro=0, altura=0.4,
      gatilho='perto', raio=3.0, chance=0.6, som='caixa_despenca', alcance=10, mov='drop', dir=(-0.6, 0, 0),
      aviso='Uma caixa despencou no Depósito.'),
 dict(nome='Cadeira de balanço', modelo='achado:Cadeira de Balanço', gatilho='ninguemOlhando', raio=7.0,
      som='cadeira_balanco', alcance=5, mov='swing', aviso='A cadeira de balanço está balançando.'),
 dict(nome='Tampa da pianola', modelo='', nivel=0, x=9.25, z=-5.7, altura=0.9, gatilho='correndo', raio=1.8,
      som='piano_tampa_bate', alcance=10, aviso='A tampa do piano bateu na Sala de Música.'),
 dict(nome='Gaveta do aparador', modelo='gaveta', nivel=0, x=-15.55, z=0.0, giro=90, altura=0.62,
      gatilho='segundo', segundo=90, som='gaveta_abre', alcance=5, mov='slide', dir=(0, 0, 0.28),
      pilha=(-15.25, 0.0), aviso='Uma gaveta abriu sozinha na Sala de Jantar.'),
 dict(nome='Telefone de parede', modelo='achado:Telefone de Parede', gatilho='segundo', segundo=120,
      som='telefone_manivela', alcance=14, mov='swing', aviso='Um telefone tocou na Entrada.'),
 dict(nome='Porta da frente', modelo='', nivel=0, x=14.0, z=-13.0, altura=1.0, gatilho='movel', raio=1.5,
      somEm=(0.0, -17.0), som='porta_bate', alcance=12, aviso='A porta da frente bateu.'),
 dict(nome='Velas do patamar', modelo='', nivel=1, x=0.0, z=6.4, altura=0.2, gatilho='correndo', raio=3.2,
      som='vela_apaga', alcance=2, aviso='Um sopro passou pelas velas.'),
 dict(nome='Velas do salão', modelo='', nivel=1, x=-1.0, z=16.9, altura=0.6, gatilho='correndo', raio=1.5,
      som='vela_apaga', alcance=2, aviso='Um sopro passou pelas velas.'),
 dict(nome='Máquina de escrever', modelo='achado:Máquina de Escrever', gatilho='perto', raio=2.5, chance=1.0,
      som='maquina_escrever', alcance=8, aviso='As teclas batem sozinhas: S-U-P-L-E-N-T-E.'),
 dict(nome='Máquina de costura', modelo='achado:Máquina de Costura', gatilho='ninguemOlhando', raio=6.0,
      som='maquina_costura_pedal', alcance=6, aviso='O pedal da máquina de costura range sozinho.'),
 dict(nome='Atrás da banqueta', modelo='', nivel=0, x=8.0, z=-4.2, altura=0.2, gatilho='movel', raio=1.0,
      susto=False, pilha=(8.0, -4.2), aviso='Tinha pilhas atrás da banqueta.'),
 dict(nome='Bolso da arara', modelo='', nivel=1, x=13.0, z=13.0, altura=0.2, gatilho='movel', raio=1.2,
      susto=False, pilha=(13.0, 13.0), aviso='Pilhas num bolso de casaco da arara.'),
]
# Teias de vão (§6.3): portas pouco usadas que começam fechadas por teia (só jogador rasga).
TEIAS_VAO = [('3', 'CO'), ('CO', '9'), ('10', '11'), ('CL', '11'), ('7', '5'), ('5', '6'), ('3', '10'), ('8', '9')]

NASCIMENTO = [(-1.2, -19.3), (0.0, -19.3), (1.2, -19.3)]
CAMINHAO = (18.5, -13.0, 180)       # x, z, giro (a caçamba virada para o corredor de saída)
RELOGIO = (-3.6, -3.0, 90)       # na parede oeste do hall: no eixo, travaria o armário descendo a escadaria


def area(r): return (r[3]-r[2])*(r[5]-r[4])
def levels(k):
    return (0, 1) if k in DOUBLE else (R[k][1],)
def edge(r, x, z):
    inside = r[2]-1e-6 <= x <= r[3]+1e-6 and r[4]-1e-6 <= z <= r[5]+1e-6
    return inside and (min(abs(x-r[2]), abs(x-r[3]), abs(z-r[4]), abs(z-r[5])) < 1e-6)


def check():
    ok = True
    for a, b in itertools.combinations(R, 2):
        if not set(levels(a)) & set(levels(b)) or {a, b} & PLATFORM: continue
        A, B = R[a], R[b]
        if min(A[3],B[3])-max(A[2],B[2]) > 1e-6 and min(A[5],B[5])-max(A[4],B[4]) > 1e-6: print('SOBREPÕE', a, b); ok = False
    for d in D:
        if not (edge(R[d[0]], d[3], d[4]) and edge(R[d[1]], d[3], d[4])): print('PORTA FORA DA PAREDE COMUM', d); ok = False
        if not set(levels(d[0])) & set(levels(d[1])): print('PORTA ENTRE NÍVEIS SEM CÔMODO DUPLO', d); ok = False
    for k, r in R.items():   # apoio do nível de cima: sobre o morro (Z >= 7) ou sobre um cômodo do térreo
        if r[1] != 1: continue
        sup = max(0, r[5]-max(r[4], HILL_Z))*(r[3]-r[2])
        for g in R.values():
            if g[1] == 0:
                ox = min(r[3],g[3])-max(r[2],g[2]); oz = min(r[5],g[5],HILL_Z)-max(r[4],g[4])
                if ox > 0 and oz > 0: sup += ox*oz
        if sup + 1e-6 < area(r): print('SEM APOIO', k, sup, area(r)); ok = False
    for k, r in R.items():
        if r[1] == 0 and r[5] > HILL_Z + 1e-6: print('TÉRREO DENTRO DO MORRO', k); ok = False
    for m in MOVEIS:   # todo móvel dentro de um cômodo do nível dele
        if room_at(m[3], m[4], m[2], interior=False) is None: print('MÓVEL FORA DA CASA', m[0]); ok = False
    # R09: espelhos de esconderijo a pelo menos 4 m um do outro no mesmo nível
    hide = [e for e in ESPELHOS if e[6] in ('heroi', 'comum')]
    for a, b in itertools.combinations(hide, 2):
        if a[1] == b[1] and math.dist(a[2:4], b[2:4]) < 4 - 1e-6: print('ESPELHOS PERTO DEMAIS', a[0], b[0]); ok = False
    # nada com colisão a menos de 1,3 m do meio de uma porta (no mesmo nível), e espelho fora do vão
    doors = [(d[3], d[4], set(levels(d[0])) & set(levels(d[1]))) for d in D if d[0] not in PLATFORM and d[1] not in PLATFORM]
    for c in CENARIO:
        if c[6] and any(c[1] in lv and math.dist((c[2], c[3]), (x, z)) < 1.3 for x, z, lv in doors):
            print('CENÁRIO NA FRENTE DA PORTA', c); ok = False
    for nome, _, lv, x, z, _ in ACHADOS:
        if room_at(x, z, lv, interior=False) is None: print('ACHADO FORA DA CASA', nome); ok = False
        if any(lv in dl and math.dist((x, z), (dx, dz)) < 1.3 for dx, dz, dl in doors):
            print('ACHADO NA FRENTE DA PORTA', nome); ok = False
    for lv, x, z in LAMPIOES:
        if room_at(x, z, lv, interior=False) is None: print('LAMPIÃO FORA DA CASA', x, z); ok = False
    hidden = [(p[1], p[2]) for p in PILHAS if p[3] == 'escondida']
    linked = [q['pilha'] for q in QUEDAS if 'pilha' in q]
    for h in hidden:
        if h not in linked: print('PILHA ESCONDIDA SEM QUEDA', h); ok = False
    names = {a[0] for a in ACHADOS}
    for q in QUEDAS:
        if q['modelo'].startswith('achado:') and q['modelo'][7:] not in names: print('QUEDA SEM ACHADO', q['nome']); ok = False
    for a, b in TEIAS_VAO:
        if not any({a, b} == {d[0], d[1]} for d in D): print('TEIA SEM PORTA', a, b); ok = False
    for e in ESPELHOS:
        if e[6] in ('porta',): continue
        if any(e[1] in lv and math.dist(e[2:4], (x, z)) < 1.0 for x, z, lv in doors):
            print('ESPELHO NO VÃO DA PORTA', e[0]); ok = False
    return ok


def room_at(x, z, level, interior=True):
    for k, r in R.items():
        if interior and (k in EXTERIOR or k in PLATFORM): continue
        if level in levels(k) and r[2] < x < r[3] and r[4] < z < r[5]: return k
    return None


# ---------- Paredes: módulo a módulo, a partir dos cômodos e das portas ----------
# Cada aresta é dividida em pedaços de 1 m. Em cada pedaço e em cada nível: de um lado e do
# outro há cômodos internos? Dois → parede compartilhada (feita uma vez). Um → parede de fora.
# As portas viram módulos de 2 m centrados nelas; os vãos ("aberto") ficam sem parede.
def walls():
    units = {}   # (eixo, linha, a, nível) -> dono
    for k, r in R.items():
        if k in EXTERIOR or k in PLATFORM: continue
        x0, x1, z0, z1 = r[2], r[3], r[4], r[5]
        sides = [('z', z0, x0, x1, 0), ('z', z1, x0, x1, 180), ('x', x0, z0, z1, 90), ('x', x1, z0, z1, -90)]
        for axis, line, a0, a1, yaw in sides:
            for lv in levels(k):
                for a in range(int(a0), int(a1)):
                    key = (axis, line, a, lv)
                    prev = units.get(key)
                    # o dono é o de pé-direito duplo (a parede dele vai até 6,8); empate: o primeiro
                    if prev is None or (k in DOUBLE and prev[0] not in DOUBLE):
                        units[key] = (k, yaw)
    # vãos e portas em cada linha
    openings = {}  # (eixo, linha, nível) -> [(a0, a1, tipo)]
    for d in D:
        a, b, w, x, z, kind = d
        if a in PLATFORM or b in PLATFORM: continue
        ra, rb = R[a], R[b]
        axis = 'x' if any(abs(x - v) < 1e-6 for v in (ra[2], ra[3])) and not any(abs(z - v) < 1e-6 for v in (ra[4], ra[5])) else 'z'
        line, c = (x, z) if axis == 'x' else (z, x)
        lv = sorted(set(levels(a)) & set(levels(b)))
        for l in lv:
            if kind in OPEN_KINDS: span = (c - w / 2, c + w / 2, 'vao')
            else: span = (c - 1, c + 1, 'dupla' if w >= 1.5 else 'porta')
            openings.setdefault((axis, line, l), []).append(span)
    mods = []
    lines = {}
    for (axis, line, a, lv), owner in units.items():
        lines.setdefault((axis, line, lv), {})[a] = owner
    for (axis, line, lv), us in lines.items():
        ops = openings.get((axis, line, lv), [])
        def kind_of(a):
            for o0, o1, t in ops:
                if o0 - 1e-6 <= a and a + 1 <= o1 + 1e-6: return t, o0
            return 'lisa', None
        a_sorted = sorted(us)
        i = 0
        while i < len(a_sorted):
            a = a_sorted[i]; owner = us[a]; t, o0 = kind_of(a)
            if t == 'vao': i += 1; continue
            if t in ('porta', 'dupla'):
                mods.append(module(axis, line, lv, o0, 2, t, owner)); i += 2; continue
            j = i   # sequência de paredes lisas do mesmo dono
            while j < len(a_sorted) and a_sorted[j] == a + (j - i) and kind_of(a_sorted[j])[0] == 'lisa' and us[a_sorted[j]] == owner:
                j += 1
            n = j - i; s = a
            while n >= 2: mods.append(module(axis, line, lv, s, 2, 'lisa', owner)); s += 2; n -= 2
            if n == 1: mods.append(module(axis, line, lv, s, 1, 'lisa', owner))
            i = j
    return mods


def module(axis, line, lv, start, length, kind, owner):
    k, yaw = owner
    c = start + length / 2
    x, z = (line, c) if axis == 'x' else (c, line)
    if k in DOUBLE:   # o volume duplo: a fileira de cima começa no forro do térreo e vai até 6,8
        y, sy = (0.0, 1.0) if lv == 0 else (WALL_H, (2 * WALL_H + 0.4 - WALL_H) / WALL_H)
    else:
        y, sy = (0.0, 1.0) if lv == 0 else (UP_Y, 1.0)
    return {'tipo': kind, 'x': x, 'y': y, 'z': z, 'giro': yaw, 'largura': length, 'escalaY': round(sy, 4), 'dono': k}


def slabs():
    """Pisos (ladrilhos de 2 m), lajes de forro e o piso do patamar, cômodo a cômodo."""
    floors, ceilings = [], []
    for k, r in R.items():
        if k in EXTERIOR: continue
        lv = R[k][1]
        fy = 0.0 if lv == 0 else UP_Y
        top = 2 * WALL_H + 0.8 if (k in DOUBLE or lv == 1) else WALL_H + 0.4   # topo da laje do forro
        for x in range(int(r[2]) + 1, int(r[3]), 2):
            for z in range(int(r[4]) + 1, int(r[5]), 2):
                if k in PLATFORM:
                    floors.append({'tipo': 'laje', 'x': x, 'y': fy, 'z': z})
                    floors.append({'tipo': 'tabua', 'x': x, 'y': fy, 'z': z})
                    continue
                stair = any(s[1][0] == k and abs(x - s[1][1]) < s[3] / 2 + 1 and s[1][2] < z < s[2][2] for s in STAIRS)
                floors.append({'tipo': FLOOR.get(k, 'tabua'), 'x': x, 'y': fy, 'z': z})
                if not (k == '3' and 5 < z < 7):   # o forro do patamar é a clarabóia
                    ceilings.append({'tipo': 'laje', 'x': x, 'y': top, 'z': z})
    return floors, ceilings


def distances():
    def cen(k): r = R[k]; return ((r[2]+r[3])/2, (r[4]+r[5])/2)
    adj, pts = {}, {}
    def link(p, q, w): adj.setdefault(p, []).append((q, w)); adj.setdefault(q, []).append((p, w))
    for i, d in enumerate(D):
        for k in d[:2]: link(('r', k), ('d', i), math.dist(cen(k), d[3:5])); pts.setdefault(k, []).append((('d', i), d[3:5]))
    for i, s in enumerate(STAIRS):
        (ka, xa, za), (kb, xb, zb) = s[1], s[2]
        link(('s', i, 0), ('s', i, 1), math.hypot(s[4], 3.6))
        for k, p, n in ((ka, (xa, za), ('s', i, 0)), (kb, (xb, zb), ('s', i, 1))):
            link(('r', k), n, math.dist(cen(k), p)); pts.setdefault(k, []).append((n, p))
    for k, lst in pts.items():
        for (n1, p1), (n2, p2) in itertools.combinations(lst, 2): link(n1, n2, math.dist(p1, p2))
    dist = {('r', '14'): 0}; pq = [(0, ('r', '14'))]
    while pq:
        d0, u = heapq.heappop(pq)
        if d0 > dist[u]: continue
        for v, w in adj.get(u, []):
            if d0 + w < dist.get(v, 1e9): dist[v] = d0 + w; heapq.heappush(pq, (dist[v], v))
    return {k: dist.get(('r', k)) for k in R}


def queda(q):
    d = {'nome': q['nome'], 'modelo': q['modelo'], 'nivel': q.get('nivel', 0), 'x': q.get('x', 0), 'z': q.get('z', 0),
         'giro': q.get('giro', 0), 'altura': q.get('altura', 0), 'gatilho': q['gatilho'], 'raio': q.get('raio', 1.5),
         'chance': q.get('chance', 0.5), 'segundo': q.get('segundo', 0), 'som': q.get('som', ''), 'alcance': q.get('alcance', 10),
         'mov': q.get('mov', ''), 'aviso': q.get('aviso', ''), 'susto': q.get('susto', True)}
    dx, dy, dz = q.get('dir', (0, 0, 0))
    d.update({'dirX': dx, 'dirY': dy, 'dirZ': dz})
    px, pz = q.get('pilha', (0, 0))
    d.update({'temPilha': 'pilha' in q, 'pilhaX': px, 'pilhaZ': pz})
    sx, sz = q.get('somEm', (0, 0))
    d.update({'temSomEm': 'somEm' in q, 'somEmX': sx, 'somEmZ': sz})
    return d


def teia(a, b):
    """A porta entre a e b: onde fica, de que largura, e se a parede corre em X (giro 0) ou em Z (giro 90)."""
    d = next(d for d in D if {a, b} == {d[0], d[1]})
    x, z = d[3], d[4]
    ra = R[d[0]]
    along_x = abs(z - ra[4]) < 1e-6 or abs(z - ra[5]) < 1e-6
    lv = min(set(levels(d[0])) & set(levels(d[1])))
    return {'nivel': lv, 'x': x, 'z': z, 'largura': d[2], 'giro': 0 if along_x else 90}


def export(path):
    floors, ceilings = slabs()
    data = {
        'nivelDeCima': UP_Y, 'alturaParede': WALL_H, 'morroZ': HILL_Z,
        'comodos': [{'id': k, 'nome': r[0], 'nivel': r[1], 'x0': r[2], 'x1': r[3], 'z0': r[4], 'z1': r[5],
                     'duplo': k in DOUBLE, 'exterior': k in EXTERIOR, 'plataforma': k in PLATFORM} for k, r in R.items()],
        'paredes': walls(), 'pisos': floors, 'forros': ceilings,
        'escadas': [{'nome': s[0], 'x': s[1][1], 'z': s[1][2], 'largura': s[3], 'avanco': s[4]} for s in STAIRS],
        'luzes': [{'nivel': l[0], 'x': l[1], 'z': l[2], 'tipo': l[3], 'nome': l[4]} for l in LUZES],
        'pilhas': [{'nivel': p[0], 'x': p[1], 'z': p[2], 'tipo': p[3], 'altura': p[4] if len(p) > 4 else -1} for p in PILHAS],
        'moveis': [{'nome': m[0], 'prefab': m[1], 'nivel': m[2], 'x': m[3], 'z': m[4], 'giro': m[5], 'etapa': m[6]} for m in MOVEIS],
        'espelhos': [{'id': e[0], 'nivel': e[1], 'x': e[2], 'z': e[3], 'giro': e[4], 'tamanho': e[5], 'tipo': e[6]} for e in ESPELHOS],
        'imitador': {'espelho': IMITADOR['espelho'], 'saidaDaAparicao': IMITADOR['saidaDaAparicao'],
                     'palcoX': IMITADOR['palco'][0], 'palcoZ': IMITADOR['palco'][1]},
        'ruido': [{'prefab': r[0], 'nivel': r[1], 'x': r[2], 'z': r[3], 'giro': r[4]} for r in RUIDO],
        'ancoras': [{'nome': a[0], 'som': a[1], 'nivel': a[2], 'x': a[3], 'altura': a[4], 'z': a[5]} for a in ANCORAS],
        'range': [{'nome': r[0], 'nivel': r[1], 'x': r[2], 'z': r[3], 'largura': r[4], 'profundidade': r[5]} for r in RANGE],
        'cenario': [{'prefab': c[0], 'nivel': c[1], 'x': c[2], 'z': c[3], 'giro': c[4], 'altura': c[5], 'colide': c[6]} for c in CENARIO],
        'achados': [{'nome': a[0], 'prefab': a[1], 'nivel': a[2], 'x': a[3], 'z': a[4], 'giro': a[5]} for a in ACHADOS],
        'lampioes': [{'nivel': l[0], 'x': l[1], 'z': l[2]} for l in LAMPIOES],
        'quedas': [queda(q) for q in QUEDAS],
        'teiasVao': [teia(a, b) for a, b in TEIAS_VAO],
        'nascimento': [{'x': x, 'z': z} for x, z in NASCIMENTO],
        'caminhao': {'x': CAMINHAO[0], 'z': CAMINHAO[1], 'giro': CAMINHAO[2]},
        'relogio': {'x': RELOGIO[0], 'z': RELOGIO[1], 'giro': RELOGIO[2]},
    }
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    return data


if __name__ == '__main__':
    ok = check()
    print('geometria ok' if ok else 'PROBLEMAS')
    g0 = sum(area(r) for k, r in R.items() if R[k][1] == 0 and k not in EXTERIOR)
    g1 = sum(area(r) for k, r in R.items() if R[k][1] == 1)
    print(f'área interna: térreo {g0:.0f} m², nível de cima {g1:.0f} m², total {g0+g1:.0f} m²')
    for s in STAIRS: print(f"{s[0]}: {math.degrees(math.atan(3.6/s[4])):.1f}°, {round(3.6/0.18)} degraus, largura {s[3]} m")
    dt = distances()
    print('\ndistância andando até o caminhão (centro do cômodo):')
    for k in R:
        if k != '14': print(f"  {k:>3} {R[k][0]:42} {dt[k]:5.0f} m")
    if '--json' in sys.argv:
        out = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mansao_v1_2.json')
        data = export(out)
        kinds = {}
        for m in data['paredes']: kinds[m['tipo']] = kinds.get(m['tipo'], 0) + 1
        print(f"\nJSON: {out}\n  paredes {len(data['paredes'])} {kinds} · pisos {len(data['pisos'])} · forros {len(data['forros'])}")
    if not ok: sys.exit(1)
