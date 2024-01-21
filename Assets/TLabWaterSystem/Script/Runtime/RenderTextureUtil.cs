using UnityEngine;

namespace TLab.WaterSystem
{
    public struct RenderTextureOption
    {
        public int width;
        public int height;
        public int depth;
        public RenderTextureFormat format;
        public RenderTextureReadWrite readWrite;
    }

    public static class RenderTextureUtil
    {
        public static Color32 GetPixel(float x, float y, RenderTexture target)
        {
            var current_rt = RenderTexture.active;

            RenderTexture.active = target;

            var texture = new Texture2D(target.width, target.height);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();

            RenderTexture.active = current_rt;

            return texture.GetPixel(
                (int)(x * target.width), (int)(y * target.height));
        }

        public static void CreateRenderTexture(RenderTextureOption option, ref RenderTexture texture)
        {
            texture = new RenderTexture(option.width, option.height, option.depth, option.format, option.readWrite);
        }
    }
}
