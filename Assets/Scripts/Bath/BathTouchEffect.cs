using UnityEngine;

public class BathTouchEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem touchParticle;

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        var cam = Camera.main;
        if (cam == null) return Vector3.zero;

        // カメラ Z=-10、キャラ前面 Z≈-5.74（カメラ距離 4.26）
        // dist=3 → Z=-7 でキャラ前面より確実に手前
        const float dist = 3f;
        return cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, dist));
    }

    // 一発再生（OnPointerDown 時など）
    public void Play(Vector2 screenPosition)
    {
        if (touchParticle == null) return;
        touchParticle.transform.position = ScreenToWorld(screenPosition);
        touchParticle.Play();
    }

    // 毎フレーム位置を更新（ドラッグ追従用）
    public void UpdatePosition(Vector2 screenPosition)
    {
        if (touchParticle == null) return;
        var worldPos = ScreenToWorld(screenPosition);
        touchParticle.transform.position = worldPos;
        Debug.Log($"[TouchEffect] UpdatePosition: screen={screenPosition} world={worldPos} isPlaying={touchParticle.isPlaying} emissionEnabled={touchParticle.emission.enabled}");
    }

    // 連続放出：emission を有効化し、停止中なら Play() する
    public void StartContinuous(Vector2 screenPosition)
    {
        if (touchParticle == null)
        {
            Debug.LogWarning("[TouchEffect] StartContinuous: touchParticle is null");
            return;
        }
        var rend = touchParticle.GetComponent<ParticleSystemRenderer>();
        if (rend != null && rend.sharedMaterial == null)
            Debug.LogWarning("[TouchEffect] Particle Renderer に Material が設定されていません");

        // 先に位置を確定してから再生する（原点に出ないよう）
        UpdatePosition(screenPosition);

        var emission = touchParticle.emission;
        emission.enabled = true;

        if (!touchParticle.isPlaying)
        {
            var main = touchParticle.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            touchParticle.Play();
        }

        Debug.Log($"[TouchEffect] StartContinuous: emissionEnabled={touchParticle.emission.enabled} rateOverTime={touchParticle.emission.rateOverTimeMultiplier} isPlaying={touchParticle.isPlaying} worldPos={touchParticle.transform.position}");
    }

    // 新規放出だけ止める（Stop() を呼ばず StopAction を起動させない）
    public void StopContinuous()
    {
        if (touchParticle == null) return;
        var emission = touchParticle.emission;
        emission.enabled = false;
        Debug.Log($"[TouchEffect] StopContinuous: emissionEnabled={touchParticle.emission.enabled} isPlaying={touchParticle.isPlaying}");
    }
}
