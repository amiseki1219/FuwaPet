using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Care 画面での、ぽこ以外のキャラのアクション制御。
///
/// 【なぜ CarePokoController と分けたか】
///   あちらは「ぽこの Prefab に付いていて、Poko_Eat.anim の 3.375秒に合わせた
///   固定の秒数」で動いている。ぽこが非表示のときはコルーチンすら起動できない。
///   キャラごとに Eat の長さも違う（える95F / ここ72F / ぱる83F / ぴよこ79F）ので、
///   同じ秒数を使い回すこともできない。
///
/// 【どこに置くか】
///   CharacterDisplaySystem など、キャラが非表示でも生きているオブジェクトに1つ。
///   キャラ本体に付けると、ぽこと同じ「非アクティブで動かない」問題が起きる。
///
/// 【タイミングの決め方】
///   秒数ではなく「クリップ全体に対する割合」で指定する。
///   実行時に Animator からクリップの長さを読んで、割合を秒に直している。
///   これでキャラごとの長さの違いを気にしなくてよくなる。
///
/// 【ぽこのとき】
///   characterDisplayAnchor の下にキャラが居ないので、Animator が見つからず何もしない。
///   ぽこは今までどおり CarePokoController が担当する。
/// </summary>
public class CareCharacterActionController : MonoBehaviour
{
    [Header("結線")]
    [Tooltip("キャラが生成される場所。ここの子から Animator と FaceController を探す")]
    [SerializeField] private Transform characterDisplayAnchor;

    [Tooltip("アクション中に押せなくするボタンのまとまり。未結線でもよい")]
    [SerializeField] private GameObject careButtonGroup;

    [Header("皿・おやつ（今後追加する。いまは未結線でよい）")]
    [SerializeField] private GameObject dishRoot;
    [SerializeField] private GameObject snackRoot;

    [Header("Animator のパラメータ名")]
    [Tooltip("おやつのときに送る Trigger 名。クリップ名を探すキーワードにも使う")]
    [SerializeField] private string eatTrigger = "Eat";

    [Tooltip("お風呂あがりに送る Trigger 名")]
    [SerializeField] private string happyTrigger = "Happy";

    // Animator 側で Eat → Happy と繋いでいる場合、Eat の長さだけ待つと
    // Happy の途中で表情が自動判定へ戻ってしまう。その分も待たせるためのスイッチ。
    [Tooltip("Animator で Eat のあとに Happy が続く場合は ON。\n" +
             "表情を元に戻すまでの時間に、Happy のぶんも足す")]
    [SerializeField] private bool playHappyAfterEat = true;

    // ── おやつ中の表情（割合で切り替え） ──────────────────────────────────────
    //
    // 初期値はぽこの Poko_Eat（3.375秒）での秒数から出した目安。
    //   0.750s → 22% / 1.750s → 52% / 2.625s → 78% / 3.100s → 92%
    [Header("おやつ中の表情（クリップ全体に対する割合）")]
    [SerializeField] private string eatStartExpression = "Fun";

    [Range(0f, 1f)] [Tooltip("皿とおやつを出す割合")]
    [SerializeField] private float dishOnRatio = 0.22f;

    [SerializeField] private string midExpression = "Surprised";
    [Range(0f, 1f)] [SerializeField] private float midExpressionRatio = 0.52f;

    [SerializeField] private string endExpression = "Happy";
    [Range(0f, 1f)] [SerializeField] private float endExpressionRatio = 0.78f;

    [Range(0f, 1f)] [Tooltip("皿とおやつを片付ける割合")]
    [SerializeField] private float dishOffRatio = 0.92f;

    // ── お風呂あがり ──────────────────────────────────────────────────────────
    [Header("お風呂あがり")]
    // キャラによって表情の持ち方が2通りある（下の SetFaceExpression のコメント参照）。
    // Relaxed は5体すべてに登録があるので、ここに使える。
    [Tooltip("お風呂のあとに固定する表情。5体すべてにあるキーにすること")]
    [SerializeField] private string bathExpression = "Relaxed";

    [Tooltip("その表情を保つ秒数。300 = 5分。過ぎたら状態に応じた自動判定へ戻る")]
    [SerializeField] private float bathExpressionDuration = 300f;

    [Header("保険")]
    [Tooltip("クリップの長さが読めなかったときに使う秒数")]
    [SerializeField] private float fallbackEatLength = 3.4f;

    private Coroutine _eatCoroutine;
    private Coroutine _bathCoroutine;

    // ── 公開 API ──────────────────────────────────────────────────────────────

    /// <summary>おやつをあげたときに呼ぶ。ぽこが表示されているときは何もしない。</summary>
    public void PlayEat()
    {
        if (_eatCoroutine != null) return;   // 二重発動よけ

        Animator animator = FindAnimator();
        if (animator == null) return;        // ぽこのときはここで抜ける

        _eatCoroutine = StartCoroutine(EatSequence(animator));
    }

    /// <summary>
    /// お風呂から戻ったときに呼ぶ。
    /// 嬉しいアニメを鳴らして、そのあと bathExpressionDuration の間だけ表情を固定する。
    /// </summary>
    public void PlayHappy()
    {
        Animator animator = FindAnimator();
        if (animator == null) return;

        if (!string.IsNullOrEmpty(happyTrigger)) animator.SetTrigger(happyTrigger);

        if (_bathCoroutine != null) StopCoroutine(_bathCoroutine);
        _bathCoroutine = StartCoroutine(BathExpressionSequence());
    }

    // ── コルーチン ────────────────────────────────────────────────────────────

