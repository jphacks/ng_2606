using NUnit.Framework;
using UnityEngine;

namespace DeskStage.UI.Tests
{
    public class ViewGestureTrackerTests
    {
        const float H = 1000f;

        ViewGestureTracker NewTracker() => new ViewGestureTracker
        {
            DegreesPerScreenHeight = 180f,
            ZoomMetersPerScreenHeight = 0.6f,
        };

        [Test]
        public void 一本指でなぞると回す量になる()
        {
            var t = NewTracker();
            t.Down(1, new Vector2(100, 100));
            var o = t.Move(1, new Vector2(600, 200), H);
            Assert.AreEqual(90f, o.OrbitDegrees.x, 1e-4f);
            Assert.AreEqual(18f, o.OrbitDegrees.y, 1e-4f);
            Assert.AreEqual(0f, o.ZoomMeters);
        }

        [Test]
        public void 二本指で広げると近づく量になり回さない()
        {
            var t = NewTracker();
            t.Down(1, new Vector2(400, 500));
            t.Down(2, new Vector2(600, 500));
            var o = t.Move(2, new Vector2(800, 500), H);
            Assert.AreEqual(0.12f, o.ZoomMeters, 1e-4f);
            Assert.AreEqual(Vector2.zero, o.OrbitDegrees);
        }

        [Test]
        public void 二本目を離しても視点が飛ばない()
        {
            var t = NewTracker();
            t.Down(1, new Vector2(400, 500));
            t.Down(2, new Vector2(600, 500));
            t.Move(1, new Vector2(300, 500), H);
            t.Up(2);
            var o = t.Move(1, new Vector2(310, 500), H);
            Assert.AreEqual(1.8f, o.OrbitDegrees.x, 1e-4f);
        }

        [Test]
        public void 知らない指と三本目の指は使わない()
        {
            var t = NewTracker();
            t.Down(1, Vector2.zero);
            t.Down(2, new Vector2(100, 0));
            t.Down(3, new Vector2(200, 0));
            Assert.AreEqual(2, t.Count);
            var o = t.Move(3, new Vector2(900, 0), H);
            Assert.AreEqual(Vector2.zero, o.OrbitDegrees);
            Assert.AreEqual(0f, o.ZoomMeters);
            o = t.Move(9, new Vector2(900, 0), H);
            Assert.AreEqual(0f, o.ZoomMeters);
        }
    }
}
