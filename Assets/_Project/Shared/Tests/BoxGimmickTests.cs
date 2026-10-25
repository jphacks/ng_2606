using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Shared.Tests
{
    /// <summary>箱のしかけ（BoxData.gimmick）の保存と読み込み。</summary>
    public class BoxGimmickTests
    {
        [Test]
        public void しかけはJSONに保存して読み戻せる()
        {
            var stage = new StageData { boxes = new[] { new BoxData { id = "box-01", gimmick = BoxData.GimmickFire } } };
            var restored = JsonUtility.FromJson<StageData>(JsonUtility.ToJson(stage));
            Assert.AreEqual(BoxData.GimmickFire, restored.boxes[0].gimmick);
        }

        [Test]
        public void しかけの無い古い保存データは空として読める()
        {
            const string old = "{\"version\":2,\"boxes\":[{\"id\":\"box-01\",\"center\":[0,0.05,0],\"size\":[0.1,0.1,0.1],\"yawDegrees\":0,\"source\":\"auto\",\"texture\":\"\"}]}";
            var restored = JsonUtility.FromJson<StageData>(old);
            Assert.AreEqual("", restored.boxes[0].gimmick);
        }

        [Test]
        public void しかけの名前は決まった文字()
        {
            Assert.AreEqual("water", BoxData.GimmickWater);
            Assert.AreEqual("fire", BoxData.GimmickFire);
            Assert.AreEqual("heal", BoxData.GimmickHeal);
        }
    }
}
