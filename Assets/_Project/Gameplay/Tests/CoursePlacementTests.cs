using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// コースの物を置く決まり（C_gameplay.md の Step 12）を確かめる。
    /// </summary>
    public class CoursePlacementTests
    {
        const float Eps = 1e-5f;

        [Test]
        public void 当たらなければ置けない()
        {
            Assert.AreEqual(CoursePlacement.ReasonNotOnTerrain, CoursePlacement.CheckSurface(false, Vector3.zero));
        }

        [Test]
        public void 箱の側面には置けない()
        {
            Assert.AreEqual(CoursePlacement.ReasonNotTop, CoursePlacement.CheckSurface(true, Vector3.right));
        }

        [Test]
        public void 上向きの面には置ける()
        {
            Assert.IsNull(CoursePlacement.CheckSurface(true, Vector3.up));
        }

        [Test]
        public void スタートは1mm上に置き向きも書く()
        {
            var stage = new StageData();
            var (result, _) = CoursePlacement.Apply(stage, CourseItem.Start, new Vector3(0.1f, 0.02f, 0f), 90f);
            Assert.AreEqual(PlacementResult.Placed, result);
            Assert.AreEqual(0.021f, stage.spawn.position[1], Eps);
            Assert.AreEqual(90f, stage.spawn.yawDegrees);
        }

        [Test]
        public void ゴールは置き直すと動く()
        {
            var stage = new StageData();
            CoursePlacement.Apply(stage, CourseItem.Goal, new Vector3(0.1f, 0f, 0f), 0f);
            CoursePlacement.Apply(stage, CourseItem.Goal, new Vector3(-0.2f, 0.05f, 0.1f), 0f);
            Assert.AreEqual(-0.2f, stage.goal.position[0], Eps);
            Assert.AreEqual(CoursePlacement.DefaultRadius, stage.goal.radius);
        }

        [Test]
        public void チェックポイントは5個まで()
        {
            var stage = new StageData();
            for (int i = 0; i < 5; i++)
            {
                var (r, _) = CoursePlacement.Apply(stage, CourseItem.Checkpoint, new Vector3(i * 0.1f, 0f, 0f), 0f);
                Assert.AreEqual(PlacementResult.Placed, r);
            }
            var (result, reason) = CoursePlacement.Apply(stage, CourseItem.Checkpoint, new Vector3(0f, 0f, 0.3f), 0f);
            Assert.AreEqual(PlacementResult.Rejected, result);
            Assert.AreEqual(CoursePlacement.ReasonCheckpointFull, reason);
            Assert.AreEqual(5, stage.checkpoints.Length);
        }

        [Test]
        public void 既にあるチェックポイントの3cm以内をタップすると消す()
        {
            var stage = new StageData();
            CoursePlacement.Apply(stage, CourseItem.Checkpoint, new Vector3(0f, 0f, 0f), 0f);
            CoursePlacement.Apply(stage, CourseItem.Checkpoint, new Vector3(0.2f, 0f, 0f), 0f);
            var (result, reason) = CoursePlacement.Apply(stage, CourseItem.Checkpoint, new Vector3(0.02f, 0f, 0f), 0f);
            Assert.AreEqual(PlacementResult.Placed, result);
            Assert.AreEqual(CoursePlacement.ReasonCheckpointRemoved, reason);
            Assert.AreEqual(1, stage.checkpoints.Length);
            Assert.AreEqual(0.2f, stage.checkpoints[0].position[0], Eps);
        }

        [Test]
        public void 向きは0以上360未満()
        {
            Assert.AreEqual(0f, CoursePlacement.YawFromDirection(Vector3.forward), Eps);
            Assert.AreEqual(90f, CoursePlacement.YawFromDirection(Vector3.right), Eps);
            Assert.AreEqual(270f, CoursePlacement.YawFromDirection(Vector3.left), Eps);
        }
    }
}
