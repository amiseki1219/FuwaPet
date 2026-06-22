using System.Collections;
using UnityEngine;

public class PokoEatTester : MonoBehaviour
{
    [SerializeField] private Animator pokoAnimator;
    [SerializeField] private float eatDuration = 3.3f;

    private bool _isPlaying;
    private static readonly int IsEatingHash = Animator.StringToHash("IsEating");

    private void Start()
    {
        if (pokoAnimator == null)
            pokoAnimator = GetComponentInChildren<Animator>();

        if (pokoAnimator == null)
            Debug.LogWarning("[PokoEatTester] Animator not found. Set it in Inspector.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            OnEatKey();
    }

    private void OnEatKey()
    {
        if (pokoAnimator == null || _isPlaying) return;
        StartCoroutine(EatCoroutine());
    }

    [ContextMenu("Play Poko_Eat")]
    private void ContextPlayEat()
    {
        if (pokoAnimator == null)
        {
            Debug.LogWarning("[PokoEatTester] Animator is null.");
            return;
        }
        if (_isPlaying) return;
        StartCoroutine(EatCoroutine());
    }

    [ContextMenu("Reset IsEating")]
    private void ContextResetEating()
    {
        StopAllCoroutines();
        if (pokoAnimator != null)
            pokoAnimator.SetBool(IsEatingHash, false);
        _isPlaying = false;
        Debug.Log("[PokoEatTester] IsEating reset to false.");
    }

    private IEnumerator EatCoroutine()
    {
        _isPlaying = true;
        pokoAnimator.SetBool(IsEatingHash, true);
        Debug.Log("[PokoEatTester] Poko_Eat started (IsEating = true)");

        yield return new WaitForSeconds(eatDuration);

        pokoAnimator.SetBool(IsEatingHash, false);
        _isPlaying = false;
        Debug.Log("[PokoEatTester] Poko_Eat ended (IsEating = false) -> back to Idle");
    }
}
