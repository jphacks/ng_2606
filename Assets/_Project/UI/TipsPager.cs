namespace DeskStage.UI
{
    /// <summary>
    /// コツの絵をめくる計算。計算だけの普通のクラス（Step 9）。
    /// 指を左になぞると次の絵、右になぞると前の絵。短いなぞりはめくらない。
    /// </summary>
    public class TipsPager
    {
        /// <summary>めくるのに必要ななぞる量（画面の幅に対する割合）</summary>
        public const float SwipeRatio = 0.08f;

        public int Count { get; private set; }
        public int Index { get; private set; }

        public TipsPager(int count)
        {
            Count = count < 0 ? 0 : count;
        }

        public bool IsLast => Count == 0 || Index == Count - 1;

        /// <summary>なぞり終えたときに呼ぶ。dx は横に動いた量（右が正）。めくったら true。</summary>
        public bool EndSwipe(float dx, float screenWidth)
        {
            if (screenWidth <= 0f || System.Math.Abs(dx) < screenWidth * SwipeRatio)
            {
                return false;
            }
            return dx < 0f ? Go(Index + 1) : Go(Index - 1);
        }

        public bool Go(int index)
        {
            if (index < 0 || index >= Count || index == Index)
            {
                return false;
            }
            Index = index;
            return true;
        }

        /// <summary>今の絵の位置を表す印（今の絵が●、ほかが○）。</summary>
        public string Dots()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(i == Index ? '●' : '○');
            }
            return sb.ToString();
        }
    }
}
