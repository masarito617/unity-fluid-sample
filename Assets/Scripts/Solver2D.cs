using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Solver2D : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private int lod = 0;

    private RenderTexture _solverTex;
    private RenderTexture _densityTex;
    private RenderTexture _velocityTex;

    /// <summary>
    /// プロパティ
    /// </summary>
    private const string SolverProp = "solver";
    private const string SolverTexProp = "SolverTex";
    private const string DensityProp = "density";
    private const string VelocityProp = "velocity";
    private int _solverId;
    private int _solverTexId;
    private int _densityId;
    private int _velocityId;

    /// <summary>
    /// カーネル一覧
    /// </summary>
    private enum ComputeKernels
    {
        Draw,
    }
    private Dictionary<ComputeKernels, int> _kernelMap = new Dictionary<ComputeKernels, int>();

    /// <summary>
    /// GPUスレッド数
    /// </summary>
    private struct GPUThreads
    {
        public int X;
        public int Y;
        public int Z;

        public GPUThreads(uint x, uint y, uint z)
        {
            X = (int)x;
            Y = (int)y;
            Z = (int)z;
        }
    }
    private GPUThreads _gpuThreads;

    private void Start()
    {
        // カーネル一覧の取得
        _kernelMap = System.Enum.GetValues(typeof(ComputeKernels))
            .Cast<ComputeKernels>()
            .ToDictionary(t => t, t => computeShader.FindKernel(t.ToString()));

        // GPUスレッド数の取得
        computeShader.GetKernelThreadGroupSizes(_kernelMap[ComputeKernels.Draw], out var threadX, out var threadY, out var threadZ);
        _gpuThreads = new GPUThreads(threadX, threadY, threadZ);

        // プロパティID取得
        _solverId = Shader.PropertyToID(SolverProp);
        _solverTexId = Shader.PropertyToID(SolverTexProp);
        _densityId = Shader.PropertyToID(DensityProp);
        _velocityId = Shader.PropertyToID(VelocityProp);

        // RenderTexture生成
        var width = Screen.width;
        var height = Screen.height;
        _solverTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.ARGBFloat, TextureWrapMode.Clamp, FilterMode.Point, _solverTex);
        _densityTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, TextureWrapMode.Clamp, FilterMode.Point, _densityTex);
        _velocityTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RGHalf, TextureWrapMode.Clamp, FilterMode.Point, _velocityTex);
        Shader.SetGlobalTexture(_solverTexId, _solverTex);
    }

    private void Update()
    {
        // 描画
        computeShader.SetTexture(_kernelMap[ComputeKernels.Draw], _densityId, _densityTex);
        computeShader.SetTexture(_kernelMap[ComputeKernels.Draw], _velocityId, _velocityTex);
        computeShader.SetTextureFromGlobal(_kernelMap[ComputeKernels.Draw], _solverId, _solverTexId);
        computeShader.Dispatch(_kernelMap[ComputeKernels.Draw],
            Mathf.CeilToInt(_solverTex.width / _gpuThreads.X),
            Mathf.CeilToInt(_solverTex.height / _gpuThreads.Y),
            1);
        Shader.SetGlobalTexture(_solverTexId, _solverTex);
    }

    private void OnDestroy()
    {
        // TODO
    }
}
