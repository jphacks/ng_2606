using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Shared.Tests
{
    /// <summary>
    /// 全体 Step 3 の3：StageData を JSON にして戻すと同じ値になること、
    /// Vector3 の変換が合っていることを確かめる。
    /// </summary>
    public class StageDataTests
    {
        static StageData MakeSample()
        {
            return new StageData
            {
                id = "stage-001",
                name = "おためし",
                createdAt = "2026-10-23T15:00:00Z",
                desk = new DeskData { width = 1.2f, depth = 0.6f },
                boxes = new[]
                {
                    new BoxData
                    {
                        id = "box-01",
                        center = Vec3.From(new Vector3(0.1f, 0.02f, -0.05f)),
                        size = Vec3.From(new Vector3(0.15f, 0.04f, 0.1f)),
                        yawDegrees = 30f,
                        source = BoxData.SourceAuto,
                        texture = "",
                    },
                    new BoxData
                    {
                        id = "box-02",
                        center = Vec3.From(new Vector3(-0.2f, 0.035f, 0.12f)),
                        size = Vec3.From(new Vector3(0.08f, 0.07f, 0.08f)),
                        yawDegrees = -12.5f,
                        source = BoxData.SourceManual,
                        texture = "box-02.png",
                    },
                },
                spawn = new SpawnData
                {
                    position = Vec3.From(new Vector3(0.3f, 0.04f, 0.2f)),
                    yawDegrees = 90f,
                },
                goal = new GoalData
                {
                    position = Vec3.From(new Vector3(-0.3f, 0.07f, -0.2f)),
                    radius = 0.02f,
                },
                checkpoints = new[]
                {
                    new GoalData { position = Vec3.From(new Vector3(0f, 0.02f, 0f)), radius = 0.015f },
                },
                fallDepth = 0.10f,
            };
        }

        [Test]
        public void JSONにして戻すと同じ値になる()
        {
            var original = MakeSample();

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<StageData>(json);

            Assert.AreEqual(StageData.CurrentVersion, restored.version);
            Assert.AreEqual(original.id, restored.id);
            Assert.AreEqual(original.name, restored.name);
            Assert.AreEqual(original.createdAt, restored.createdAt);
            Assert.AreEqual(original.desk.width, restored.desk.width);
            Assert.AreEqual(original.desk.depth, restored.desk.depth);
            Assert.AreEqual(original.fallDepth, restored.fallDepth);

            Assert.AreEqual(original.boxes.Length, restored.boxes.Length);
            for (var i = 0; i < original.boxes.Length; i++)
            {
                var a = original.boxes[i];
                var b = restored.boxes[i];
                Assert.AreEqual(a.id, b.id);
                Assert.AreEqual(Vec3.To(a.center), Vec3.To(b.center));
                Assert.AreEqual(Vec3.To(a.size), Vec3.To(b.size));
                Assert.AreEqual(a.yawDegrees, b.yawDegrees);
                Assert.AreEqual(a.source, b.source);
                Assert.AreEqual(a.texture, b.texture);
            }

            Assert.AreEqual(Vec3.To(original.spawn.position), Vec3.To(restored.spawn.position));
            Assert.AreEqual(original.spawn.yawDegrees, restored.spawn.yawDegrees);
            Assert.AreEqual(Vec3.To(original.goal.position), Vec3.To(restored.goal.position));
            Assert.AreEqual(original.goal.radius, restored.goal.radius);
            Assert.AreEqual(original.checkpoints.Length, restored.checkpoints.Length);
        }

        [Test]
        public void 既定のStageDataはversion2である()
        {
            Assert.AreEqual(2, new StageData().version);
            Assert.AreEqual(2, StageData.CurrentVersion);
        }

        [Test]
        public void Vector3と配列を往復しても同じ値になる()
        {
            var value = new Vector3(1.5f, -2.25f, 0.125f);

            var array = Vec3.From(value);

            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(1.5f, array[0]);
            Assert.AreEqual(-2.25f, array[1]);
            Assert.AreEqual(0.125f, array[2]);
            Assert.AreEqual(value, Vec3.To(array));
        }

        [Test]
        public void 長さが3でない配列はVector3_zeroになる()
        {
            Assert.AreEqual(Vector3.zero, Vec3.To(null));
            Assert.AreEqual(Vector3.zero, Vec3.To(new float[0]));
            Assert.AreEqual(Vector3.zero, Vec3.To(new[] { 1f, 2f }));
            Assert.AreEqual(Vector3.zero, Vec3.To(new[] { 1f, 2f, 3f, 4f }));
        }

        [Test]
        public void Zeroはすべて0の長さ3の配列を返す()
        {
            var zero = Vec3.Zero();

            Assert.AreEqual(3, zero.Length);
            Assert.AreEqual(Vector3.zero, Vec3.To(zero));
        }
    }
}
