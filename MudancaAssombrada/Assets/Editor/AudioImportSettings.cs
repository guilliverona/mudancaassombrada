using UnityEditor;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Configura a importação dos sons de Assets/Audio sozinho, pela PASTA (o tools/audio/gerar_sons.py
    // salva cada tipo numa pasta), como o ArtImportSettings faz com os modelos.
    //
    //   Assets/Audio/Ambiente/, Musica/   longos, em loop      lidos aos poucos do disco (Streaming), estéreo
    //   Assets/Audio/Interface/           curtíssimos, 2D      descomprimidos ao carregar (tocam sem atraso)
    //   o resto (Passos, Moveis...)       curtos, 3D           descomprimidos ao carregar, MONO
    //
    // Mono porque som 3D é um ponto no mundo: a Unity decide sozinha o lado de cada ouvido.
    // Só roda na PRIMEIRA importação (arquivo .meta novo): o que alguém mudar no Inspector continua valendo.
    public class AudioImportSettings : AssetPostprocessor
    {
        const string AudioFolder = "Assets/Audio/";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioFolder) || !assetImporter.importSettingsMissing) return;
            var importer = (AudioImporter)assetImporter;

            bool longLoop = assetPath.StartsWith(AudioFolder + "Ambiente/") || assetPath.StartsWith(AudioFolder + "Musica/");
            bool ui = assetPath.StartsWith(AudioFolder + "Interface/");

            importer.forceToMono = !longLoop && !ui;
            importer.loadInBackground = longLoop;

            var settings = importer.defaultSampleSettings;
            settings.loadType = longLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = ui ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
        }
    }
}
