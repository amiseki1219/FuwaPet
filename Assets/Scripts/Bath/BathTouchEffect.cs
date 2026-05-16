using UnityEngine;

public class BathTouchEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem touchParticle;

    public void Play(Vector2 screenPosition)
    {
        if (touchParticle == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        // ScreenSpaceCamera Canvas の planeDistance 分だけ手前に出す
        var canvas = GetComponentInParent<Canvas>();
        float dist = canvas != null ? canvas.planeDistance - 1f
                                    : Mathf.Abs(cam.transform.position.z) - 1f;
        dist = Mathf.Max(dist, 0.1f);

        Vector3 worldPos = cam.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, dist));
        touchParticle.transform.position = worldPos;
        touchParticle.Play();
    }
}
