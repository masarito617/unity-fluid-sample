using System;
using UnityEngine;

public class MouseSourceProvider : MonoBehaviour
{
    [SerializeField] private Material addSourceMaterial;
    [SerializeField] private float sourceRadius;
    [SerializeField] private SourceEvent onSourceUpdated;
    private int _source2dId;
    private int _sourceRadiusId;

    private RenderTexture _addSourceTex;

    private void Awake()
    {
        _source2dId = Shader.PropertyToID("_Source");
        _sourceRadiusId = Shader.PropertyToID("_Radius");
    }

    private void Update()
    {
        InitializeSourceTex(Screen.width, Screen.height);
        UpdateSource();
    }

    private void OnDestroy()
    {
        ReleaseForceField();
    }

    // 初期化
    private void InitializeSourceTex(int width, int height)
    {
        if (_addSourceTex == null || _addSourceTex.width != width || _addSourceTex.height != height)
        {
            ReleaseForceField();
            _addSourceTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        }
    }

    private Vector3 _lastMousePos;

    // 更新
    private void UpdateSource()
    {
        // マウス入力を取得
        var mousePos = Input.mousePosition;
        var dpdt = mousePos - _lastMousePos;
        _lastMousePos = mousePos;

        // マウス入力がある場合、SourceTexを生成
        if (Input.GetMouseButton(0))
        {
            var velocitySource = Vector2.ClampMagnitude(dpdt, 1.0f);
            var uv = Camera.main.ScreenToViewportPoint(mousePos);
            addSourceMaterial.SetVector(_source2dId, new Vector4(velocitySource.x, velocitySource.y, uv.x, uv.y)); // 移動の大きさ(x,y), 現在のUV座標(z,w)
            addSourceMaterial.SetFloat(_sourceRadiusId, sourceRadius);
            Graphics.Blit(null, _addSourceTex, addSourceMaterial);
            NotifySourceTexUpdate(_addSourceTex);
        }
        else
        {
            NotifySourceTexUpdate(null);
        }
    }

    // 通知処理
    private void NotifySourceTexUpdate(RenderTexture sourceTex)
    {
        onSourceUpdated.Invoke(sourceTex);
    }

    // 破棄
    private void ReleaseForceField()
    {
        Destroy(_addSourceTex);
    }

    [Serializable]
    public class SourceEvent : UnityEngine.Events.UnityEvent<RenderTexture> { }
}
