using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Solver2D : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private int lod = 0;
    [SerializeField] private float densityCoef;
    [SerializeField] private float velocityCoef;

    private RenderTexture _solverTex;
    private RenderTexture _densityTex;
    private RenderTexture _velocityTex;
    private RenderTexture _prevTex;

    /// <summary>
    /// 入力ソース
    /// </summary>
    [SerializeField] private RenderTexture sourceTex;
    public RenderTexture SourceTex
    {
        set => sourceTex = value;
        get => sourceTex;
    }

    /// <summary>
    /// プロパティ
    /// </summary>
    private const string SolverProp = "solver";
    private const string SolverTexProp = "SolverTex";
    private const string DensityProp = "density";
    private const string VelocityProp = "velocity";
    private const string PrevProp = "prev";
    private const string SourceProp = "source";
    private const string DtProp = "dt";
    private const string DensityCoefProp = "densityCoef";
    private const string VelocityCoefProp = "velocityCoef";
    private int _solverId;
    private int _solverTexId;
    private int _densityId;
    private int _velocityId;
    private int _prevId;
    private int _sourceId;
    private int _dtId;
    private int _densityCoefId;
    private int _velocityCoefId;

    /// <summary>
    /// カーネル一覧
    /// </summary>
    private enum ComputeKernels
    {
        AddSourceDensity,
        AddSourceVelocity,
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
        Initialize();
    }

    private void Update()
    {
        Draw();
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Initialize()
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
        _prevId = Shader.PropertyToID(PrevProp);
        _sourceId = Shader.PropertyToID(SourceProp);
        _dtId = Shader.PropertyToID(DtProp);
        _densityCoefId = Shader.PropertyToID(DensityCoefProp);
        _velocityCoefId = Shader.PropertyToID(VelocityCoefProp);

        // RenderTexture生成
        var width = Screen.width;
        var height = Screen.height;
        _solverTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.ARGBFloat, TextureWrapMode.Clamp, FilterMode.Point, _solverTex);
        _densityTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, TextureWrapMode.Clamp, FilterMode.Point, _densityTex);
        _velocityTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RGHalf, TextureWrapMode.Clamp, FilterMode.Point, _velocityTex);
        _prevTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.ARGBHalf, TextureWrapMode.Clamp, FilterMode.Point, _prevTex);
        Shader.SetGlobalTexture(_solverTexId, _solverTex);
    }

    /// <summary>
    /// 描画処理
    /// </summary>
    private void Draw()
    {
        // TODO パラメータの設定
        computeShader.SetFloat(_dtId, Time.deltaTime);
        computeShader.SetFloat(_densityCoefId, densityCoef);
        computeShader.SetFloat(_velocityCoefId, velocityCoef);

        // TODO 速度場、密度場の更新
        DensityStep();
        VelocityStep();

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

    /// <summary>
    /// 密度場の更新
    /// </summary>
    private void DensityStep()
    {
        // TODO 外力項
        if (SourceTex != null)
        {
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceDensity], _sourceId, SourceTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceDensity], _densityId, _densityTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceDensity], _prevId, _prevTex);
            computeShader.Dispatch(_kernelMap[ComputeKernels.AddSourceDensity],
                Mathf.CeilToInt(_solverTex.width / _gpuThreads.X),
                Mathf.CeilToInt(_solverTex.height / _gpuThreads.Y),
                1);
        }
    }

    /// <summary>
    /// 速度場の更新
    /// </summary>
    private void VelocityStep()
    {
        // TODO 外力項
        if (SourceTex != null)
        {
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceVelocity], _sourceId, SourceTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceVelocity], _velocityId, _velocityTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceVelocity], _prevId, _prevTex);
            computeShader.Dispatch(_kernelMap[ComputeKernels.AddSourceVelocity],
                Mathf.CeilToInt(_solverTex.width / _gpuThreads.X),
                Mathf.CeilToInt(_solverTex.height / _gpuThreads.Y),
                1);
        }

        // TODO 粘性項

        // TODO 入れ替え

        // TODO 移流項

        // TODO 質量
    }
}
