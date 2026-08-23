using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 開発用：お風呂の「1日2回まで」の制限をリセットするボタン。
///
/// 何度もお風呂に入って泡や演出を確認したいときに使う。
/// SaveData の bathCountToday と lastBathDate だけを戻す。
///
/// 製品ビルドでは自動的に消える:
///   Unity エディタ、または Development Build のときだけボタンが動く。
///   それ以外（＝ストアに出すビルド）では Awake でボタンごと非表示にするので、
///   Scene にボタンを置いたまま出荷しても画面には出ない。
///
/// 要らなくなったら、このファイルと Scene 上のボタンを消すだけでよい。
/// 他のスクリプトからは一切参照されていない。
/// </summary>
public class BathDevResetButton : MonoBehaviour
{
    [Header("結線")]
    [Tooltip("押したときにリセットするボタン。未設定なら同じ GameObject の Button を探す")]
    [SerializeField] private Button resetButton;

    [Tooltip("製品ビルドで隠す対象。未設定ならこの GameObject 自身を隠す")]
    [SerializeField] private GameObject rootToHide;

    private void Awake()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (resetButton == null) resetButton = GetComponent<Button>();

        if (resetButton == null)
        {
            Debug.LogWarning("[Bath][開発用] リセットボタンが未結線です");
            return;
        }

        resetButton.onClick.AddListener(ResetBathCount);
#else
        // 製品ビルドでは存在ごと消す
        var target = rootToHide != null ? rootToHide : gameObject;
        target.SetActive(false);
#endif
    }

    /// <summary>
    /// 今日のお風呂回数を 0 に戻す。
    /// Inspector の OnClick から直接呼んでもよい（その場合 resetButton は未結線でよい）。
    ///
    /// 触らないもの:
    ///   statusLastBathAt … 清潔度の時間経過用。回数制限とは別物
    ///   nextBath         … 現状どこからも書き込まれていない
    ///   コイン・ルナストーン … 払ったシャンプー代は戻さない
    /// </summary>
    public void ResetBathCount()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null)
        {
            Debug.LogWarning("[Bath][開発用] SaveData が取得できませんでした");
            return;
        }

        int before = save.bathCountToday;

        save.bathCountToday = 0;
        save.lastBathDate   = "";

        SaveManager.Instance?.Save();

        Debug.Log($"<color=#00E5FF>[決定]</color> [Bath][開発用] お風呂の回数をリセットしました {before}回 → 0回");
    }
}
