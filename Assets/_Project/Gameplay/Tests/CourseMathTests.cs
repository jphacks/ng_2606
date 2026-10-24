using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// スタート・ゴール・チェックポイントの触れた判定（C_gameplay.md の Step 14）を確かめる。
    /// </summary>
    public class CourseMathTests
    {
        const float Radius = 0.03f;
        const float CharacterRadius = 0.008f;

        [Test]
        public void 真上に立つと触れている()
        {
            // 体の中心は足元から 1.5cm 上
            Assert.IsTrue(CourseMath.IsTouching(new Vector3(0f, 0.015f, 0f), Vector3.zero, Radius, CharacterRadius));
        }

        [Test]
        public void 半径とキャラの半径を足した距離の内側は触れている()
        {
            Assert.IsTrue(CourseMath.IsTouching(new Vector3(0.0379f, 0f, 0f), Vector3.zero, Radius, CharacterRadius));
        }

        [Test]
        public void それより遠いと触れていない()
        {
            Assert.IsFalse(CourseMath.IsTouching(new Vector3(0.039f, 0f, 0f), Vector3.zero, Radius, CharacterRadius));
        }

        [Test]
        public void 高さが違うと届かない()
        {
            // 8cm の箱の上のゴールに、机の上から真下で近づいても触れない
            Assert.IsFalse(CourseMath.IsTouching(new Vector3(0f, 0.015f, 0f), new Vector3(0f, 0.08f, 0f), Radius, CharacterRadius));
        }
    }
}
