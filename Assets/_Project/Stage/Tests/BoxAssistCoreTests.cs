using System.Collections.Generic;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>BoxAssistCore：読み取りの補助の計算（B の Step 9）。</summary>
    public class BoxAssistCoreTests
    {
        const float Tolerance = 1e-5f;
        static readonly Vector2 Desk = new Vector2(0.9f, 0.44f);

        static List<BoxData> OneBox()
        {
            return new List<BoxData>
            {
                new BoxData { id = "box-01", center = new[] { 0f, 0.02f, 0f }, size = new[] { 0.2f, 0.04f, 0.1f }, source = BoxData.SourceAuto },
            };
        }

        static BoxAssistCore Core() => new BoxAssistCore(BoxAssistCore.Settings.Default);

        [Test]
        public void 机の上に一辺5cmの箱を足すと5mmにそろいmanualになる()
        {
            var boxes = new List<BoxData>();

            var box = Core().Add(boxes, new Vector2(0.1234f, -0.0517f), 0f, Desk);

            Assert.That(boxes.Count, Is.EqualTo(1));
            Assert.That(box.id, Is.EqualTo("box-01"));
            Assert.That(box.source, Is.EqualTo(BoxData.SourceManual));
            Assert.That(box.center[0], Is.EqualTo(0.125f).Within(Tolerance));
            Assert.That(box.center[2], Is.EqualTo(-0.05f).Within(Tolerance));
            Assert.That(box.size[1], Is.EqualTo(0.05f).Within(Tolerance));
            Assert.That(box.center[1], Is.EqualTo(0.025f).Within(Tolerance));
        }

        [Test]
        public void 箱の上面に足すと積んだ高さになる()
        {
            var boxes = OneBox();

            var box = Core().Add(boxes, new Vector2(0f, 0f), 0.04f, Desk);

            Assert.That(box.id, Is.EqualTo("box-02"));
            Assert.That(box.size[1], Is.EqualTo(0.09f).Within(Tolerance), "下の箱 4cm の上に 5cm");
        }

        [Test]
        public void 動かすと5mmにそろい机の外には出ない()
        {
            var core = Core();
            var boxes = OneBox();

            core.Move(boxes, "box-01", new Vector2(0.0123f, 0f), Desk);
            Assert.That(boxes[0].center[0], Is.EqualTo(0.01f).Within(Tolerance));
            Assert.That(boxes[0].source, Is.EqualTo(BoxData.SourceManual));

            core.Move(boxes, "box-01", new Vector2(5f, 5f), Desk);
            Assert.That(boxes[0].center[0], Is.EqualTo(0.45f - 0.1f).Within(Tolerance), "机の右の端で止まる");
            Assert.That(boxes[0].center[2], Is.EqualTo(0.22f - 0.05f).Within(Tolerance), "机の奥の端で止まる");
        }

        [Test]
        public void 大きさは範囲で止まり高さは底を机に付けたまま()
        {
            var core = Core();
            var boxes = OneBox();

            core.Resize(boxes, "box-01", new Vector3(1f, 1f, -1f));

            Assert.That(boxes[0].size[0], Is.EqualTo(0.6f).Within(Tolerance));
            Assert.That(boxes[0].size[1], Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(boxes[0].size[2], Is.EqualTo(0.01f).Within(Tolerance));
            Assert.That(boxes[0].center[1], Is.EqualTo(0.2f).Within(Tolerance), "中心の高さは高さの半分");
            Assert.That(boxes[0].center[0], Is.EqualTo(0f).Within(Tolerance), "幅を変えても中心は動かない");
        }

        [Test]
        public void 回すと15度ごとの向きに5度以内なら吸い付く()
        {
            var core = Core();
            var boxes = OneBox();

            core.Rotate(boxes, "box-01", 7f);
            Assert.That(boxes[0].yawDegrees, Is.EqualTo(7f).Within(Tolerance), "0度から7度は吸い付かない");

            core.Rotate(boxes, "box-01", 7f);
            Assert.That(boxes[0].yawDegrees, Is.EqualTo(15f).Within(Tolerance), "14度は15度に吸い付く");

            core.Rotate(boxes, "box-01", 7f);
            Assert.That(boxes[0].yawDegrees, Is.EqualTo(21f).Within(Tolerance), "吸い付いたあとも少しずつ回せる");

            core.Rotate(boxes, "box-01", -23f);
            Assert.That(boxes[0].yawDegrees, Is.EqualTo(0f).Within(Tolerance), "-2度は0度に吸い付く");
        }

        [Test]
        public void もどすで元に戻る()
        {
            var core = Core();
            var boxes = OneBox();
            var original = Vec3.To(boxes[0].center);

            core.Move(boxes, "box-01", new Vector2(0.1f, 0f), Desk);
            core.Add(boxes, new Vector2(-0.2f, 0f), 0f, Desk);
            core.Delete(boxes, "box-01");

            Assert.That(core.Undo(boxes), Is.True);
            Assert.That(boxes.Count, Is.EqualTo(2), "消す前に戻る");
            Assert.That(core.Undo(boxes), Is.True);
            Assert.That(boxes.Count, Is.EqualTo(1), "足す前に戻る");
            Assert.That(core.Undo(boxes), Is.True);
            Assert.That(Vec3.To(boxes[0].center), Is.EqualTo(original), "動かす前に戻る");
            Assert.That(boxes[0].source, Is.EqualTo(BoxData.SourceAuto));
            Assert.That(core.Undo(boxes), Is.False, "それより前はない");
        }

        [Test]
        public void もどすは最大20回分()
        {
            var core = Core();
            var boxes = OneBox();

            for (var i = 0; i < 25; i++)
            {
                core.Move(boxes, "box-01", new Vector2(0.01f, 0f), Desk);
            }

            Assert.That(core.UndoCount, Is.EqualTo(20));
        }

        [Test]
        public void 選んでいない箱は変えない()
        {
            var core = Core();
            var boxes = OneBox();

            Assert.That(core.Move(boxes, "", Vector2.one, Desk), Is.Null);
            Assert.That(core.Delete(boxes, "box-99"), Is.False);
            Assert.That(core.UndoCount, Is.EqualTo(0), "何も変えていないので記録しない");
        }
    }
}
