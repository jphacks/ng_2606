using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 切り抜いたキャラの板を、いちばん手前に描くカメラへ向ける（縦はまっすぐのまま、横だけ向ける）。
    /// 視点を回しても、いつも正面が見える、紙人形のような見せ方。
    /// </summary>
    public class CutoutBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            Camera front = null;
            foreach (var cam in Camera.allCameras)
            {
                if (front == null || cam.depth > front.depth)
                {
                    front = cam;
                }
            }
            if (front == null)
            {
                return;
            }

            // Quad は −Z 側から見える。カメラが −Z 側に来るよう、カメラと反対向きへ向ける
            var away = transform.position - front.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 1e-8f)
            {
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
        }
    }
}
