using UnityEngine;

/// <summary>
/// ノッチ・ホームインジケータを避けるための RectTransform 調整コンポーネント。
///
/// 使い方:
///   Canvas の直下に空の GameObject（例: SafeArea）を作って RectTransform ごとアタッチし、
///   UI はすべてその下に入れる。Canvas 自体には付けないこと。
///
/// 端末の回転・解像度変更・iOS の safeArea 変化に自動で追従する。
/// エディタでは Device Simulator を使うと効果を確認できる。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("避ける辺")]
    [Tooltip("上辺を safeArea に合わせる（ノッチ / ダイナミックアイランド）")]
    [SerializeField] private bool applyTop = true;

    [Tooltip("下辺を safeArea に合わせる（ホームインジケータ）")]
    [SerializeField] private bool applyBottom = true;

    [Tooltip("左辺を safeArea に合わせる（横向き時のノッチ）")]
    [SerializeField] private bool applyLeft = true;

    [Tooltip("右辺を safeArea に合わせる（横向き時のノッチ）")]
    [SerializeField] private bool applyRight = true;

    [Header("デバッグ")]
    [Tooltip("適用時に Console へログを出す")]
    [SerializeField] private bool logOnApply = true;

    private RectTransform _rect;

    // 前回適用時の状態。変化したときだけ再適用する
    private Rect _lastSafeArea = Rect.zero;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Invalidate();
        Apply();
    }

    private void Update()
    {
        if (HasChanged()) Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Inspector で辺のチェックを変えたら即反映する
        Invalidate();
    }
#endif

    /// <summary>次のフレームで必ず再適用させる</summary>
    public void Invalidate()
    {
        _lastScreenWidth = -1;
        _lastScreenHeight = -1;
        _lastSafeArea = Rect.zero;
    }

    private bool HasChanged()
    {
        return Screen.safeArea != _lastSafeArea
            || Screen.width != _lastScreenWidth
            || Screen.height != _lastScreenHeight
            || Screen.orientation != _lastOrientation;
    }

    private void Apply()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        if (_rect == null) return;

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        // 起動直後などで 0 が返ることがある。0 除算を避ける
        if (screenWidth <= 0 || screenHeight <= 0) return;

        Rect safe = Screen.safeArea;

        // safeArea が不正なら何もしない（一部の環境で 0 サイズが返る）
        if (safe.width <= 0f || safe.height <= 0f) return;

        float xMin = applyLeft ? safe.xMin : 0f;
        float xMax = applyRight ? safe.xMax : screenWidth;
        float yMin = applyBottom ? safe.yMin : 0f;
        float yMax = applyTop ? safe.yMax : screenHeight;

        Vector2 anchorMin = new Vector2(xMin / screenWidth, yMin / screenHeight);
        Vector2 anchorMax = new Vector2(xMax / screenWidth, yMax / screenHeight);

        if (!IsValid(anchorMin) || !IsValid(anchorMax)) return;
        if (anchorMax.x <= anchorMin.x || anchorMax.y <= anchorMin.y) return;

        _rect.anchorMin = anchorMin;
        _rect.anchorMax = anchorMax;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.localScale = Vector3.one;
        _rect.localRotation = Quaternion.identity;

        _lastSafeArea = safe;
        _lastScreenWidth = screenWidth;
        _lastScreenHeight = screenHeight;
        _lastOrientation = Screen.orientation;

        if (logOnApply && Application.isPlaying)
        {
            Debug.Log($"[SafeArea] 適用 screen={screenWidth}x{screenHeight} safeArea={safe} " +
                      $"anchorMin={anchorMin} anchorMax={anchorMax}", this);
        }
    }

    private static bool IsValid(Vector2 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y)
            && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);
    }
}
