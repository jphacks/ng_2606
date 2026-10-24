using System;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 写真を読んだ Claude の答え（Docs/LlmScanFormat.md、scan_result.schema.json と同じ形）。
    /// JsonUtility で読むため、名前は JSON の項目名とそろえる。長さは cm。
    /// </summary>
    [Serializable]
    public class LlmScanAnswer
    {
        public bool paperFound;
        public string scaleReference = "";
        // JsonUtility は null を読めないので、desk が null のときも中身が 0 の物になる
        public LlmDesk desk;
        // 1枚目（真上）の写真の中の、机の四隅（絵を貼るため）。なければ中身が 0
        public LlmQuad deskInPhoto1;
        public LlmBlock[] blocks;
        public string notes = "";
    }

    [Serializable]
    public class LlmDesk
    {
        public float widthCm;
        public float depthCm;
    }

    [Serializable]
    public class LlmBlock
    {
        public string label = "";
        public float centerXCm;
        public float centerZCm;
        public float widthCm;
        public float depthCm;
        public float heightCm;
        public float yawDegrees;
        public float confidence;
        // 1枚目（真上）の写真の中の、上面の四隅（絵を貼るため）。なければ中身が 0
        public LlmQuad topFaceInPhoto1;
    }

    /// <summary>写真の中の四隅。写真の左上が (0, 0)、右下が (1, 1) の割合。ぐるっと一周する順。</summary>
    [Serializable]
    public class LlmQuad
    {
        public float x0, y0, x1, y1, x2, y2, x3, y3;

        public Vector2[] ToPoints() => new[]
        {
            new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(x2, y2), new Vector2(x3, y3),
        };
    }
}
