using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParameterName = "IsWalking";
    [SerializeField] private float facingYOffset;
    [SerializeField] private float moveSpeed = 0f; // 0以下は未設定（PetoWalk 側の値にフォールバック）
    [SerializeField] private float displayScale = 1f; // 0以下は未設定（等倍にフォールバック）

    private Animator _validatedAnimator;
    private string _validatedParameterName;
    private int _walkingParameterHash;
    private bool _hasWalkingParameter;

    public float FacingYOffset => facingYOffset;
    public float MoveSpeed => moveSpeed;
    public float DisplayScale => displayScale > 0f ? displayScale : 1f;

    public void SetWalking(bool isWalking)
    {
        ValidateWalkingParameter();
        if (_hasWalkingParameter)
            animator.SetBool(_walkingParameterHash, isWalking);
    }

    private void ValidateWalkingParameter()
    {
        if (_validatedAnimator == animator &&
            _validatedParameterName == walkingParameterName)
            return;

        _validatedAnimator = animator;
        _validatedParameterName = walkingParameterName;
        _hasWalkingParameter = false;

        if (animator == null || string.IsNullOrWhiteSpace(walkingParameterName))
            return;

        _walkingParameterHash = Animator.StringToHash(walkingParameterName);
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == _walkingParameterHash &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                _hasWalkingParameter = true;
                return;
            }
        }
    }
}
