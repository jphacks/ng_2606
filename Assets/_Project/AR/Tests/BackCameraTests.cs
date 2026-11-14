using NUnit.Framework;
using UnityEngine;

namespace DeskStage.AR.Tests
{
    /// <summary>撮影に使うカメラの選び方（BackCamera.Pick）。</summary>
    public class BackCameraTests
    {
        static BackCamera.Info Cam(string name, bool front, WebCamKind kind) =>
            new BackCamera.Info { Name = name, IsFrontFacing = front, Kind = kind };

        [Test]
        public void 前のカメラが先に並んでいても背面の広角を選ぶ()
        {
            var cameras = new[]
            {
                Cam("Front Camera", true, WebCamKind.WideAngle),
                Cam("Back Ultra Wide Camera", false, WebCamKind.UltraWideAngle),
                Cam("Back Camera", false, WebCamKind.WideAngle),
            };
            Assert.AreEqual(2, BackCamera.Pick(cameras));
        }

        [Test]
        public void 前向きと報告されない前の深度カメラは選ばない()
        {
            var cameras = new[]
            {
                Cam("Front TrueDepth Camera", false, WebCamKind.ColorAndDepth),
                Cam("Back Dual Wide Camera", false, WebCamKind.ColorAndDepth),
            };
            Assert.AreEqual(1, BackCamera.Pick(cameras));
        }

        [Test]
        public void 前のカメラしかなければ_それを使う_無ければマイナス1()
        {
            Assert.AreEqual(0, BackCamera.Pick(new[] { Cam("Front Camera", true, WebCamKind.WideAngle) }));
            Assert.AreEqual(-1, BackCamera.Pick(new BackCamera.Info[0]));
        }
    }
}
