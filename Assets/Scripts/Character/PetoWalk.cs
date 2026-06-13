using UnityEngine;
using System.Collections.Generic;

public class PetoWalk : MonoBehaviour
{
    public enum State { Idle, Turn, Walk, Cry }

    [Header("参照")]
    [SerializeField] public WalkZone walkZone;
    [SerializeField] private Transform visualRoot; // PokoVisualRoot を設定する

    [Header("移動")]
    [SerializeField] public float moveSpeed      = 0.5f;
    [SerializeField] public float rotationSpeed  = 4f;
    [SerializeField] public float arrivalDistance = 0.2f;
    [SerializeField] public float decelDistance   = 0.4f;

    [Header("待機")]
    [SerializeField] public float idleTimeMin = 2f;
    [SerializeField] public float idleTimeMax = 5f;

    [Header("向き転換")]
    [SerializeField] public float turnAlignAngle = 15f;

    [Header("メッシュ向き補正")]
    [SerializeField] private float meshFacingYOffset = -90f;

    public State CurrentState { get; private set; } = State.Idle;

    private const int   MaxTargetRetries    = 10;
    private const float ObstacleCheckRadius = 0.3f;

    private static readonly string[] FurnitureSlotNames =
    {
        "FurnitureSlot_Bed", "FurnitureSlot_Sofa", "FurnitureSlot_Table",
        "FurnitureSlot_Shelf", "FurnitureSlot_Rug"
    };

    private float    _fixedY;
    private Vector3  _targetPoint;
    private float    _idleTimer;
    private float    _currentSpeed;
    private Animator _animator;
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
        CurrentState  = State.Idle;
        _currentSpeed = 0f;
        _idleTimer    = Random.Range(idleTimeMin, idleTimeMax);
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
        // targetPosition - transform.position のワールド方向から角度を計算
        Vector3 moveDir = _targetPoint - transform.position;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.001f) { EnterIdle(); return; }

        float targetAngle  = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg + meshFacingYOffset;
        float currentAngle = visualRoot != null ? visualRoot.localEulerAngles.y : transform.eulerAngles.y;
        float angleDiff    = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        ApplyVisualRotation(targetAngle);

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
        Vector3 pos    = transform.position;
        float   distXZ = new Vector2(pos.x - _targetPoint.x, pos.z - _targetPoint.z).magnitude;

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
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, moveSpeed * 4f * Time.deltaTime);

        // 移動方向をワールド座標で計算（transform.forward は使わない）
        Vector3 moveDir = _targetPoint - pos;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            // PokoWalkRoot の position のみ変更
            Vector3 newPos = pos + moveDir.normalized * _currentSpeed * Time.deltaTime;
            newPos.y = _fixedY;
            transform.position = newPos;

            // PokoVisualRoot の localRotation のみ変更
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg + meshFacingYOffset;
            ApplyVisualRotation(targetAngle);
        }

        SetAnimatorSpeed(_currentSpeed / moveSpeed);
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
