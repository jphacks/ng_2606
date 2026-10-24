using DeskStage.Shared;
using UnityEditor;
using UnityEngine;

namespace DeskStage.Gameplay.EditorTools
{
    /// <summary>
    /// C の Step 3：キャラのプレハブ Player.prefab を作る。
    /// Character Controller の値は CharacterSettings から入れる。作り直すと同じファイルに上書きする。
    /// </summary>
    public static class CreatePlayerPrefab
    {
        const string Folder = "Assets/_Project/Gameplay/Prefabs";
        public const string PrefabPath = Folder + "/Player.prefab";

        [MenuItem("DeskStage/C/2. キャラのプレハブを作る")]
        public static void Run()
        {
            var prefab = Build();
            if (prefab != null)
            {
                Selection.activeObject = prefab;
            }
        }

        /// <summary>プレハブを作って返す。CharacterSettings がなければ null。</summary>
        public static GameObject Build()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CharacterSettings>(CreateCharacterSettings.AssetPath);
            if (settings == null)
            {
                Debug.LogError($"[Gameplay] {CreateCharacterSettings.AssetPath} がありません。先に「DeskStage/C/1. キャラの設定アセットを作る」を実行してください");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Gameplay", "Prefabs");
            }

            var root = new GameObject("Player");
            var controller = root.AddComponent<CharacterController>();
            settings.ApplyTo(controller);

            // 見た目の体。Capsule は高さ2・半径0.5 なので、身長と太さに合わせて縮める
            var body = CreateVisual(PrimitiveType.Capsule, "Body", root.transform);
            body.transform.localPosition = new Vector3(0f, settings.centerY, 0f);
            body.transform.localScale = new Vector3(settings.radius * 2f, settings.height / 2f, settings.radius * 2f);

            // 向きが分かる小さな鼻。前（Z）に少し出す
            var nose = CreateVisual(PrimitiveType.Cube, "Nose", root.transform);
            nose.transform.localPosition = new Vector3(0f, settings.height * 0.75f, settings.radius + 0.002f);
            nose.transform.localScale = new Vector3(0.004f, 0.004f, 0.006f);

            SetLayerRecursively(root, Layers.Player);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[Gameplay] {PrefabPath} を作りました");
            return prefab;
        }

        // 見た目だけの形を作る。当たり判定は Character Controller だけにするため Collider を外す
        static GameObject CreateVisual(PrimitiveType type, string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
