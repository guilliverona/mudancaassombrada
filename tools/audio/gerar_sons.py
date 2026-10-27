"""Gera os sons do jogo a partir do catalogo.json (mesma ideia dos scripts do Blender).

- Sons com "texto": pede à API de efeitos sonoros do ElevenLabs e salva em MP3.
- Sons "procedural": gerados aqui mesmo, por código (WAV), sem IA e sem gastar créditos.
- Sons "externo": arquivos trazidos pela equipe (ex.: a trilha). Ficam no catálogo para a
  biblioteca e a mixagem, mas este script nunca gera nem sobrescreve.
- Sons com "fala": uma VOZ dizendo frases (API de voz do ElevenLabs, a voz escolhida em "voz").
  Cada frase da lista vira uma variação. Ex.: o Imitador sussurrando "minha casa...".
- "tom" (semitons, negativo = mais grave): abaixa o som inteiro, como tocar mais devagar. Serve
  para deixar passos mais sombrios e a voz mais grossa. Sai em .wav, como o "normalizar".

Saída: MudancaAssombrada/Assets/Audio/<pasta>/<id>.mp3 (ou <id>_01.mp3, _02... com variações).
A importação no Unity é configurada pela pasta (Assets/Editor/AudioImportSettings.cs).

A chave fica na variável de ambiente ELEVENLABS_API_KEY (nunca neste arquivo nem no repositório).

Uso (na raiz do repositório):
    python tools/audio/gerar_sons.py                     gera o que ainda não existe
    python tools/audio/gerar_sons.py --pasta Passos      só uma pasta (pode repetir)
    python tools/audio/gerar_sons.py --so apito          só um som (pode repetir)
    python tools/audio/gerar_sons.py --so apito --refazer   gera de novo, por cima
    python tools/audio/gerar_sons.py --listar            mostra o que seria gerado, sem gerar

Só usa a biblioteca padrão do Python (nada para instalar).
"""
import argparse
import json
import math
import os
import random
import struct
import sys
import urllib.error
import urllib.request
import wave

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
CATALOGO = os.path.join(AQUI, "catalogo.json")
SAIDA = os.path.join(RAIZ, "MudancaAssombrada", "Assets", "Audio")

API_URL = "https://api.elevenlabs.io/v1/sound-generation?output_format="
VOZ_URL = "https://api.elevenlabs.io/v1/text-to-speech/{voz}?output_format="
MODELO_VOZ = "eleven_v3"  # fala português e entende marcações como [whispers]
TAXA_VOZ = 24000  # a voz sem compressão em 44,1 kHz é só do plano Pro; 24 kHz sobra para um sussurro
FORMATO_MP3 = "mp3_44100_128"
FORMATO_PCM = "pcm_44100"  # sem compressão: 16 bits, mono (dá para mexer no volume aqui mesmo)
MODELO = "eleven_text_to_sound_v2"  # o modelo que aceita "loop"
TAXA = 44100  # amostras por segundo dos sons procedurais e do PCM
PICO_MAX_DB = -1.0  # a normalização nunca passa disto (sem estourar)


def arquivos_do_som(som):
    """Os caminhos que um som do catálogo gera (1 por variação)."""
    # Procedurais, normalizados, com tom mudado e falas saem em WAV (ajustados aqui, sem perder qualidade).
    ext = ".wav" if som.get("procedural") or em_pcm(som) else ".mp3"
    pasta = os.path.join(SAIDA, som["pasta"])
    n = len(som["fala"]) if "fala" in som else som.get("variacoes", 1)
    if n <= 1:
        return [os.path.join(pasta, som["id"] + ext)]
    return [os.path.join(pasta, f"{som['id']}_{i:02d}{ext}") for i in range(1, n + 1)]


# ---------------------------------------------------------------- ElevenLabs

def em_pcm(som):
    """Pede o áudio sem compressão quando o script ainda vai mexer nele (volume, tom ou fala)."""
    return "normalizar" in som or "tom" in som or "fala" in som


