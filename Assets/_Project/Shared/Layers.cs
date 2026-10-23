namespace DeskStage.Shared
{
    /// <summary>
    /// レイヤー番号の定数（00_overview.md 3.2）。
    /// 各担当はレイヤー番号を直接書かず、ここの定数を使う。
    /// </summary>
    public static class Layers
    {
        /// <summary>机の板と箱の当たり判定。キャラが乗れるもの。</summary>
        public const int Terrain = 8;

        /// <summary>スキャン中とたしかめ画面だけ見せる物。</summary>
        public const int ScanView = 9;

        /// <summary>キャラ。</summary>
        public const int Player = 10;

        /// <summary>予約（色ギミックの拡張で使う。今は使わない）。</summary>
        public const int Gimmick = 11;

        /// <summary>Terrain だけを対象にするマスク値。レイキャストの絞り込みに使う。</summary>
        public const int TerrainMask = 1 << Terrain;
    }
}
