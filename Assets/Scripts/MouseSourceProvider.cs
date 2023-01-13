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

    // 更新
    private void UpdateSource()
    {
        var mousePos = Input.mousePosition;
        var dpdt = UpdateMousePos(mousePos);

        // マウス入力がある場合
        if (Input.GetMouseButton(0))
        {
            var velocitySource = Vector2.ClampMagnitude(dpdt, 1.0f);
            var uv = Camera.main.ScreenToViewportPoint(mousePos);
            addSourceMaterial.SetVector(_source2dId, new Vector4(velocitySource.x, velocitySource.y, uv.x, uv.y)); // 移動の大きさ, 現在の座標
            addSourceMaterial.SetFloat(_sourceRadiusId, sourceRadius);
            Graphics.Blit(null, _addSourceTex, addSourceMaterial);
            NotifySourceTexUpdate(_addSourceTex);
        }
        else
        {
            NotifySourceTexUpdate(null);
        }
    }

    // 破棄
    private void ReleaseForceField()
    {
        Destroy(_addSourceTex);
    }

    // 通知処理
    private void NotifySourceTexUpdate(RenderTexture sourceTex)
    {
        onSourceUpdated.Invoke(sourceTex);
    }

    private Vector3 _lastMousePos;
    private Vector3 UpdateMousePos(Vector3 mousePos)
    {
        var dpdt = mousePos - _lastMousePos;
        _lastMousePos = mousePos;
        return dpdt;
    }

    [Serializable]
    public class SourceEvent : UnityEngine.Events.UnityEvent<RenderTexture> { }
}
