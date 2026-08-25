using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Care画面専用の Poko アニメーション制御。
/// Eat アニメーション再生・表情切り替え・皿ON/OFF・ボタン無効化を管理する。
/// Main の PetoWalk には触れない。
/// </summary>
public class CarePokoController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Animator     pokoAnimator;
    [SerializeField] private FaceController faceController;

    [Header("皿・おやつ仮オブジェクト（後で差し替え可）")]
    [SerializeField] private GameObject dishRoot;
    [SerializeField] private GameObject snackRoot;

    [Header("Eat中に無効化する Care ボタングループ")]
    [SerializeField] private GameObject careButtonGroup;

    // ── Animator パラメータ ─────────────────────────────────────────────────
    private static readonly int IsEatingHash  = Animator.StringToHash("IsEating");
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    // ── Poko_Eat.anim タイムライン (3.375s / 24fps / 81F) ──────────────────
    // 0s    : Eat開始 → 表情 Fun
    // 0.750s: 18F → 皿ON / おやつON
    // 1.750s: 42F → 表情 Surprised (仮)
    // 2.625s: 63F → 表情 Happy
    // 3.100s: 74F → 皿OFF (終盤)
    // 3.375s: 81F → IsEating=false / ResetToAuto
    private const float kDishOn    = 0.750f;
    private const float kSurprised = 1.750f;
    private const float kHappy     = 2.625f;
    private const float kDishOff   = 3.100f;
    private const float kEatEnd    = 3.375f;

    private Coroutine _eatCoroutine;

    // ── Unity ライフサイクル ────────────────────────────────────────────────

    private void Start()
    {
        // Care 画面では歩行しない
        if (pokoAnimator != null)
        {
            pokoAnimator.SetBool(IsWalkingHash, false);
            pokoAnimator.SetBool(IsEatingHash,  false);
        }
        if (dishRoot  != null) dishRoot.SetActive(false);
        if (snackRoot != null) snackRoot.SetActive(false);

        // Care 通常状態は Normal 表情 + Blink
        faceController?.SetExpression("Normal");
    }

    // ── 公開 API ────────────────────────────────────────────────────────────

    /// <summary>
    /// OyatuManager の「あげる」ボタン押下時に呼ぶ。
    /// 二重発動は自動で防止する。
    /// </summary>
    public void PlayEat()
    {
        // ぽこ以外を選んでいると、このオブジェクト（ぽこの Prefab）は非アクティブ。
        // 非アクティブなオブジェクトではコルーチンを起動できずエラーになるので、ここで抜ける。
        // ぽこ以外は CareCharacterActionController が担当する。
        if (!gameObject.activeInHierarchy) return;

        Debug.Log("[CarePokoController] PlayEat called. coroutine already running: " + (_eatCoroutine != null));
        if (_eatCoroutine != null) return;
        _eatCoroutine = StartCoroutine(EatSequence());
    }

    // ── コルーチン ──────────────────────────────────────────────────────────

    private IEnumerator EatSequence()
    {
        SetButtonsInteractable(false);

        // 0F: Fun 表情 + Eat 開始
        faceController?.SetExpression("Fun");
        if (pokoAnimator != null)
        {
            pokoAnimator.SetBool(IsEatingHash, true);
            Debug.Log("[CarePokoController] IsEating = true. Current state: " + pokoAnimator.GetCurrentAnimatorStateInfo(0).IsName("Poko_Eat"));
        }

        // 18F: 皿・おやつ ON
        yield return new WaitForSeconds(kDishOn);
        if (dishRoot  != null) dishRoot.SetActive(true);
        if (snackRoot != null) snackRoot.SetActive(true);

        // 42F: Surprised (一口ずつ消す処理は後回し)
        yield return new WaitForSeconds(kSurprised - kDishOn);
        faceController?.SetExpression("Surprised");

        // 63F: Happy
        yield return new WaitForSeconds(kHappy - kSurprised);
        faceController?.SetExpression("Happy");

        // 74F: 皿 OFF (終盤)
        yield return new WaitForSeconds(kDishOff - kHappy);
        if (dishRoot  != null) dishRoot.SetActive(false);
        if (snackRoot != null) snackRoot.SetActive(false);

        // 81F: アニメ終了
        yield return new WaitForSeconds(kEatEnd - kDishOff);

        if (pokoAnimator != null)
        {
            pokoAnimator.SetBool(IsEatingHash, false);
            Debug.Log("[CarePokoController] IsEating = false. Returning to Idle.");
        }

        // Eat 終了後は Normal + Blink に戻す
        faceController?.SetExpression("Normal");
        SetButtonsInteractable(true);
        _eatCoroutine = null;
    }

    // ── プライベートヘルパー ────────────────────────────────────────────────

    private void SetButtonsInteractable(bool interactable)
    {
        if (careButtonGroup == null) return;
        foreach (var btn in careButtonGroup.GetComponentsInChildren<Button>(true))
            btn.interactable = interactable;
    }
}
