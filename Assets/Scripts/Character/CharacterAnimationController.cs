using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParameterName = "IsWalking";
    [SerializeField] private float facingYOffset;

    private Animator _validatedAnimator;
    private string _validatedParameterName;
    private int _walkingParameterHash;
    private bool _hasWalkingParameter;

    public float FacingYOffset => facingYOffset;

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
