using UnityEngine;
using System.Collections.Generic;

public class PetoWalk : MonoBehaviour
{
    public enum State { Idle, Turn, Walk, Cry }

    [Header("参照")]
    [SerializeField] public WalkZone walkZone;

    [Header("移動")]
    [SerializeField] public float moveSpeed       = 0.08f;
    [SerializeField] public float rotationSpeed   = 4f;
    [SerializeField] public float arrivalDistance  = 0.15f;
    [SerializeField] public float decelDistance    = 0.4f;   // この距離内で減速開始

    [Header("待機")]
    [SerializeField] public float idleTimeMin = 2f;
    [SerializeField] public float idleTimeMax = 5f;

    [Header("向き転換")]
    [SerializeField] public float turnAlignAngle = 15f;      // この角度以内で向き完了とみなす

    public State CurrentState { get; private set; } = State.Idle;

    private const int   MaxTargetRetries    = 10;
    private const float ObstacleCheckRadius = 0.3f;

    private static readonly string[] FurnitureSlotNames =
    {
        "FurnitureSlot_Bed", "FurnitureSlot_Sofa", "FurnitureSlot_Table",
        "FurnitureSlot_Shelf", "FurnitureSlot_Rug"
    };

    private float           _fixedY;
    private Vector3         _targetPoint;
    private float           _idleTimer;
    private float           _currentSpeed;
    private Animator        _animator;
    private readonly List<Collider> _obstacleColliders = new List<Collider>();

    // ─── Unity ────────────────────────────────────────────
    private void Start()
    {
        _fixedY = transform.position.y;
        CollectObstacleColliders();
        _animator = GetComponentInChildren<Animator>();
        EnterIdle();
    }

    private void Update()
    {
        if (walkZone == null) return;

        switch (CurrentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Turn: UpdateTurn(); break;
            case State.Walk: UpdateWalk(); break;
        }
    }

    // ─── Idle ─────────────────────────────────────────────
    private void EnterIdle()
    {
        CurrentState   = State.Idle;
        _currentSpeed  = 0f;
        _idleTimer     = Random.Range(idleTimeMin, idleTimeMax);
        SetAnimatorSpeed(0f);
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
        SetAnimatorSpeed(0f);
    }

    private void UpdateTurn()
    {
        Vector3 pos   = transform.position;
        Vector3 dirXZ = new Vector3(_targetPoint.x - pos.x, 0f, _targetPoint.z - pos.z);
        if (dirXZ.sqrMagnitude < 0.001f) { EnterIdle(); return; }
        dirXZ.Normalize();

        float targetY  = Quaternion.LookRotation(dirXZ).eulerAngles.y;
        float currentY = transform.eulerAngles.y;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentY, targetY));

        float newY = Mathf.LerpAngle(currentY, targetY, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, newY, 0f);

        if (angleDiff < turnAlignAngle)
            EnterWalk();
    }

    // ─── Walk ─────────────────────────────────────────────
    private void EnterWalk()
    {
        CurrentState  = State.Walk;
        _currentSpeed = 0f;
        SetAnimatorSpeed(1f);
    }

    private void UpdateWalk()
    {
        Vector3 pos     = transform.position;
        float   distXZ  = new Vector2(pos.x - _targetPoint.x, pos.z - _targetPoint.z).magnitude;

        // 到達 → Idle へ
        if (distXZ <= arrivalDistance)
        {
            _currentSpeed = 0f;
            SetAnimatorSpeed(0f);
            EnterIdle();
            return;
        }

        // 目標速度（減速ゾーン内は線形に落とす）
        float targetSpeed = (distXZ < decelDistance)
            ? moveSpeed * (distXZ / decelDistance)
            : moveSpeed;

        // 加速・減速（MoveTowards でなめらかに）
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, moveSpeed * 4f * Time.deltaTime);

        Vector3 dirXZ = new Vector3(_targetPoint.x - pos.x, 0f, _targetPoint.z - pos.z).normalized;

        // 移動（Y固定）
        Vector3 newPos = pos + dirXZ * _currentSpeed * Time.deltaTime;
        newPos.y = _fixedY;
        transform.position = newPos;

        // 向き（Y軸のみ、Walk中も微調整）
        if (dirXZ.sqrMagnitude > 0.001f)
        {
            float targetY  = Quaternion.LookRotation(dirXZ).eulerAngles.y;
            float currentY = transform.eulerAngles.y;
            float newY     = Mathf.LerpAngle(currentY, targetY, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, newY, 0f);
        }

        // アニメ速度を移動速度に同期（てちてち感）
        SetAnimatorSpeed(_currentSpeed / moveSpeed);
    }

    // ─── ヘルパー ──────────────────────────────────────────
    private void SetAnimatorSpeed(float speed)
    {
        if (_animator != null) _animator.speed = speed;
    }

    private Vector3 PickNextTarget()
    {
        for (int i = 0; i < MaxTargetRetries; i++)
        {
            Vector3 c = walkZone.GetRandomPoint();
            c.y = _fixedY;
            if (!IsOverlappingObstacle(c)) return c;
        }
        Vector3 fallback = walkZone.GetRandomPoint();
        fallback.y = _fixedY;
        return fallback;
    }

    private bool IsOverlappingObstacle(Vector3 point)
    {
        foreach (var col in _obstacleColliders)
        {
            if (col == null) continue;
            Bounds b = col.bounds;
            if (Mathf.Abs(point.x - b.center.x) < b.extents.x + ObstacleCheckRadius &&
                Mathf.Abs(point.z - b.center.z) < b.extents.z + ObstacleCheckRadius)
                return true;
        }
        return false;
    }

    private void CollectObstacleColliders()
    {
        _obstacleColliders.Clear();
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var slotName in FurnitureSlotNames)
        {
            foreach (var g in allGOs)
            {
                if (!g.scene.IsValid() || g.name != slotName) continue;
                foreach (var col in g.GetComponentsInChildren<Collider>(true))
                    _obstacleColliders.Add(col);
                break;
            }
        }
    }
}
