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
    //   Assets/Art/Animations/...  clipes (Mixamo)        rig Humanoid, em loop
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
            importer.importAnimation = false; // os modelos não trazem animação (elas ficam em Animations/)

            // Clipes de animação (Mixamo): Humanoid também, para servirem em QUALQUER personagem
            // nosso (o Unity "traduz" o movimento de um esqueleto para o outro).
            if (IsAnimation)
            {
                importer.bakeAxisConversion = false; // o Mixamo já vem com a frente do Unity
                importer.importAnimation = true;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                return;
            }

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

        bool IsAnimation => assetPath.StartsWith(ArtFolder + "Animations/");

        // Os clipes de Animations/: o nome do clipe vira o do arquivo (Andar, Correr...), todos em
        // loop menos o "Agachar" (a transição), e o corpo não sai do lugar nem gira sozinho: quem
        // move o jogador é o CharacterController (PlayerMovement), não a animação.
        void OnPreprocessAnimation()
        {
            if (!IsAnimation || !assetImporter.importSettingsMissing) return;
            var importer = (ModelImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = name;
                clip.loopTime = name != "Agachar";
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
