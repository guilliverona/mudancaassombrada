using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MudancaAssombrada.Editor
{
    // Pequenas ferramentas usadas por todos os construtores do editor (dados, prefabs, cenas).
    public static class EditorTools
    {
        public const string PrefabsFolder = "Assets/Prefabs";
        public const string MaterialsFolder = "Assets/Prefabs/Materiais";
        public const string ScenesFolder = "Assets/Scenes";

        // Liga um campo [SerializeField] privado, exatamente como arrastar algo no Inspector.
        public static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // O mesmo, para um campo que é uma lista (array).
        public static void SetReferences(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var list = so.FindProperty(field);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Cria a pasta (e as pastas-mãe) se ainda não existir. Ex.: "Assets/Data/Personagens".
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        // Um material com cor, salvo como ARQUIVO. Prefabs só guardam referências a arquivos:
        // um material criado só na memória sumiria (e o objeto ficaria rosa).
        // Se o material já existe, é reaproveitado (e só a cor é atualizada).
        public static Material ColorMaterial(string name, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                // Copia o material padrão de um cubo temporário (o shader certo do URP).
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mat = new Material(temp.GetComponent<Renderer>().sharedMaterial);
                Object.DestroyImmediate(temp);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Salva um objeto da cena como prefab (arquivo) e apaga a cópia da cena.
        public static GameObject SavePrefab(GameObject go, string path)
        {
            EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // Coloca uma cópia de um prefab na cena, LIGADA a ele (mudar o prefab muda a cópia).
        public static GameObject Place(GameObject prefab, Vector3 position, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;
            if (name != null) go.name = name;
            return go;
        }

        // Objetos de rede da cena precisam de um "RG" (GlobalObjectIdHash) para o host e os
        // clientes saberem que o fantasma de um é o MESMO fantasma do outro. O Netcode calcula
        // esse número a partir do arquivo da cena, então só funciona DEPOIS do primeiro save.
        // O método que calcula (OnValidate) é interno do Netcode; chamamos por "reflexão",
        // um jeito do C# de acessar métodos que normalmente não estariam disponíveis.
        public static void FixNetworkObjectIds(UnityEngine.SceneManagement.Scene scene)
        {
            var validate = typeof(Unity.Netcode.NetworkObject).GetMethod("OnValidate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (var netObj in Object.FindObjectsByType<Unity.Netcode.NetworkObject>())
                validate.Invoke(netObj, null);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Trocar de cena só funciona com cenas listadas na Build Settings. O Menu é a primeira
        // (índice 0): é a cena que abre quando alguém roda o jogo exportado.
        public static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuSceneBuilder.ScenePath, true),
                new EditorBuildSettingsScene(PrototypeSceneBuilder.ScenePath, true),
            };
        }
    }
}