    private IEnumerator EatSequence(Animator animator)
    {
        SetButtonsInteractable(false);

        // 表情の割合を計算する基準は Eat クリップの長さ
        float eatLength = FindClipLength(animator, eatTrigger);
        if (eatLength <= 0f)
        {
            Debug.LogWarning($"[CareAction] '{eatTrigger}' を含むクリップが見つかりません。{fallbackEatLength}秒として扱います");
            eatLength = fallbackEatLength;
        }

        // 表情を元に戻すのは、Eat（＋続く Happy）が終わってから
        float totalLength = eatLength;
        if (playHappyAfterEat) totalLength += FindClipLength(animator, happyTrigger);

        if (!string.IsNullOrEmpty(eatTrigger)) animator.SetTrigger(eatTrigger);
        SetFaceExpression(eatStartExpression);

        // 割合の小さい順に処理する。
        // Inspector で順番が前後しても、並べ替えてから進むので破綻しない
        var steps = new List<KeyValuePair<float, Action>>
        {
            new(dishOnRatio,          () => SetDish(true)),
            new(midExpressionRatio,   () => SetFaceExpression(midExpression)),
            new(endExpressionRatio,   () => SetFaceExpression(endExpression)),
            new(dishOffRatio,         () => SetDish(false)),
        };
        steps.Sort((a, b) => a.Key.CompareTo(b.Key));

        float elapsed = 0f;
        foreach (var step in steps)
        {
            float target = Mathf.Clamp01(step.Key) * eatLength;
            float wait = target - elapsed;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
                elapsed = target;
            }
            step.Value();
        }

        float rest = totalLength - elapsed;
        if (rest > 0f) yield return new WaitForSeconds(rest);

        // 食べ終わったら自動判定に戻す
        ResetFaceToAuto();
        SetButtonsInteractable(true);
        _eatCoroutine = null;
    }

    private IEnumerator BathExpressionSequence()
    {
        SetFaceExpression(bathExpression);

        if (bathExpressionDuration > 0f) yield return new WaitForSeconds(bathExpressionDuration);

        ResetFaceToAuto();
        _bathCoroutine = null;
    }

    // ── 内部ヘルパー ──────────────────────────────────────────────────────────

    /// <summary>いま表示されているキャラの Animator。ぽこのときは見つからない。</summary>
    private Animator FindAnimator()
    {
        if (characterDisplayAnchor == null)
        {
            Debug.LogWarning("[CareAction] characterDisplayAnchor が未結線です", this);
            return null;
        }
        return characterDisplayAnchor.GetComponentInChildren<Animator>(true);
    }

    /// <summary>
    /// 表情を切り替える。
    ///
    /// 【なぜ2通り見るのか】
    ///   表情の持ち方がキャラで分かれている。
    ///     ぽこ  … FaceController（Inspector に直接テクスチャ）
    ///     他4体 … CharacterFaceController（FaceExpressionDatabase を参照）
    ///   このコントローラが担当するのは後者だが、将来ぽこが混ざっても困らないよう
    ///   両方を見るようにしている。
    ///
    /// 【キーの違いに注意】
    ///   ぽこにしかない: SlightHappy
    ///   4体にしかない : Shy / Close
    ///   共通         : Normal / Fun / Happy / Sad / Angry / Surprised / Relaxed
    ///   Inspector に入れるキーは、共通のものにしておくと事故が少ない。
    /// </summary>
    private void SetFaceExpression(string key)
    {
        if (string.IsNullOrEmpty(key) || characterDisplayAnchor == null) return;

        var charFace = characterDisplayAnchor.GetComponentInChildren<CharacterFaceController>(true);
        if (charFace != null) { charFace.SetExpression(key); return; }

        var pokoFace = characterDisplayAnchor.GetComponentInChildren<FaceController>(true);
        if (pokoFace != null) { pokoFace.SetExpression(key); return; }

        Debug.LogWarning($"[CareAction] 表情を切り替えるコンポーネントが見つかりません（key={key}）", this);
    }

    /// <summary>表情の固定を解除して、状態に応じた自動判定へ戻す。</summary>
    private void ResetFaceToAuto()
    {
        if (characterDisplayAnchor == null) return;

        var charFace = characterDisplayAnchor.GetComponentInChildren<CharacterFaceController>(true);
        if (charFace != null) { charFace.ResetToAuto(); return; }

        var pokoFace = characterDisplayAnchor.GetComponentInChildren<FaceController>(true);
        pokoFace?.ResetToAuto();
    }

    /// <summary>
    /// Animator が持っているクリップから、名前に keyword を含むものの長さを返す。
    /// 見つからなければ 0。
    ///
    /// GetCurrentAnimatorStateInfo().length を使わないのは、
    /// SetTrigger の直後だとまだ前の State を指していて、正しい長さが取れないため。
    /// </summary>
    private static float FindClipLength(Animator animator, string keyword)
    {
        if (animator == null || string.IsNullOrEmpty(keyword)) return 0f;

        var controller = animator.runtimeAnimatorController;
        if (controller == null) return 0f;

        foreach (var clip in controller.animationClips)
        {
            if (clip == null) continue;
            if (clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return clip.length;
        }
        return 0f;
    }

    private void SetDish(bool on)
    {
        if (dishRoot  != null) dishRoot.SetActive(on);
        if (snackRoot != null) snackRoot.SetActive(on);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (careButtonGroup == null) return;
        foreach (var btn in careButtonGroup.GetComponentsInChildren<Button>(true))
            btn.interactable = interactable;
    }
}
