using System;

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
    }
}
