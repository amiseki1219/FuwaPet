using UnityEngine;

public class PokoFaceController : MonoBehaviour
{
    [SerializeField] private Renderer eyeLRenderer;
    [SerializeField] private Renderer eyeRRenderer;
    [SerializeField] private Renderer mouthRenderer;
    [SerializeField] private FaceExpressionDatabase database;
    [SerializeField] private string defaultExpressionKey = "Normal";

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    public string CurrentExpressionKey { get; private set; }

    private Texture2D _currentLeftEye;
    private Texture2D _currentRightEye;
    private Texture2D _currentMouth;

    private void Start()
    {
        SetExpression(defaultExpressionKey);
    }

    public void SetExpression(string key)
    {
        var data = database?.GetExpression(key);
        if (data == null) return;

        CurrentExpressionKey = key;
        _currentLeftEye  = data.leftEyeTexture;
        _currentRightEye = data.rightEyeTexture;
        _currentMouth    = data.mouthTexture;

        ApplyTexture(eyeLRenderer,  _currentLeftEye);
        ApplyTexture(eyeRRenderer,  _currentRightEye);
        ApplyTexture(mouthRenderer, _currentMouth);
    }

    public void SetEyes(Texture2D leftEyeTexture, Texture2D rightEyeTexture)
    {
        ApplyTexture(eyeLRenderer, leftEyeTexture);
        ApplyTexture(eyeRRenderer, rightEyeTexture);
    }

    public void SetMouth(Texture2D mouthTexture)
    {
        ApplyTexture(mouthRenderer, mouthTexture);
    }

    public void RestoreCurrentExpressionEyes()
    {
        ApplyTexture(eyeLRenderer, _currentLeftEye);
        ApplyTexture(eyeRRenderer, _currentRightEye);
    }

    public void RestoreCurrentExpressionMouth()
    {
        ApplyTexture(mouthRenderer, _currentMouth);
    }

    private void ApplyTexture(Renderer r, Texture2D tex)
    {
        if (r == null || tex == null) return;
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetTexture(BaseMapId, tex);
        r.SetPropertyBlock(mpb);
    }
}
