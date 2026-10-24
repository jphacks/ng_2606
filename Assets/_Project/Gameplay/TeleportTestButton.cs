using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 練習用シーンで瞬間移動（Step 5）を確かめる仮のボタン。押すと target の位置と向きへキャラを移す。
    /// キャラは GameFlow が出すので、motor が空なら GameFlow の今のキャラを使う。
    /// </summary>
    public class TeleportTestButton : MonoBehaviour
    {
        public CharacterMotor motor;
        public GameFlow gameFlow;
        public Transform target;

        void OnGUI()
        {
            var m = motor != null ? motor : gameFlow != null ? gameFlow.Motor : null;
            if (m == null || target == null)
            {
                return;
            }
            if (GUI.Button(new Rect(16, 16, 200, 48), "しゅんかんいどう"))
            {
                m.Teleport(target.position, target.rotation);
            }
        }
    }
}
