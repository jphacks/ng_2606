using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR.Tests
{
    /// <summary>ScanStatusResolver：AR の状態からスキャン状態への置き換え（A_ar.md Step 9 の表）。</summary>
    public class ScanStatusResolverTests
    {
        const float Timeout = 3f;

        static (ScanStatus status, ScanReason reason) Resolve(
            ARSessionState state,
            NotTrackingReason reason = NotTrackingReason.None,
            bool deskDecided = false,
            float relocalizingSeconds = 0f)
        {
            return ScanStatusResolver.Resolve(state, reason, deskDecided, relocalizingSeconds, Timeout);
        }

        [TestCase(ARSessionState.None)]
        [TestCase(ARSessionState.Unsupported)]
        public void ARを使っていないときはNotStarted(ARSessionState state)
        {
            Assert.That(Resolve(state).status, Is.EqualTo(ScanStatus.NotStarted));
        }

        [TestCase(ARSessionState.CheckingAvailability)]
        [TestCase(ARSessionState.NeedsInstall)]
        [TestCase(ARSessionState.Installing)]
        [TestCase(ARSessionState.Ready)]
        public void 準備中はStarting(ARSessionState state)
        {
            Assert.That(Resolve(state).status, Is.EqualTo(ScanStatus.Starting));
        }

        [Test]
        public void 初期化中で理由がなければStarting()
        {
            Assert.That(Resolve(ARSessionState.SessionInitializing, NotTrackingReason.Initializing).status,
                Is.EqualTo(ScanStatus.Starting));
        }

        [Test]
        public void 追跡中で机が決まる前はFindingDesk()
        {
            Assert.That(Resolve(ARSessionState.SessionTracking, deskDecided: false).status,
                Is.EqualTo(ScanStatus.FindingDesk));
        }

        [Test]
        public void 追跡中で机が決まった後はScanning()
        {
            Assert.That(Resolve(ARSessionState.SessionTracking, deskDecided: true).status,
                Is.EqualTo(ScanStatus.Scanning));
        }

        [TestCase(NotTrackingReason.InsufficientLight, ScanReason.Dark)]
        [TestCase(NotTrackingReason.ExcessiveMotion, ScanReason.FastMotion)]
        [TestCase(NotTrackingReason.InsufficientFeatures, ScanReason.FewFeatures)]
        public void 追跡が弱い理由はLimitedの理由になる(NotTrackingReason reason, ScanReason expected)
        {
            foreach (var state in new[] { ARSessionState.SessionInitializing, ARSessionState.SessionTracking })
            {
                var r = Resolve(state, reason);
                Assert.That(r.status, Is.EqualTo(ScanStatus.Limited), state.ToString());
                Assert.That(r.reason, Is.EqualTo(expected), state.ToString());
            }
        }

        [Test]
        public void 再推定が3秒未満なら通常のまま()
        {
            Assert.That(Resolve(ARSessionState.SessionTracking, NotTrackingReason.Relocalizing,
                deskDecided: true, relocalizingSeconds: 2.9f).status, Is.EqualTo(ScanStatus.Scanning));
        }

        [Test]
        public void 再推定が3秒以上ならLost()
        {
            Assert.That(Resolve(ARSessionState.SessionTracking, NotTrackingReason.Relocalizing,
                deskDecided: true, relocalizingSeconds: 3f).status, Is.EqualTo(ScanStatus.Lost));
        }

        [Test]
        public void Limited以外の状態では理由がNoneになる()
        {
            Assert.That(Resolve(ARSessionState.SessionTracking, deskDecided: true).reason, Is.EqualTo(ScanReason.None));
        }
    }
}
