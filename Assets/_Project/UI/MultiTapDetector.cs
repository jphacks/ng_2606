namespace DeskStage.UI
{
    /// <summary>
    /// 「3本指で2回たたく」を見分ける。計算だけの普通のクラス（Step 6 の1）。
    /// 3本の指がそろった瞬間を1回とし、間隔が MaxInterval 秒以内に2回そろったら成立。
    /// </summary>
    public class MultiTapDetector
    {
        public float MaxInterval = 0.4f;
        public int Fingers = 3;

        float lastTapTime = float.NegativeInfinity;
        bool wasDown;

        /// <summary>毎フレーム、今さわっている指の数と時刻を渡す。成立したフレームだけ true。</summary>
        public bool Update(int touchCount, float time)
        {
            var down = touchCount >= Fingers;
            var began = down && !wasDown;
            wasDown = down;
            if (!began)
            {
                return false;
            }
            if (time - lastTapTime <= MaxInterval)
            {
                lastTapTime = float.NegativeInfinity;
                return true;
            }
            lastTapTime = time;
            return false;
        }
    }
}
