using UnityEngine;
using System.Collections.Generic;

public class PetoWalk : MonoBehaviour
{
    public enum State { Idle, Turn, Walk, Cry, IdleWaiting }

    [Header("参照")]
    [SerializeField] private WalkZone[] walkZones = new WalkZone[0]; // 複数WalkZone対応
    [SerializeField] private Transform visualRoot; // PokoVisualRoot を設定する

    [Header("移動")]
    [SerializeField] public float moveSpeed      = 0.5f;
    [SerializeField] public float rotationSpeed  = 4f;
    [SerializeField] public float arrivalDistance = 0.2f;
    [SerializeField] public float decelDistance   = 0.4f;

    [Header("待機")]
    [SerializeField] public float idleTimeMin = 2f;
    [SerializeField] public float idleTimeMax = 5f;

    [Header("待機モーション")]
    [SerializeField] public float idleWaitSeconds = 5f;
    [SerializeField] private string walkingParamName = "IsWalking";

    [Header("向き転換")]
    [SerializeField] public float turnAlignAngle = 15f;
    [SerializeField] private float turnSpeed = 420f;
    [SerializeField] private float turnAngleThreshold = 10f;

    [Header("メッシュ向き補正")]
    [SerializeField] private float meshFacingYOffset = -90f;

    [Header("歩き出し遅延")]
    [SerializeField] private float walkStartDelay = 0.15f;

    public State CurrentState { get; private set; } = State.IdleWaiting;

    private const int   MaxTargetRetries    = 10;
    private const float ObstacleCheckRadius = 0.3f;

    // 障害物となる家具スロット（FurnitureSlot_Rug は床ラグのため除外＝上を歩いてOK）
    private static readonly string[] ObstacleSlotNames =
    {
        "FurnitureSlot_Bed", "FurnitureSlot_Sofa", "FurnitureSlot_Table",
        "FurnitureSlot_Shelf"
    };

    private float    _fixedY;
    private Vector3  _targetPoint;
    private float    _idleTimer;
    private float    _walkStartTimer;
    private Animator _animator;
    private readonly List<Bounds> _obstacleBounds = new List<Bounds>();

    // ─── Unity ────────────────────────────────────────────
    private void Start()
    {
        _fixedY = transform.position.y;
        CollectObstacleColliders();
        _animator = GetComponentInChildren<Animator>();
        EnterIdleWaiting();
    }

    private void Update()
    {
        if (!HasActiveZone()) return;

        switch (CurrentState)
        {
            case State.IdleWaiting: UpdateIdleWaiting(); break;
            case State.Idle:        UpdateIdle();        break;
            case State.Turn:        UpdateTurn();        break;
            case State.Walk:        UpdateWalk();        break;
        }
    }

    private bool HasActiveZone()
    {
        if (walkZones == null) return false;
        foreach (var z in walkZones)
            if (z != null && z.gameObject.activeInHierarchy) return true;
        return false;
    }

    // ─── IdleWaiting ──────────────────────────────────────
    private void EnterIdleWaiting()
    {
        CurrentState  = State.IdleWaiting;
        _idleTimer    = idleWaitSeconds;
        SetAnimatorIsWalking(false);
        SetAnimatorSpeed(1f);
    }

    private void UpdateIdleWaiting()
    {
        _idleTimer -= Time.deltaTime;
        if (_idleTimer > 0f) return;

        _targetPoint = PickNextTarget();
        EnterTurn();
    }

    // ─── Idle ─────────────────────────────────────────────
    private void EnterIdle()
    {
        CurrentState  = State.Idle;
        _idleTimer    = Random.Range(idleTimeMin, idleTimeMax);
        SetAnimatorIsWalking(false);
        SetAnimatorSpeed(1f);
    }

    private void UpdateIdle()
    {
        _idleTimer -= Time.deltaTime;
        if (_idleTimer > 0f) return;

        _targetPoint = PickNextTarget();
        EnterTurn();
    }

    // ─── Turn ─────────────────────────────────────────────
    private void EnterTurn()
    {
        CurrentState = State.Turn;
        SetAnimatorIsWalking(false); // Turn中はIdleアニメーション維持（位置スライド防止）
        SetAnimatorSpeed(1f);
    }

    private void UpdateTurn()
    {
        Vector3 moveDir = _targetPoint - transform.position;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.001f) { EnterIdleWaiting(); return; }

