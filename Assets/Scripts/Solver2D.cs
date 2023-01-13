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
    /// MouseSourceProviderから設定される
    /// </summary>
    [SerializeField] private RenderTexture sourceTex;
    public RenderTexture SourceTex
    {
        set => sourceTex = value;
        get => sourceTex;
    }

    /// <summary>
    /// シェーダープロパティ
    /// enum名はプロパティ名と合わせること
    /// </summary>
    private enum ShaderProps
    {
        solver,
        SolverTex,
        density,
        velocity,
        prev,
        source,
        dt,
        densityCoef,
        velocityCoef,
    }
    private Dictionary<ShaderProps, int> _shaderPropIdMap = new Dictionary<ShaderProps, int>();

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

        // プロパティIDの取得
        _shaderPropIdMap = System.Enum.GetValues(typeof(ShaderProps))
            .Cast<ShaderProps>()
            .ToDictionary(t => t, t => Shader.PropertyToID(t.ToString()));

        // RenderTexture生成
        var width = Screen.width;
        var height = Screen.height;
        _solverTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.ARGBFloat, TextureWrapMode.Clamp, FilterMode.Point, _solverTex);
        _densityTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, TextureWrapMode.Clamp, FilterMode.Point, _densityTex);
        _velocityTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RGHalf, TextureWrapMode.Clamp, FilterMode.Point, _velocityTex);
        _prevTex = RenderUtility.CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.ARGBHalf, TextureWrapMode.Clamp, FilterMode.Point, _prevTex);
        Shader.SetGlobalTexture(_shaderPropIdMap[ShaderProps.SolverTex], _solverTex);
    }

    /// <summary>
    /// 描画処理
    /// </summary>
    private void Draw()
    {
        // パラメータの設定
        computeShader.SetFloat(_shaderPropIdMap[ShaderProps.dt], Time.deltaTime);
        computeShader.SetFloat(_shaderPropIdMap[ShaderProps.densityCoef], densityCoef);
        computeShader.SetFloat(_shaderPropIdMap[ShaderProps.velocityCoef], velocityCoef);

        // 密度場、速度場の更新
        DensityStep();
        VelocityStep();

        // 描画
        computeShader.SetTexture(_kernelMap[ComputeKernels.Draw], _shaderPropIdMap[ShaderProps.density], _densityTex);
        computeShader.SetTexture(_kernelMap[ComputeKernels.Draw], _shaderPropIdMap[ShaderProps.velocity], _velocityTex);
        computeShader.SetTextureFromGlobal(_kernelMap[ComputeKernels.Draw], _shaderPropIdMap[ShaderProps.solver], _shaderPropIdMap[ShaderProps.SolverTex]);
        computeShader.Dispatch(_kernelMap[ComputeKernels.Draw],
            Mathf.CeilToInt(_solverTex.width / _gpuThreads.X),
            Mathf.CeilToInt(_solverTex.height / _gpuThreads.Y),
            1);
        Shader.SetGlobalTexture(_shaderPropIdMap[ShaderProps.SolverTex], _solverTex);
    }

    /// <summary>
    /// 密度場の更新
    /// </summary>
    private void DensityStep()
    {
        // 外力項
        if (SourceTex != null)
        {
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceDensity], _shaderPropIdMap[ShaderProps.source], SourceTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceDensity], _shaderPropIdMap[ShaderProps.density], _densityTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceDensity], _shaderPropIdMap[ShaderProps.prev], _prevTex);
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
        // 外力項
        if (SourceTex != null)
        {
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceVelocity], _shaderPropIdMap[ShaderProps.source], SourceTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceVelocity], _shaderPropIdMap[ShaderProps.velocity], _velocityTex);
            computeShader.SetTexture(_kernelMap[ComputeKernels.AddSourceVelocity], _shaderPropIdMap[ShaderProps.prev], _prevTex);
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
