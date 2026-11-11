using DeskStage.Gameplay;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 遊ぶとき、選んだ写真のキャラ（人形の切り抜き）を、キャラの体の代わりに立てる（キャラクターを写真で作る）。
    /// C の GameFlow が GameStarted を出したときに、キャラ（CharacterMotor）の体（Body、Nose）を隠し、
    /// 切り抜きの板（Cutout）を足してカメラへ向ける。
    /// 顔の枠（HumanMask）で撮ったキャラは、板ではなく、立体の体（CharacterBody3D）を立てて顔の写真を頭に貼り、手足を動かす（CharacterLimbs。2026-11-10）。
    /// 選んでいなければ「いつもの こ」：描いた顔（DefaultFace）で、同じ立体の体を立てる（2026-11-11。前は C の青いカプセル）。
    /// 当たり判定や動きは、C のキャラのまま変えない。見た目だけ。
    /// </summary>
    public class CharacterAvatar : MonoBehaviour
    {
        const string CutoutName = "Cutout";
        const string BodyName = "Body3D";

        // 見た目の大きさ。C のキャラの当たり判定の高さ（3cm）の何倍にするか（2026-10-24：1.5 倍）。当たり判定は変えない
        const float SizeScale = 1.5f;

        [SerializeField] Material cutoutMaterial;

        CharacterStore store;
        Texture2D texture;
        Material material;
        readonly System.Collections.Generic.List<Object> created = new System.Collections.Generic.List<Object>();

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
            foreach (var name in new[] { CutoutName, BodyName })
            {
                var old = player.Find(name);
                if (old != null)
                {
                    Destroy(old.gameObject);
                }
            }
            SetBodyVisible(player, true);
            Release();

            // 身長は、C のキャラの当たり判定の高さ（3cm）の SizeScale 倍
            var controller = motor.GetComponent<CharacterController>();
            var height = (controller != null ? controller.height : 0.03f) * SizeScale;

            // 撮ったキャラを選んでいない（または読めない）ときは、「いつもの こ」：描いた顔（DefaultFace）で、同じ立体の体を立てる（2026-11-11）
            var id = store.SelectedId;
            var info = string.IsNullOrEmpty(id) ? null : store.Find(id);
            var png = info != null ? store.LoadPng(id) : null;
            if (png == null)
            {
                ApplyDefault(motor, height);
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
                ApplyDefault(motor, height);
                return;
            }

            // 横は、切り抜きの縦横の比
            var aspect = info.aspect > 0f ? info.aspect : (float)texture.width / texture.height;

            // 顔の枠で撮ったキャラは、顔の写真を立体の頭に貼る。前の人型の枠で撮ったキャラは、人型の頭の部分を顔として使う（縦横の比で見分ける）
            var face = Mathf.Abs(aspect - HumanMask.Aspect) < 0.02f ? HumanMask.Face
                : Mathf.Abs(aspect - HumanMask.LegacyAspect) < 0.02f ? HumanMask.LegacyHead
                : (Vector4?)null;
            if (face != null)
            {
                if (BuildBody(motor, height, face.Value, null))
                {
                    SetBodyVisible(player, false);
                }
                return;
            }
            if (cutoutMaterial == null)
            {
                return;
            }

            material = new Material(cutoutMaterial);
            material.SetTexture("_BaseMap", texture);
            material.mainTexture = texture;

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

        // 「いつもの こ」：描いた顔を、撮った顔の写真と同じ枠（HumanMask.Face）で貼る。作れなければ、C のカプセルのまま
        void ApplyDefault(CharacterMotor motor, float height)
        {
            texture = DefaultFace.Create();
            if (BuildBody(motor, height, HumanMask.Face, DefaultFace.HairColor))
            {
                SetBodyVisible(motor.transform, false);
            }
            else
            {
                Release();
            }
        }

        // 立体の体：陰の付く素材（無ければ、切り抜きの素材）に写真を貼り、6 つの部品を作って、手足を動かす部品を付ける。素材が無ければ作らない
        bool BuildBody(CharacterMotor motor, float height, Vector4 face, Color? hair)
        {
            var player = motor.transform;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var front = lit != null ? new Material(lit) : cutoutMaterial != null ? new Material(cutoutMaterial) : null;
            if (front == null)
            {
                return false;
            }
            front.SetTexture("_BaseMap", texture);
            front.mainTexture = texture;
            if (front.HasProperty("_Smoothness")) front.SetFloat("_Smoothness", 0.1f);
            if (front.HasProperty("_Cull")) front.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
            created.Add(front);

            var root = new GameObject(BodyName).transform;
            root.SetParent(player, false);
            root.gameObject.layer = player.gameObject.layer;
            var joints = CharacterBody3D.Build(root, front, texture, face, height, created, hair);
            root.gameObject.AddComponent<CharacterLimbs>().Set(motor, joints, height);
            return true;
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
            foreach (var o in created)
            {
                if (o != null) Destroy(o);
            }
            created.Clear();
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
