using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 練習用シーン（Test_C）で、しかけを確かめるための表示（開発用）。D の画面がないので、ハートの数と、出ている敵の数を左下に出す。
    /// 本番（Main）では使わない。
    /// </summary>
    public class GimmickDebugHud : MonoBehaviour
    {
        public GimmickSystem gimmicks;

        void OnGUI()
        {
            if (gimmicks == null)
            {
                return;
            }
            var hearts = new string('♥', gimmicks.Health.Hearts) + new string('♡', PlayerHealth.Max - gimmicks.Health.Hearts);
            var text = $"ハート：{hearts}　てき：{gimmicks.EnemyCount}{(gimmicks.Health.IsDead ? "　ゲームオーバー" : "")}";
            GUI.Box(new Rect(16, Screen.height - 56, 320, 40), text);
        }
    }
}
