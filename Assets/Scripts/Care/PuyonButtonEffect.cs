using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // クリック検知に必要だお
using DG.Tweening;

public class PuyonButtonEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("アニメーション設定")]
    [SerializeField] private float pushScale = 0.93f; // 押した時のサイズ
    [SerializeField] private float duration = 0.1f;    // 変化する速さ

    [Header("パーティクル（任意）")]
    [SerializeField] private ParticleSystem clickEffect;

    private Vector3 defaultScale;

    void Start()
    {
        defaultScale = transform.localScale;
    }

    // ボタンが押された瞬間！
    public void OnPointerDown(PointerEventData eventData)
    {
        // ギュッっと縮むお
        transform.DOScale(defaultScale * pushScale, duration).SetEase(Ease.OutQuad);

        // パーティクルがあれば再生！
        if (clickEffect != null) clickEffect.Play();
    }

    // 指を離した瞬間！
    public void OnPointerUp(PointerEventData eventData)
    {
        // ポヨンッと元のサイズより少し大きく戻ってから落ち着く（弾力演出）
        transform.DOScale(defaultScale, duration * 2f).SetEase(Ease.OutBack);
    }
}