        float targetAngle  = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg + meshFacingYOffset;
        float currentAngle = visualRoot != null ? visualRoot.localEulerAngles.y : transform.eulerAngles.y;

        // 位置を動かさずその場で向きを変える
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.Euler(0f, newAngle, 0f);

        // 回転後の角度差で判定
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(newAngle, targetAngle));
        if (angleDiff < turnAngleThreshold)
            EnterWalk();
    }

    // ─── Walk ─────────────────────────────────────────────
    private void EnterWalk()
    {
        CurrentState     = State.Walk;
        _walkStartTimer  = walkStartDelay;
        SetAnimatorIsWalking(true);
        SetAnimatorSpeed(1f);
    }

    private void UpdateWalk()
    {
        if (_walkStartTimer > 0f)
        {
            _walkStartTimer -= Time.deltaTime;
            return;
        }

        Vector3 pos    = transform.position;
        float   distXZ = new Vector2(pos.x - _targetPoint.x, pos.z - _targetPoint.z).magnitude;

        if (distXZ <= arrivalDistance)
        {
            // 到着：減速もアニメ速度ダウンもせず即Idleへ（speedは1fのまま、IsWalking=false）
            EnterIdleWaiting();
            return;
        }

        // 一定速度で移動（到着前の減速・スローモーション表現はしない）
        Vector3 moveDir = _targetPoint - pos;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 newPos = pos + moveDir.normalized * moveSpeed * Time.deltaTime;
            newPos.y = _fixedY;
            transform.position = newPos;

            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg + meshFacingYOffset;
            ApplyVisualRotation(targetAngle);
        }
        // Animator speed は EnterWalk で 1f に設定済み。歩行中は変更しない。
    }

    // ─── ヘルパー ──────────────────────────────────────────
    private void ApplyVisualRotation(float targetAngle)
    {
        if (visualRoot == null) return;
        float currentAngle = visualRoot.localEulerAngles.y;
        float newAngle     = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        visualRoot.localRotation = Quaternion.Euler(0f, newAngle, 0f);
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (_animator != null) _animator.speed = speed;
    }

    private void SetAnimatorIsWalking(bool isWalking)
    {
        if (_animator != null && !string.IsNullOrEmpty(walkingParamName))
            _animator.SetBool(walkingParamName, isWalking);
    }

    private Vector3 PickNextTarget()
    {
        // アクティブな WalkZone をリストアップ
        var active = new List<WalkZone>();
        if (walkZones != null)
            foreach (var z in walkZones)
                if (z != null && z.gameObject.activeInHierarchy) active.Add(z);

        if (active.Count == 0) return transform.position;

        // ランダムにゾーン選択 → 障害物を避けたランダム座標を生成
        WalkZone zone = active[Random.Range(0, active.Count)];
        for (int i = 0; i < MaxTargetRetries; i++)
        {
            Vector3 c = zone.GetRandomPoint();
            c.y = _fixedY;
            if (!IsOverlappingObstacle(c)) return c;
        }
        Vector3 fallback = zone.GetRandomPoint();
        fallback.y = _fixedY;
        return fallback;
    }

    private bool IsOverlappingObstacle(Vector3 point)
    {
        foreach (var b in _obstacleBounds)
        {
            if (Mathf.Abs(point.x - b.center.x) < b.extents.x + ObstacleCheckRadius &&
                Mathf.Abs(point.z - b.center.z) < b.extents.z + ObstacleCheckRadius)
                return true;
        }
        return false;
    }

    // 家具の占有範囲を収集。Collider があればそれを、無ければ Renderer.bounds で代替する。
    private void CollectObstacleColliders()
    {
        _obstacleBounds.Clear();
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var slotName in ObstacleSlotNames)
        {
            foreach (var g in allGOs)
            {
                if (!g.scene.IsValid() || g.name != slotName) continue;

                var cols = g.GetComponentsInChildren<Collider>(true);
                if (cols.Length > 0)
                {
                    foreach (var col in cols) _obstacleBounds.Add(col.bounds);
                }
                else
                {
                    // Collider 未設定の家具は Renderer の AABB を障害物として使う
                    foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                        _obstacleBounds.Add(r.bounds);
                }
                break;
            }
        }
    }
}