def _pedir(url, corpo, chave):
    req = urllib.request.Request(url, data=json.dumps(corpo).encode("utf-8"), method="POST", headers={
        "xi-api-key": chave, "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            return resp.read()
    except urllib.error.HTTPError as e:
        # Mostra só a mensagem da API (ela nunca devolve a chave).
        raise RuntimeError(f"HTTP {e.code}: {e.read().decode('utf-8', 'replace')[:300]}") from None


def gerar_ia(som, caminho, chave):
    pcm = em_pcm(som)
    dados = _pedir(API_URL + (FORMATO_PCM if pcm else FORMATO_MP3), {
        "text": som["texto"],
        "duration_seconds": som["duracao"],
        "prompt_influence": som.get("influencia", 0.5),
        "loop": som.get("loop", False),
        "model_id": MODELO,
    }, chave)
    if pcm:
        return salvar_pcm(dados, caminho, som)
    with open(caminho, "wb") as f:
        f.write(dados)
    return len(dados)


def gerar_fala(som, frase, caminho, chave):
    """Uma frase falada pela voz do catálogo ("voz" = id da voz no ElevenLabs)."""
    ajustes = som.get("ajustesVoz", {})
    dados = _pedir(VOZ_URL.format(voz=som["voz"]) + f"pcm_{TAXA_VOZ}", {
        "text": frase,
        "model_id": som.get("modelo", MODELO_VOZ),
        "language_code": som.get("idioma", "pt"),
        "voice_settings": {
            "stability": ajustes.get("estabilidade", 0.5),
            "similarity_boost": ajustes.get("semelhanca", 0.75),
            "style": ajustes.get("estilo", 0.0),
        },
    }, chave)
    return salvar_pcm(dados, caminho, som, TAXA_VOZ)


def _db(x):
    return 20 * math.log10(max(x, 1e-9))


def salvar_pcm(pcm, caminho, som, taxa_original=TAXA):
    """Salva o PCM (16 bits, mono) em .wav, ajustando o que o catálogo pedir:

    NORMALIZAR: sobe (ou desce) o volume do arquivo inteiro até o RMS (volume médio) chegar em
    'normalizar' dBFS, sem deixar o pico passar de PICO_MAX_DB. É o que um estúdio faz antes de
    mixar: todo arquivo da mesma camada chega no mesmo nível, e a mixagem (volume no catálogo)
    fica previsível. A IA entrega os ambientes muito baixos (~ -60 dB), por isso existe isto.

    TOM: grava o arquivo dizendo que ele tem MENOS amostras por segundo do que tem de verdade.
    Quem toca, toca mais devagar, e tudo fica mais grave (-3 semitons = 16% mais lento). É o
    truque mais antigo do rádio para voz de monstro, e não precisa de biblioteca nenhuma.

    ASSOMBRAR: o tratamento de voz de fantasma (ver assombrar), antes de normalizar."""
    n = len(pcm) // 2
    amostras = struct.unpack(f"<{n}h", pcm[:n * 2])
    if "assombrar" in som:
        amostras = assombrar(amostras, taxa_original, som["assombrar"])
        n = len(amostras)
    if "normalizar" in som:
        rms = math.sqrt(sum(a * a for a in amostras) / max(n, 1)) / 32768
        pico = max((abs(a) for a in amostras), default=0) / 32768
        ganho_db = min(som["normalizar"] - _db(rms), PICO_MAX_DB - _db(pico))  # o que vier primeiro: alvo ou pico
        ganho = 10 ** (ganho_db / 20)
        amostras = [max(-32767, min(32767, int(a * ganho))) for a in amostras]
        print(f"      normalizado: RMS {_db(rms):.1f} -> {_db(rms) + ganho_db:.1f} dB (ganho {ganho_db:+.1f} dB, "
              f"pico {_db(pico) + ganho_db:.1f} dB)")
    amostras = [max(-32767, min(32767, int(a))) for a in amostras]
    taxa = taxa_original
    if som.get("tom"):
        taxa = int(round(taxa_original * 2 ** (som["tom"] / 12)))
        print(f"      tom: {som['tom']:+} semitons ({taxa_original} -> {taxa} amostras/s)")
    with wave.open(caminho, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(taxa)
        w.writeframes(struct.pack(f"<{len(amostras)}h", *amostras))
    return os.path.getsize(caminho)


# ---------------------------------------------------------------- voz de fantasma
# O tratamento que os filmes de terror fazem numa voz comum, em quatro camadas:
#   1. GRAVE sem arrastar: abaixa o tom e devolve a velocidade (a fala continua natural);
#   2. SUB: uma cópia uma OITAVA abaixo, bem baixinha, por baixo (o "peso" que não se ouve, se sente);
#   3. ECO de sala grande e escura (reverb com os agudos morrendo primeiro, como num teatro vazio);
#   4. ECO AO CONTRÁRIO: o reverb da fala tocado de trás para frente ANTES dela, um "sopro" que
#      cresce do nada até a primeira palavra. É o truque clássico de voz de possessão.
# Campos em "assombrar": tom (semitons), sub (dB da oitava abaixo; ausente = sem), eco (0 a 1, quanto
# do som é reverb), cauda (segundos do reverb), reverso (dB do eco ao contrário; ausente = sem) e
# pre (segundos do eco ao contrário antes da voz).

def _reamostrar(x, fator):
    """Lê o som 'fator' vezes mais rápido (0,84 = mais devagar e mais grave), por interpolação."""
    n = int(len(x) / fator)
    saida = []
    for i in range(n):
        p = i * fator
        j = int(p)
        f = p - j
        a = x[j] if j < len(x) else 0.0
        b = x[j + 1] if j + 1 < len(x) else 0.0
        saida.append(a + (b - a) * f)
    return saida


def _esticar(x, fator, taxa):
    """Muda a DURAÇÃO sem mudar o tom (sobreposição de grãos com janela de Hann): fator 0,84 = 16% mais curto."""
    janela = int(taxa * 0.04)  # grãos de 40 ms
    passo = janela // 4        # 75% de sobreposição
    hann = [0.5 - 0.5 * math.cos(2 * math.pi * i / janela) for i in range(janela)]
    total = int(len(x) * fator)
    saida = [0.0] * (total + janela)
    peso = [0.0] * (total + janela)
    k = 0
    while k * passo < total:
        origem = int(k * passo / fator)
        for i in range(janela):
            j = origem + i
            if j >= len(x):
                break
            saida[k * passo + i] += x[j] * hann[i]
            peso[k * passo + i] += hann[i]
        k += 1
    return [s / w if w > 1e-3 else 0.0 for s, w in zip(saida[:total], peso[:total])]


def _tom_sem_arrastar(x, semitons, taxa):
    fator = 2 ** (semitons / 12)  # < 1 = mais grave
    return _esticar(_reamostrar(x, fator), fator, taxa)


def _reverb(x, taxa, cauda):
    """Reverb de Schroeder: 4 ecos curtos realimentados (com os agudos morrendo) + 2 'borradores'."""
    x = list(x) + [0.0] * int(taxa * cauda)
    n = len(x)
    molhado = [0.0] * n
    for ms in (29.7, 37.1, 41.1, 43.7):
        d = int(taxa * ms / 1000)
        g = 10 ** (-3 * (ms / 1000) / cauda)  # some 60 dB em 'cauda' segundos
        buf = [0.0] * d
        filtro, pos = 0.0, 0
        for i in range(n):
            saida = buf[pos]
            filtro = saida * 0.6 + filtro * 0.4  # amortece: o agudo morre antes do grave
            buf[pos] = x[i] + filtro * g
            molhado[i] += saida
            pos = (pos + 1) % d
    for ms in (5.0, 1.7):
        d = int(taxa * ms / 1000)
        buf = [0.0] * d
        pos = 0
        for i in range(n):
            atraso = buf[pos]
            v = molhado[i] + atraso * 0.7
            buf[pos] = v
            molhado[i] = atraso - v * 0.7
            pos = (pos + 1) % d
    return molhado


def _rms(x):
    return math.sqrt(sum(a * a for a in x) / max(len(x), 1))


def assombrar(amostras, taxa, cfg):
    seco = [float(a) for a in amostras]
    if cfg.get("tom"):
        seco = _tom_sem_arrastar(seco, cfg["tom"], taxa)
    base = _rms(seco)
    if "sub" in cfg:
        sub = _tom_sem_arrastar(seco, -12, taxa)
        ganho = 10 ** (cfg["sub"] / 20) * base / max(_rms(sub), 1e-9)
        seco = [a + (sub[i] * ganho if i < len(sub) else 0.0) for i, a in enumerate(seco)]
    cauda = cfg.get("cauda", 2.0)
    eco = cfg.get("eco", 0.0)
    pre = int(taxa * cfg.get("pre", 0.0)) if "reverso" in cfg else 0
    n = pre + len(seco) + int(taxa * cauda)
    saida = [0.0] * n
    for i, a in enumerate(seco):
        saida[pre + i] += a * (1 - eco * 0.5)
    if eco > 0:
        rev = _reverb(seco, taxa, cauda)
        g = eco * base / max(_rms(rev[:len(seco)]), 1e-9)
        for i, a in enumerate(rev):
            if pre + i < n:
                saida[pre + i] += a * g
    if pre:
        # O reverb da fala de trás para frente, terminando onde a voz começa.
        rev = _reverb(seco[::-1], taxa, cfg.get("pre", 1.0))[::-1]
        inicio = len(rev) - len(seco)  # onde a voz começa dentro do 'rev'
        g = 10 ** (cfg["reverso"] / 20) * base / max(_rms(rev[:inicio]), 1e-9)
        for i in range(max(0, inicio - pre), inicio):  # só o "sopro" antes: por cima da fala, embolaria
            j = i - inicio + pre
            if 0 <= j < n:
                saida[j] += rev[i] * g
    print(f"      assombrado: tom {cfg.get('tom', 0):+}, sub {cfg.get('sub', '-')} dB, eco {eco}, "
          f"cauda {cauda} s, reverso {cfg.get('reverso', '-')} dB ({len(saida) / taxa:.1f} s)")
    return saida


# ---------------------------------------------------------------- procedurais
# Cada função devolve uma lista de amostras entre -1 e 1. Seguem o DESIGN.md (§6):
# clique abafado como madeira, hover quase inaudível, erro grave e curto, nada de "plim" alegre.

def _envelope(t, ataque, queda):
    """Sobe rápido em 'ataque' segundos e cai exponencialmente ('queda' = meia-vida)."""
    subida = min(1.0, t / ataque) if ataque > 0 else 1.0
    return subida * math.pow(0.5, t / queda)


def _ruido_filtrado(rng, n, suavizar):
    """Ruído branco passado num filtro passa-baixa simples (quanto maior 'suavizar', mais abafado)."""
    saida, anterior = [], 0.0
    for _ in range(n):
        anterior += (rng.uniform(-1, 1) - anterior) / suavizar
        saida.append(anterior)
    return saida


def ui_clique(rng):
    n = int(TAXA * 0.08)
    ruido = _ruido_filtrado(rng, n, 6)
    return [(_envelope(i / TAXA, 0.001, 0.008) * (0.7 * ruido[i] * 3
             + 0.5 * math.sin(2 * math.pi * 190 * i / TAXA))) * 0.6 for i in range(n)]


def ui_hover(rng):
    n = int(TAXA * 0.04)
    ruido = _ruido_filtrado(rng, n, 3)
    return [_envelope(i / TAXA, 0.002, 0.006) * ruido[i] * 0.25 for i in range(n)]


def ui_erro(rng):
    n = int(TAXA * 0.3)
    amostras = []
    for i in range(n):
        t = i / TAXA
        # Dois "tuns" graves seguidos (o segundo um pouco mais baixo).
        tom, inicio = (110, 0.0) if t < 0.14 else (92, 0.14)
        local = t - inicio
        onda = math.sin(2 * math.pi * tom * local) + 0.3 * math.sin(2 * math.pi * tom * 2 * local)
        amostras.append(_envelope(local, 0.004, 0.04) * onda * 0.45)
    return amostras


def eco_pulso(rng):
    n = int(TAXA * 0.6)
    amostras, fase = [], 0.0
    for i in range(n):
        t = i / TAXA
        freq = 880 - 300 * min(1.0, t / 0.25)  # desce um pouco, como sonar
        fase += 2 * math.pi * freq / TAXA
        amostras.append(_envelope(t, 0.003, 0.09) * math.sin(fase) * 0.5)
    return amostras


PROCEDURAIS = {"ui_clique": ui_clique, "ui_hover": ui_hover, "ui_erro": ui_erro, "eco_pulso": eco_pulso}


def gerar_procedural(som, caminho):
    funcao = PROCEDURAIS.get(som["id"])
    if funcao is None:
        raise RuntimeError(f"não há função procedural chamada {som['id']}")
    rng = random.Random(som["id"])  # mesma semente = mesmo arquivo sempre (sem falsas mudanças no Git)
    amostras = funcao(rng)
    with wave.open(caminho, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(TAXA)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, a)) * 32767)) for a in amostras))
    return os.path.getsize(caminho)


