using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
                // Clipe feito no Blender sobre um personagem nosso: lê com o avatar DELE.
                string clip = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (OwnRigClips.TryGetValue(clip, out var source))
                {
                    var avatar = AssetDatabase.LoadAllAssetsAtPath(source).OfType<Avatar>().FirstOrDefault();
                    if (avatar != null)
                    {
                        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                        importer.sourceAvatar = avatar;
                        // Mesma conversão de eixos do modelo (feito pelo mesmo export_fbx), senão
                        // os ossos do clipe não batem com os do avatar e a pose sai torta.
                        importer.bakeAxisConversion = true;
                    }
                    else UnityEngine.Debug.LogWarning($"{clip}: o modelo {source} ainda não tem avatar; importe ele antes.");
                }
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

        // Clipes gerados pelos scripts de tools/blender sobre o rig de um personagem nosso (ex.: a
        // pega da bengala, no rig da Cida). O FBX só tem o esqueleto, e JÁ na pose: "criar o avatar
        // deste modelo" tomaria a própria pose como repouso e o clipe não mexeria em nada. Então
        // eles usam o avatar do personagem de onde vieram (Copy From Other Avatar).
        static readonly Dictionary<string, string> OwnRigClips = new()
        {
            { "PegaBengala", "Assets/Art/Characters/Cida/SK_CHR_Cida.fbx" },
            { "PegaLanterna", "Assets/Art/Characters/Tiago/SK_CHR_Tiago.fbx" }, // tools/blender/build_pega_lanterna.py
        };

        // Clipes que tocam uma vez só (transições): o resto fica em loop.
        // O "Cair" para no último quadro: é a pose de caído, até alguém ajudar.
        static readonly string[] OneShot = { "Agachar", "Cair", "Levantar" };

        // Os clipes de Animations/: o nome do clipe vira o do arquivo (Andar, Correr...), todos em
        // loop menos os de OneShot (transições), e o corpo não sai do lugar nem gira sozinho: quem
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
                clip.loopTime = System.Array.IndexOf(OneShot, name) < 0;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }

        // --- Materiais ---
        //
        // O FBX do Blender leva o "Metallic" do material no campo ReflectionFactor, e o importador
        // do URP ignora esse campo: latão, dourado, ferro e espelho chegavam com metal 0 (pareciam
        // de plástico). A rugosidade chega certa (vira o Smoothness). Este passo roda DEPOIS do
        // importador do URP (ordem alta) e só corrige o metal. Vale a cada importação, não só na 1ª.
        public override int GetPostprocessOrder() => 100;
        public override uint GetVersion() => 4; // mudou a regra: os FBX de Assets/Art reimportam sozinhos

        void OnPreprocessMaterialDescription(UnityEditor.AssetImporters.MaterialDescription description,
            Material material, AnimationClip[] clips)
        {
            if (!assetPath.StartsWith(ArtFolder) || !material.HasProperty("_Metallic")) return;
            if (description.TryGetProperty("ReflectionFactor", out float metal))
                material.SetFloat("_Metallic", Mathf.Clamp01(metal));
            ApplyDetail(material);
        }

        // --- Textura de detalhe (o grão por cima da cor lisa) ---
        //
        // As texturas vêm de tools/blender/build_detail_textures.py. Cada material do Blender cai
        // numa família pelo NOME (MAT_PRP_Mogno -> Madeira). A cor continua a do Art Bible: o
        // "Detail" do URP Lit multiplica por 2x a textura cinza, que tem média 128 (não muda nada).
        // O UV dos móveis está em METROS (blockout_common.metric_uvs): "por metro" abaixo é literal.
        // Personagens só entram com UV em metros, numa tabela à parte (o primeiro nome que bate vence).
        const string DetailFolder = ArtFolder + "Textures/Detail/";

        // Força geral do detalhe (cor e relevo), por cima dos valores de cada família.
        // 1 ficou agressivo no Play; 0,6 mantém o grão sem virar estampa.
        const float DetailStrength = 0.6f;

        static readonly (string[] keys, string family, float perMeter, float normal)[] DetailFamilies =
        {
            (new[] { "Papelao" }, "Papelao", 3f, 0.6f),
            (new[] { "PapelVinho", "PapelVelho" }, "PapelParede", 1f, 0.2f), // relevo baixo: papel, não plástico
            (new[] { "Papel" }, "Papel", 3f, 0.3f),
            (new[] { "Mogno", "Madeira", "Tabua", "Rodape", "Palhinha" }, "Madeira", 1.5f, 0.4f),
            (new[] { "Veludo", "Tecido", "Lencol" }, "Tecido", 2f, 0.5f),
            (new[] { "Ferro", "Latao", "Dourado", "Aco", "Ferrugem" }, "Metal", 2f, 0.4f),
            (new[] { "Reboco", "Tijolo", "Barro" }, "Reboco", 1f, 0.8f),
            (new[] { "Xadrez" }, "Piso", 0.5f, 0.3f),
            // o cenário de fora (tools/blender/build_exterior_blockout.py)
            (new[] { "Pedra" }, "Pedra", 1f, 0.8f),
            (new[] { "Grama", "Terra" }, "Grama", 1f, 0.6f),
            (new[] { "Folhagem" }, "Folhagem", 1.5f, 0.7f),
        };

        // Personagens: só quem já tem UV em metros (o script do personagem chama metric_uvs) e pelo
        // nome COMPLETO do material, para não pegar a Cida e o Marcelo, que ainda não têm esse UV.
        static readonly (string[] keys, string family, float perMeter, float normal)[] CharacterDetailFamilies =
        {
            (new[] { "MAT_CHR_Tiago_Moletom" }, "Malha", 3f, 0.35f), // também pega o MoletomEscuro
            (new[] { "MAT_CHR_Tiago_Calca", "MAT_CHR_Tiago_Remendo" }, "Sarja", 3f, 0.35f), // e a CalcaBarra
        };

        void ApplyDetail(Material material)
        {
            var families = assetPath.StartsWith(ArtFolder + "Characters/") ? CharacterDetailFamilies
                : assetPath.StartsWith(ArtFolder + "Props/") || assetPath.StartsWith(ArtFolder + "Environment/") ? DetailFamilies
                : null;
            if (families == null || !material.HasProperty("_DetailAlbedoMap")) return;
            foreach (var (keys, family, perMeter, normal) in families)
            {
                if (!keys.Any(k => material.name.Contains(k))) continue;
                string albedoPath = $"{DetailFolder}T_Detail_{family}.png";
                string normalPath = $"{DetailFolder}T_Detail_{family}_N.png";
                // Diz ao Unity que este modelo depende das texturas: importa elas antes, e
                // reimporta o modelo se uma textura for gerada de novo.
                context.DependsOnArtifact(albedoPath);
                context.DependsOnArtifact(normalPath);
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                var bump = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (albedo == null || bump == null) return; // ainda não geradas: fica a cor lisa
                material.SetTexture("_DetailAlbedoMap", albedo);
                material.SetTexture("_DetailNormalMap", bump);
                material.SetTextureScale("_DetailAlbedoMap", Vector2.one * perMeter);
                material.SetFloat("_DetailAlbedoMapScale", DetailStrength);
                material.SetFloat("_DetailNormalMapScale", normal * DetailStrength);
                material.EnableKeyword("_DETAIL_MULX2");
                return;
            }
        }

        // As texturas de detalhe: repetem (Repeat), e as _N são mapas de normal.
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(DetailFolder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4; // o chão e as paredes são vistos de lado: sem isso o grão borra
            importer.textureType = assetPath.EndsWith("_N.png") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !assetPath.EndsWith("_N.png");
        }
    }
}
