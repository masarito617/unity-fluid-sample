using System;
using UnityEngine;

public class RenderEffect : MonoBehaviour
{
    [SerializeField] private String propName = "_PropName";
    [SerializeField] private Material[] effects;
    [SerializeField] private int downSample = 0;

    private RenderTexture _output;
    private readonly RenderTexture[] _rts = new RenderTexture[2];

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // レンダーテクスチャ生成
        CreateRTs(src);

        // エフェクト適用
        Graphics.Blit(src, _rts[0]);
        foreach (var effect in effects)
        {
            Graphics.Blit(_rts[0], _rts[1], effect);
            (_rts[0], _rts[1]) = (_rts[1], _rts[0]); // Swap
        }

        // 最終的な結果を設定
        Graphics.Blit(_rts[0], _output);
        Shader.SetGlobalTexture(propName, _output); // グローバルなテクスチャを設定
        Graphics.Blit(_output, dest);
    }

    private void CreateRTs(RenderTexture src)
    {
        // レンダーテクスチャの生成
        if (_rts[0] == null
            || _rts[0].width != src.width >> downSample
            || _rts[0].height != src.height >> downSample)
        {
            for (var i = 0; i < _rts.Length; i++)
            {
                _rts[i] = RenderUtility.CreateRenderTexture(src.width >> downSample, src.height >> downSample, 16, RenderTextureFormat.ARGB32, TextureWrapMode.Repeat, FilterMode.Bilinear, _rts[i]);
            }
            _output = RenderUtility.CreateRenderTexture(src.width >> downSample, src.height >> downSample, 16, RenderTextureFormat.ARGB32, TextureWrapMode.Repeat, FilterMode.Bilinear, _output);
        }
    }
}
