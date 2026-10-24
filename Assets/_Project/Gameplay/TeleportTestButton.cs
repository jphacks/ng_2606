using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 練習用シーンで瞬間移動（Step 5）を確かめる仮のボタン。押すと target の位置と向きへキャラを移す。
    /// </summary>
    public class TeleportTestButton : MonoBehaviour
    {
        public CharacterMotor motor;
        public Transform target;

        void OnGUI()
        {
            if (motor == null || target == null)
            {
                return;
            }
            if (GUI.Button(new Rect(16, 16, 200, 48), "しゅんかんいどう"))
            {
                motor.Teleport(target.position, target.rotation);
            }
        }
    }
}
