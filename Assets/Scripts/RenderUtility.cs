using UnityEngine;

public class RenderUtility : MonoBehaviour
{
    public static RenderTexture CreateRenderTexture(int width, int height, int depth,
        RenderTextureFormat format, TextureWrapMode wrapMode, FilterMode filterMode,
        RenderTexture rt = null)
    {
        if (rt != null)
        {
            if (rt.width == width && rt.height == height) return rt;
        }

        ReleaseRenderTexture(rt);
        rt = new RenderTexture(width, height, depth, format);
        rt.enableRandomWrite = true;
        rt.wrapMode = wrapMode;
        rt.filterMode = filterMode;
        rt.Create();
        ClearRenderTexture(rt, Color.clear);
        return rt;
    }

    private static void ReleaseRenderTexture(RenderTexture rt)
    {
        if (rt == null) return;

        rt.Release();
        Destroy(rt);
    }

    private static void ClearRenderTexture(RenderTexture target, Color bg)
    {
        var active = RenderTexture.active;
        RenderTexture.active = target;
        GL.Clear(true, true, bg);
        RenderTexture.active = active;
    }
}
