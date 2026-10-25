using UnityEditor;

namespace MudancaAssombrada.Editor
{
    // Configura a importação dos modelos de Assets/Art sozinho, pela PASTA onde o FBX está
    // (os scripts de tools/blender salvam cada tipo numa pasta). Assim ninguém precisa lembrar
    // de mudar o Rig no Inspector a cada modelo novo.
    //
    //   Assets/Art/Characters/...  personagens jogáveis   rig Humanoid
    //   Assets/Art/Ghosts/...      fantasmas              rig Humanoid
    //   Assets/Art/Bosses/...      Seu Valdemar, Inquilino rig Humanoid
    //   Assets/Art/Props/...       móveis, caminhão...    sem rig
    //   Assets/Art/Environment/... paredes, pisos, portas sem rig
    //
    // Só roda quando o modelo é importado pela PRIMEIRA vez (arquivo .meta novo): depois disso,
    // o que alguém mudar no Inspector continua valendo.
    public class ArtImportSettings : AssetPostprocessor
    {
        const string ArtFolder = "Assets/Art/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ArtFolder) || !assetImporter.importSettingsMissing) return;
            var importer = (ModelImporter)assetImporter;

            // Os scripts do Blender já exportam em metros e com a frente certa (ART_BIBLE.md).
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false; // ainda não há animações nos modelos

            bool hasRig = assetPath.StartsWith(ArtFolder + "Characters/") || assetPath.StartsWith(ArtFolder + "Ghosts/")
                          || assetPath.StartsWith(ArtFolder + "Bosses/");
            if (hasRig)
            {
                // O Unity mapeia os ossos sozinho pelos nomes (Hips, Spine, LeftUpperArm...).
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
            }
        }
    }
}
