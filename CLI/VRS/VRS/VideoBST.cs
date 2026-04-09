using System;
using System.Collections.Generic;

namespace VRS
{
    public class VideoBST
    {
        public VideoNode Root { get; private set; }

        public void Insert(Video video)
        {
            Root = InsertRec(Root, video);
        }

        private VideoNode InsertRec(VideoNode root, Video video)
        {
            if (root == null)
                return new VideoNode(video);

            if (video.VideoID < root.Data.VideoID)
                root.Left = InsertRec(root.Left, video);
            else
                root.Right = InsertRec(root.Right, video);

            return root;
        }

        public Video Search(int videoID)
        {
            return SearchRec(Root, videoID);
        }

        private Video SearchRec(VideoNode root, int videoID)
        {
            if (root == null)
                return null;

            if (videoID == root.Data.VideoID)
                return root.Data;

            if (videoID < root.Data.VideoID)
                return SearchRec(root.Left, videoID);

            return SearchRec(root.Right, videoID);
        }

        public List<Video> InOrder()
        {
            List<Video> list = new List<Video>();
            InOrderRec(Root, list);
            return list;
        }

        private void InOrderRec(VideoNode root, List<Video> list)
        {
            if (root != null)
            {
                InOrderRec(root.Left, list);
                list.Add(root.Data);
                InOrderRec(root.Right, list);
            }
        }

        public void Clear()
        {
            Root = null;
        }
    }
}
