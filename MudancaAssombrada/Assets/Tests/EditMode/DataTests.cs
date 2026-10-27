using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MudancaAssombrada.Tests
{
    // Testes de DADOS: carregam os .asset de verdade (Elenco, Campanha, Sons, móveis, cena Menu)
    // e conferem que estão coerentes. Pegam erro de quem edita no Inspector, não só no código.
    // Falhou aqui? Quase sempre é um dado para corrigir no Unity, não um teste para mudar.
    public class DataTests
    {
        static CharacterRoster Roster => Resources.Load<CharacterRoster>(CharacterRoster.ResourceName);
        static Campaign Campaign => Resources.Load<Campaign>(Campaign.ResourceName);
        static SoundLibrary Sounds => Resources.Load<SoundLibrary>(SoundLibrary.ResourceName);

        // --- Elenco ---

        [Test]
        public void Elenco_TemOsTresPersonagens_SemBuraco()
        {
            var roster = Roster;
            Assert.IsNotNull(roster, "Assets/Resources/Elenco.asset não encontrado");
            Assert.AreEqual(3, roster.Count, "Cida, Tiago e Marcelo");

            var seen = new HashSet<CharacterDefinition>();
            for (int i = 0; i < roster.Count; i++)
            {
                var c = roster.Get(i);
                Assert.IsNotNull(c, $"posição {i} do Elenco está vazia");
                Assert.IsTrue(seen.Add(c), $"{c.name} aparece duas vezes no Elenco");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.displayName), $"{c.name} sem nome");
                Assert.Greater(c.speed, 0f, $"{c.name}: velocidade");
                Assert.Greater(c.strength, 0f, $"{c.name}: força");
            }
        }

        [Test]
        public void Elenco_ForcasDoARQUITETURA()
        {
            // ARQUITETURA.md, "Peso e dupla": Cida 1, Tiago 1,5, Marcelo 3.
            var byName = new Dictionary<string, float>();
            for (int i = 0; i < Roster.Count; i++) byName[Roster.Get(i).name] = Roster.Get(i).strength;
            Assert.AreEqual(1f, byName["Cida"]);
            Assert.AreEqual(1.5f, byName["Tiago"]);
            Assert.AreEqual(3f, byName["Marcelo"]);
        }

        // --- Campanha e capítulos ---

        [Test]
        public void Campanha_QuatroCapitulosNaOrdemDoRoteiro()
        {
            var campaign = Campaign;
            Assert.IsNotNull(campaign, "Assets/Resources/Campanha.asset não encontrado");
            var ids = Enumerable.Range(0, campaign.Count).Select(i => campaign.Get(i)?.id).ToArray();
            CollectionAssert.AreEqual(
                new[] { "casa-do-espelho", "conservatorio", "biblioteca", "ultima-mudanca" }, ids);
        }

        [Test]
        public void Campanha_IdsUnicosEPreenchidos()
        {
            // Dois capítulos com o mesmo id dividiriam o "concluído" no save (CampaignTests).
            var seen = new HashSet<string>();
            for (int i = 0; i < Campaign.Count; i++)
            {
                var c = Campaign.Get(i);
                Assert.IsNotNull(c, $"posição {i} da Campanha está vazia");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.id), $"{c.name} sem id");
                Assert.IsTrue(seen.Add(c.id), $"id repetido: {c.id}");
            }
        }

        [Test]
        public void Capitulo_ComCena_TemACenaNoBuildSettings()
        {
            var inBuild = EditorBuildSettings.scenes.Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path)).ToHashSet();
            for (int i = 0; i < Campaign.Count; i++)
            {
                var c = Campaign.Get(i);
                if (!c.IsPlayable) continue; // em produção
                Assert.IsTrue(inBuild.Contains(c.sceneName),
                    $"{c.title}: a cena {c.sceneName} não está no File > Build Profiles (Scene List)");
            }
            Assert.IsTrue(inBuild.Contains("Menu"), "a cena Menu precisa estar no build");
        }

        [Test]
        public void Capitulo_ValoresValidos_EComentariosDoElenco()
        {
            var cast = Enumerable.Range(0, Roster.Count).Select(Roster.Get).ToHashSet();
            for (int i = 0; i < Campaign.Count; i++)
            {
                var c = Campaign.Get(i);
                Assert.GreaterOrEqual(c.nightDuration, 0f, $"{c.title}: duração da noite");
                Assert.GreaterOrEqual(c.payment, 0, $"{c.title}: pagamento");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.title), $"{c.name} sem título");
                foreach (var comment in c.ratingComments)
                    Assert.IsTrue(comment.character != null && cast.Contains(comment.character),
                        $"{c.title}: comentário da avaliação para um personagem fora do Elenco");
                if (c.featuredCharacter != null)
                    Assert.IsTrue(cast.Contains(c.featuredCharacter), $"{c.title}: personagem em destaque fora do Elenco");
            }
        }

        // --- Sons ---

        // Os nomes de som escritos direto no código: GameAudio.Play("x"...), PlayUi("x") e as
        // constantes da SoundLibrary. (Nomes escolhidos no Inspector, em prefabs e cenas, ficam de fora.)
        static IEnumerable<string> SoundIdsInCode()
        {
            var ids = new HashSet<string> { SoundLibrary.UiClick, SoundLibrary.UiHover, SoundLibrary.UiError };
            var call = new Regex("GameAudio\\.Play(?:Ui)?\\(\\s*\"([^\"]+)\"");
            var ternary = new Regex("GameAudio\\.Play\\([^;]*?\\?\\s*\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
            foreach (var file in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string code = File.ReadAllText(file);
                foreach (Match m in call.Matches(code)) ids.Add(m.Groups[1].Value);
                foreach (Match m in ternary.Matches(code)) { ids.Add(m.Groups[1].Value); ids.Add(m.Groups[2].Value); }
            }
            return ids;
        }

        [Test]
        public void Sons_TodoSomDoCodigoExisteNaBiblioteca()
        {
            var library = Sounds;
            Assert.IsNotNull(library, "Assets/Resources/Sons.asset não encontrado");
            var missing = SoundIdsInCode().Where(id => library.Get(id) == null).ToList();
            Assert.IsEmpty(missing, "sons usados no código sem entrada (ou sem clipe) em Sons.asset: " + string.Join(", ", missing));
        }

        [Test]
        public void Sons_NenhumaEntradaSemClipeOuRepetida()
        {
            var seen = new HashSet<string>();
            var problems = new List<string>();
            foreach (var e in Sounds.sounds)
            {
                if (string.IsNullOrWhiteSpace(e.id)) { problems.Add("(entrada sem id)"); continue; }
                if (!seen.Add(e.id)) problems.Add($"{e.id} repetido");
                if (e.clips.Length == 0 || e.clips.Any(c => c == null)) problems.Add($"{e.id} sem clipe");
            }
            Assert.IsEmpty(problems, string.Join(", ", problems));
        }

        // --- Móveis ---

        [Test]
        public void Moveis_TodoPrefabTemOsComponentesEPesoValido()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Móveis" });
            Assert.IsNotEmpty(guids, "nenhum prefab em Assets/Prefabs/Móveis");
            foreach (var guid in guids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var furniture = prefab.GetComponent<Furniture>();
                Assert.IsNotNull(furniture, $"{prefab.name} sem Furniture");
                Assert.IsNotNull(prefab.GetComponent<Rigidbody>(), $"{prefab.name} sem Rigidbody");
                Assert.IsNotNull(prefab.GetComponent<NetworkObject>(), $"{prefab.name} sem NetworkObject");
                Assert.That(furniture.Weight, Is.InRange(1f, 5f), $"{prefab.name}: peso");
            }
        }

        // --- Cena Menu ---

        [Test]
        public void Menu_NenhumCampoDoMainMenuFicouVazio()
        {
            // Abre a cena Menu POR CIMA da que estiver aberta (Additive) e fecha no fim:
            // não mexe no que você está editando.
            const string path = "Assets/Scenes/Menu.unity";
            var already = SceneManager.GetSceneByPath(path);
            bool wasOpen = already.IsValid() && already.isLoaded; // já aberta: usa e NÃO fecha no fim
            var scene = wasOpen ? already : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenu>(true)).FirstOrDefault();
                Assert.IsNotNull(menu, "a cena Menu não tem MainMenu");

                var empty = new List<string>();
                var so = new SerializedObject(menu);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                    if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue == null
                        && prop.name != "m_Script")
                        empty.Add(prop.propertyPath);
                Assert.IsEmpty(empty, "campos vazios no MainMenu (rode Mudança Assombrada > Criar Cena Menu): " + string.Join(", ", empty));
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
