using DeskStage.Gameplay;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 遊ぶとき、選んだ写真のキャラ（人形の切り抜き）を、キャラの体の代わりに立てる（キャラクターを写真で作る）。
    /// C の GameFlow が GameStarted を出したときに、キャラ（CharacterMotor）の体（Body、Nose）を隠し、
    /// 切り抜きの板（Cutout）を足してカメラへ向ける。選んでいなければ、いつものキャラのまま。
    /// 当たり判定や動きは、C のキャラのまま変えない。見た目だけ。
    /// </summary>
    public class CharacterAvatar : MonoBehaviour
    {
        const string CutoutName = "Cutout";

        [SerializeField] Material cutoutMaterial;

        CharacterStore store;
        Texture2D texture;
        Material material;

        void Awake() => store = CharacterStore.AtDefaultPlace();

        void OnEnable() => GameEvents.GameStarted += Apply;

        void OnDisable()
        {
            GameEvents.GameStarted -= Apply;
            Release();
        }

        void Apply()
        {
            var motor = FindFirstObjectByType<CharacterMotor>();
            if (motor == null)
            {
                return;
            }
            var player = motor.transform;

            // 前の板を外し、体を元に戻す
            var old = player.Find(CutoutName);
            if (old != null)
            {
                Destroy(old.gameObject);
            }
            SetBodyVisible(player, true);
            Release();

            var id = store.SelectedId;
            if (string.IsNullOrEmpty(id) || cutoutMaterial == null)
            {
                return;
            }
            var info = store.Find(id);
            var png = store.LoadPng(id);
            if (info == null || png == null)
            {
                return;
            }

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (!texture.LoadImage(png))
            {
                Release();
                return;
            }

            material = new Material(cutoutMaterial);
            material.SetTexture("_BaseMap", texture);
            material.mainTexture = texture;

            // 身長は、C のキャラの当たり判定の高さ（3cm）。横は、切り抜きの縦横の比
            var controller = motor.GetComponent<CharacterController>();
            var height = controller != null ? controller.height : 0.03f;
            var aspect = info.aspect > 0f ? info.aspect : (float)texture.width / texture.height;

            var cutout = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cutout.name = CutoutName;
            var collider = cutout.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            cutout.transform.SetParent(player, false);
            cutout.transform.localPosition = new Vector3(0f, height / 2f, 0f);
            cutout.transform.localScale = new Vector3(height * aspect, height, 1f);
            cutout.layer = player.gameObject.layer;
            cutout.GetComponent<Renderer>().sharedMaterial = material;
            cutout.AddComponent<CutoutBillboard>();

            SetBodyVisible(player, false);
        }

        static void SetBodyVisible(Transform player, bool visible)
        {
            foreach (var name in new[] { "Body", "Nose" })
            {
                var part = player.Find(name);
                if (part == null)
                {
                    continue;
                }
                var renderer = part.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        void Release()
        {
            if (material != null)
            {
                Destroy(material);
                material = null;
            }
            if (texture != null)
            {
                Destroy(texture);
                texture = null;
            }
        }
    }
}