# ---------------------------------------------------------------- principal

def main():
    p = argparse.ArgumentParser(description="Gera os sons do catalogo.json.")
    p.add_argument("--so", action="append", default=[], help="id de um som (pode repetir)")
    p.add_argument("--pasta", action="append", default=[], help="só os sons desta pasta (pode repetir)")
    p.add_argument("--refazer", action="store_true", help="gera de novo mesmo se o arquivo já existe")
    p.add_argument("--listar", action="store_true", help="só mostra o que seria gerado")
    args = p.parse_args()

    with open(CATALOGO, encoding="utf-8") as f:
        sons = json.load(f)["sons"]

    ids = {s["id"] for s in sons}
    for desconhecido in set(args.so) - ids:
        sys.exit(f"id não está no catálogo: {desconhecido}")

    # "externo": arquivo trazido pela equipe (a trilha), não gerado aqui: nunca sobrescrever.
    escolhidos = [s for s in sons if not s.get("externo")
                  and (not args.so or s["id"] in args.so) and (not args.pasta or s["pasta"] in args.pasta)]

    # (som, caminho, índice da variação): a fala usa o índice para saber qual frase dizer.
    tarefas = [(s, c, i) for s in escolhidos for i, c in enumerate(arquivos_do_som(s))
               if args.refazer or not os.path.exists(c)]
    segundos_ia = sum(s["duracao"] for s, _, _ in tarefas if not s.get("procedural") and "fala" not in s)
    frases = sum(1 for s, _, _ in tarefas if "fala" in s)
    print(f"{len(tarefas)} arquivo(s) para gerar ({segundos_ia:.1f} s de efeitos e {frases} frase(s) pela IA)")

    if args.listar:
        for _, c, _ in tarefas:
            print("  ", os.path.relpath(c, RAIZ))
        return

    chave = os.environ.get("ELEVENLABS_API_KEY")
    if any(not s.get("procedural") for s, _, _ in tarefas) and not chave:
        sys.exit("ELEVENLABS_API_KEY não encontrada (configure a variável de ambiente e reabra o terminal)")

    falhas = 0
    for s, caminho, i in tarefas:
        os.makedirs(os.path.dirname(caminho), exist_ok=True)
        nome = os.path.relpath(caminho, RAIZ)
        try:
            if s.get("procedural"):
                tamanho = gerar_procedural(s, caminho)
            elif "fala" in s:
                tamanho = gerar_fala(s, s["fala"][i], caminho, chave)
            else:
                tamanho = gerar_ia(s, caminho, chave)
            print(f"SAVED {nome} ({tamanho // 1024} KB)")
        except Exception as e:  # um som com problema não impede os outros
            falhas += 1
            print(f"ERRO  {nome}: {e}")
    if falhas:
        sys.exit(f"{falhas} som(ns) com erro")


if __name__ == "__main__":
    main()
