namespace VRS
{
    public class VideoNode
    {
        public Video Data { get; set; }
        public VideoNode Left { get; set; }
        public VideoNode Right { get; set; }

        public VideoNode(Video video)
        {
            Data = video;
            Left = null;
            Right = null;
        }
    }
}
