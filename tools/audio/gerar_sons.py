"""Gera os sons do jogo a partir do catalogo.json (mesma ideia dos scripts do Blender).

- Sons com "texto": pede à API de efeitos sonoros do ElevenLabs e salva em MP3.
- Sons "procedural": gerados aqui mesmo, por código (WAV), sem IA e sem gastar créditos.

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

API_URL = "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128"
MODELO = "eleven_text_to_sound_v2"  # o modelo que aceita "loop"
TAXA = 44100  # amostras por segundo dos sons procedurais


def arquivos_do_som(som):
    """Os caminhos que um som do catálogo gera (1 por variação)."""
    ext = ".wav" if som.get("procedural") else ".mp3"
    pasta = os.path.join(SAIDA, som["pasta"])
    n = som.get("variacoes", 1)
    if n <= 1:
        return [os.path.join(pasta, som["id"] + ext)]
    return [os.path.join(pasta, f"{som['id']}_{i:02d}{ext}") for i in range(1, n + 1)]


# ---------------------------------------------------------------- ElevenLabs

def gerar_ia(som, caminho, chave):
    corpo = json.dumps({
        "text": som["texto"],
        "duration_seconds": som["duracao"],
        "prompt_influence": som.get("influencia", 0.5),
        "loop": som.get("loop", False),
        "model_id": MODELO,
    }).encode("utf-8")
    req = urllib.request.Request(API_URL, data=corpo, method="POST", headers={
        "xi-api-key": chave, "Content-Type": "application/json", "Accept": "audio/mpeg"})
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            dados = resp.read()
    except urllib.error.HTTPError as e:
        # Mostra só a mensagem da API (ela nunca devolve a chave).
        raise RuntimeError(f"HTTP {e.code}: {e.read().decode('utf-8', 'replace')[:300]}") from None
    with open(caminho, "wb") as f:
        f.write(dados)
    return len(dados)


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

    escolhidos = [s for s in sons
                  if (not args.so or s["id"] in args.so) and (not args.pasta or s["pasta"] in args.pasta)]

    tarefas = [(s, c) for s in escolhidos for c in arquivos_do_som(s)
               if args.refazer or not os.path.exists(c)]
    segundos_ia = sum(s["duracao"] for s, _ in tarefas if not s.get("procedural"))
    print(f"{len(tarefas)} arquivo(s) para gerar ({segundos_ia:.1f} s de áudio pela IA)")

    if args.listar:
        for s, c in tarefas:
            print("  ", os.path.relpath(c, RAIZ))
        return

    chave = os.environ.get("ELEVENLABS_API_KEY")
    if any(not s.get("procedural") for s, _ in tarefas) and not chave:
        sys.exit("ELEVENLABS_API_KEY não encontrada (configure a variável de ambiente e reabra o terminal)")

    falhas = 0
    for s, caminho in tarefas:
        os.makedirs(os.path.dirname(caminho), exist_ok=True)
        nome = os.path.relpath(caminho, RAIZ)
        try:
            tamanho = gerar_procedural(s, caminho) if s.get("procedural") else gerar_ia(s, caminho, chave)
            print(f"SAVED {nome} ({tamanho // 1024} KB)")
        except Exception as e:  # um som com problema não impede os outros
            falhas += 1
            print(f"ERRO  {nome}: {e}")
    if falhas:
        sys.exit(f"{falhas} som(ns) com erro")


if __name__ == "__main__":
    main()
