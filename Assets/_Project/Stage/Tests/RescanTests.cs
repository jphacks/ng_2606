using System.Collections.Generic;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>もういちどスキャン（B の Step 8）。</summary>
    public class RescanTests
    {
        static SurfaceData Surface(int id, float height, float cx, float cz, float size)
        {
            var h = size / 2f;
            return new SurfaceData
            {
                Id = id,
                Height = height,
                Outline = new[] { new Vector2(cx - h, cz - h), new Vector2(cx + h, cz - h), new Vector2(cx + h, cz + h), new Vector2(cx - h, cz + h) },
            };
        }

        [Test]
        public void manualの箱も含めてすべて消える()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();
            core.Apply(new[] { Surface(1, 0.02f, -0.2f, 0f, 0.1f), Surface(2, 0.03f, 0.2f, 0f, 0.1f) }, boxes);
            boxes[1].source = BoxData.SourceManual;

            var changes = core.ClearAll(boxes);

            Assert.That(boxes, Is.Empty);
            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(changes[0].change, Is.EqualTo(BoxChange.Removed));
            Assert.That(changes[0].source, Is.EqualTo(BoxData.SourceAuto));
            Assert.That(changes[1].boxId, Is.EqualTo("box-02"));
            Assert.That(changes[1].source, Is.EqualTo(BoxData.SourceManual), "消した箱の source をそのまま知らせる");
        }

        [Test]
        public void 動かした後の位置に箱ができ前の位置の箱は残らない()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();
            core.Apply(new[] { Surface(1, 0.02f, -0.2f, 0f, 0.1f), Surface(2, 0.03f, 0.2f, 0f, 0.1f) }, boxes);

            core.ClearAll(boxes);
            // A は並べ直した後の上面に、新しい番号を付けて知らせてくる（同じ番号が来ることもある）
            var changes = core.Apply(new[] { Surface(1, 0.02f, 0f, 0.1f, 0.1f), Surface(5, 0.03f, 0.3f, -0.1f, 0.1f) }, boxes);

            Assert.That(boxes.Count, Is.EqualTo(2));
            Assert.That(boxes[0].id, Is.EqualTo("box-01"), "連番も最初からになる");
            Assert.That(boxes[0].center[0], Is.EqualTo(0f).Within(1e-5f));
            Assert.That(boxes[0].center[2], Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(boxes[1].center[0], Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(changes.TrueForAll(c => c.change == BoxChange.Added), Is.True);
        }

        [Test]
        public void スタートとゴールとチェックポイントが消える()
        {
            var stage = new StageData
            {
                spawn = new SpawnData { position = new[] { -0.38f, 0f, 0f }, yawDegrees = 90f },
                goal = new GoalData { position = new[] { 0.4f, 0f, 0f }, radius = 0.03f },
                checkpoints = new[] { new GoalData { position = new[] { -0.095f, 0.03f, 0f }, radius = 0.03f } },
            };

            StageCourse.Clear(stage);

            Assert.That(Vec3.To(stage.spawn.position), Is.EqualTo(Vector3.zero));
            Assert.That(stage.spawn.yawDegrees, Is.EqualTo(0f));
            Assert.That(stage.goal.radius, Is.EqualTo(0f));
            Assert.That(stage.checkpoints, Is.Empty);
        }

        [Test]
        public void ステージがないときは何もしない()
        {
            Assert.DoesNotThrow(() => StageCourse.Clear(null));
        }
    }
}
