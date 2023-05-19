using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Windows.WebCam;
using Random = UnityEngine.Random;


public class Dash : MonoBehaviour
{
    /* REFERENCES */
    private PlayerReceiveInput _playerReceiveInput;
    private PlayerMovement _playerMovement;
    private PlayerSoundManager _playerSoundManager;
    private PlayerAnimationHandler _playerAnimationHandler;
    private PlayerCamera _playerCamera;
    private PlayerInteractionHandler _playerInteractionHandler;
    private ChargeShot _chargeShot;
    [SerializeField] private SphereCollider dashCollider;
    [SerializeField] private SphereCollider normalCollider;

    private STATS stats;
    /* --------------------- */

    [Header("Dash Info")] [Space(10)] [SerializeField]
    private float dashDuration;

    [SerializeField] private float doubleDashDuration;
    [SerializeField] private float doubleDashTimeThreshold;
    [SerializeField] private float dashSpeedBoostMultiplier;
    [SerializeField] private float doubleDashSpeedBoostMultiplier;
    [SerializeField] private float delayToDashAgain;
    [SerializeField] private float delayToShowDash;

    private WaitForSeconds _WaitDashDuration;
    private WaitForSeconds _WaitDoubleDashDuration;
    private WaitForSeconds _WaitToAllowDoubleDash;
    private WaitForSeconds _WaitToDisableDoubleDash;
    private WaitForSeconds _WaitToRemovePenalty = new(0.3f);
    private WaitForSeconds _WaitToAllowDashAgain;
    private WaitForSeconds _WaitToShowDash;
    private WaitForSeconds _AddedExtraTime = new(0.1f);

    [HideInInspector] public bool isDashing;
    [SerializeField] private Color dodgeColor;
    [HideInInspector] public bool isDodging;
    private readonly WaitForSeconds _dodgeDuration = new(0.3f);

    [SerializeField] private List<Transform> interactables = new();
    private bool _canDodge = true;
    [HideInInspector] public Vector3 _dodgeDirection;
    public bool _doubleDash;
    private bool canDoubleDash = false;
    private bool canDashAgain = true;
    private bool onPenalty = false;
    private Transform my3dModel;

    [SerializeField] private SmartData.SmartEvent.EventDispatcher onPlayerDash;


    private void OnEnable()
    {
        _playerReceiveInput = GetComponent<PlayerReceiveInput>();
        _playerReceiveInput.OnDash += OnActivateInput;
    }

    private void OnDisable()
    {
        _playerReceiveInput.OnDash -= OnActivateInput;
    }

    private void OnActivateInput()
    {
        DoDash();
    }

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerSoundManager = GetComponent<PlayerSoundManager>();
        _playerAnimationHandler = GetComponent<PlayerAnimationHandler>();
        _playerCamera = GetComponent<PlayerCamera>();
        _playerInteractionHandler = GetComponent<PlayerInteractionHandler>();
        _chargeShot = GetComponent<ChargeShot>();
        stats = GetComponent<STATS>();
        my3dModel = stats.myRenderer.transform.parent.transform;

        _WaitDoubleDashDuration = new WaitForSeconds(doubleDashDuration);
        _WaitToAllowDoubleDash = new WaitForSeconds(dashDuration + 0.05f);
        _WaitToDisableDoubleDash = new WaitForSeconds(doubleDashTimeThreshold);
        _WaitToAllowDashAgain = new WaitForSeconds(delayToDashAgain);
        _WaitToShowDash = new WaitForSeconds(delayToShowDash);
        _WaitDashDuration = new WaitForSeconds(dashDuration - delayToShowDash);
    }

    private void DoDash()
    {
        if (_chargeShot.chargeShot == 2)
            _chargeShot.chargeShot = 0;

        if (onPenalty || _chargeShot.chargeShot == 1) return;

        if (canDashAgain && !isDashing && !canDoubleDash)
        {
            // NORMAL DASH
            StopCoroutine(DashCoroutine());
            StartCoroutine(DashCoroutine());
        }

        else if (!_doubleDash && canDoubleDash && _playerReceiveInput.inputDirectionTo.magnitude > 0.8f)
        {
            // DOUBLE DASH
            StopCoroutine(DoubleDashCoroutine());
            StartCoroutine(DoubleDashCoroutine());
        }

        else if (isDashing && !canDoubleDash && !_doubleDash) // WRONG DASH
        {
            StopCoroutine(WrongDashPenaltyCoroutine());
            StartCoroutine(WrongDashPenaltyCoroutine());
        }
    }

    public void DoDodge()
    {
        if (_playerReceiveInput.inputDirectionTo == Vector2.zero && isDodging == false && _canDodge)
        {
            _playerAnimationHandler.SetIsDodging(true);
            StartCoroutine(DodgeCoroutine());
        }
    }

    private Vector2 CalculateDashDirection()
    {
        if (_playerMovement.inputDirectionTo.magnitude <= 0.25f)
        {
            var objects = new Collider[10];
            int numOfObjects = Physics.OverlapSphereNonAlloc(transform.position, 2f, objects);

            interactables.Clear();

            for (var i = 0; i < numOfObjects; i++)
            {
                IInteractableWithDash interactable = objects[i].GetComponent<IInteractableWithDash>();

                if (interactable != null) interactables.Add(objects[i].transform);
            }

            float closestDistance = 1000;
            Vector3 closestPosition = Vector3.zero;

            foreach (Transform interactable in interactables)
            {
                float distance = Vector2.Distance(transform.position, interactable.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPosition = interactable.position;
                }
            }

            if (closestDistance == 1000)
            {
                return new Vector2(_playerMovement.isFacingRight, 0) * 1.35f;
            }

            else
            {
                Vector3 diff = closestPosition - transform.position;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                //fit movementDirection to from -1 to 1 depending on angle
                Vector2 movementDirection = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));


                return movementDirection;
            }
        }
        else
        {
            //map inputDirectionTo to -1 to 1


            _playerMovement.inputDirectionTo = _playerMovement.inputDirectionTo.normalized;

            return _playerMovement.inputDirectionTo;
        }
    }

    private IEnumerator WrongDashPenaltyCoroutine()
    {
        onPenalty = true;
        yield return _WaitToRemovePenalty;
        onPenalty = false;
    }

    private IEnumerator DodgeCoroutine()
    {
        _canDodge = false;

        //_playerSoundManager.PlayDodgeSound();
        _dodgeDirection = Random.insideUnitCircle.normalized;
        Vector3 finalDodgePos;


        if (stats.IsInsideElevator)
            finalDodgePos = _dodgeDirection * 1.3f;
        else
            finalDodgePos = transform.position + _dodgeDirection * 1.3f;


        Vector3 originalPos;

        if (stats.IsInsideElevator)
            originalPos = Vector3.zero;
        else
            originalPos = transform.position;


        var hasRotated = false;

        if (_dodgeDirection.x < 0 && _playerMovement.isFacingRight == 1)
        {
            stats.myRenderer.transform.DOLocalRotate(new Vector3(0, -180, 0), 0.3f);
            hasRotated = true;
        }
        else if (_dodgeDirection.x > 0 && _playerMovement.isFacingRight == -1)
        {
            stats.myRenderer.transform.DOLocalRotate(new Vector3(0, 0, 0), 0.3f);
            hasRotated = true;
        }


        transform.DOLocalMove(finalDodgePos, 0.2f);

        isDodging = true;
        //pI.GlowPlayer(dodgeColor);
        //pI.MakeInvincible(true);
        yield return _dodgeDuration;
        _playerAnimationHandler.SetIsDodging(false);
        isDodging = false;

        if (hasRotated)
        {
            if (_dodgeDirection.x < 0)
                stats.myRenderer.transform.DOLocalRotate(new Vector3(0, 0, 0), 0.3f);
            else if (_dodgeDirection.x > 0 && _playerMovement.isFacingRight == -1)
                stats.myRenderer.transform.DOLocalRotate(new Vector3(0, 180, 0), 0.3f);
        }


        transform.DOLocalMove(originalPos, 0.2f).onComplete += () =>
        {
            //pI.MakeInvincible(false);
            _canDodge = true;
        };
    }

    private void CorrectFacingDirection()
    {
        if (_playerMovement.inputDirectionTo.x > 0 && _playerMovement.isFacingRight != 1)
            _playerMovement.FlipPlayer(1);
        else if (_playerMovement.inputDirectionTo.x < 0 && _playerMovement.isFacingRight != -1)
            _playerMovement.FlipPlayer(-1);
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        canDoubleDash = false;

        dashCollider.enabled = true;

        StartCoroutine(AllowDoubleDashCoroutine());

        _playerAnimationHandler.SetIsDashing(true);

        /*
        if (Mathf.Sign(_playerMovement.inputDirectionTo.x) == _playerMovement.isFacingRight)
        {
            var punchRotation = transform.right * -1f;
            var rotation = Quaternion.Euler(0, 0, transform.rotation.z - 180f);
            punchRotation = rotation * punchRotation;

            my3dModel.localPosition = Vector3.zero;
            my3dModel.DOPunchPosition(punchRotation * -0.5f, 0.7f, 1, 0.1f);
        }
        */

        my3dModel.localPosition = Vector3.zero;
        my3dModel.DOLocalMoveX(my3dModel.localPosition.x - 0.8f, 0.15f)
            .SetEase(Ease.InOutBounce).SetLoops(1, LoopType.Yoyo);


        stats.ChangeSpeedBoost(dashSpeedBoostMultiplier * 0.5f);
        yield return _WaitToShowDash;


        if (_playerInteractionHandler.HasThrowableItem())
            stats.ChangeSpeedBoost(0.1f);
        else
            stats.ChangeSpeedBoost(dashSpeedBoostMultiplier);

        onPlayerDash?.Dispatch();
        _playerMovement.inputDirection = CalculateDashDirection();
        _playerMovement.hSpeed = _playerMovement.inputDirection.x;
        _playerMovement.vSpeed = _playerMovement.inputDirection.y;
        isDashing = true;
        CorrectFacingDirection();
        SoundMaster.PlaySound(transform.position, (int)SoundListAuto.FlyDodge, false);


        yield return _WaitDashDuration;

        stats.ChangeSpeedBoost(1, 0.15f);

        _playerAnimationHandler.SetIsDashing(false);

        StartCoroutine(AllowToDashAgainCoroutine());

        yield return _AddedExtraTime;

        if (!_doubleDash)
        {
            isDashing = false;
            dashCollider.enabled = false;
        }
    }

    private void Update()
    {
        my3dModel.localPosition = Vector3.Lerp(my3dModel.localPosition, Vector3.zero, 0.02f);
    }

    private IEnumerator DoubleDashCoroutine()
    {
        dashCollider.enabled = true;
        onPlayerDash?.Dispatch();
        SoundMaster.PlaySound(transform.position, (int)SoundListAuto.FlyDodge, false);
        _playerAnimationHandler.SetIsDoubleDashing(true);
        isDashing = true;

        _doubleDash = true;

        DOTween.To(() => _playerCamera.closeUpOffset, x => _playerCamera.closeUpOffset = x, 0.35f, 0.1f)
            .SetEase(Ease.OutQuad);

        stats.ChangeSpeedBoost(doubleDashSpeedBoostMultiplier);

        /*
        var punchRotation = transform.right * _playerMovement.isFacingRight;
        var rotation = Quaternion.Euler(0, 0, transform.rotation.z + 90);
        punchRotation = rotation * punchRotation;
        */

        my3dModel.localPosition = Vector3.zero;
        /*
        my3dModel.DOPunchPosition(punchRotation * 3f * _playerMovement.isFacingRight, 0.6f, 1, 0.1f);
        */
        //doMoveX and have it go back to its original position after 
        my3dModel.DOLocalMoveX(my3dModel.localPosition.x + 0.8f, 0.35f)
            .SetEase(Ease.InOutBounce).SetLoops(1, LoopType.Yoyo);

        _playerMovement.inputDirection = _playerMovement.inputDirectionTo;
        _playerMovement.hSpeed = _playerMovement.inputDirection.x;
        _playerMovement.vSpeed = _playerMovement.inputDirection.y;
        CorrectFacingDirection();
        yield return _WaitDoubleDashDuration;

        stats.ChangeSpeedBoost(1f, 0.15f);

        _playerAnimationHandler.SetIsDoubleDashing(false);

        dashCollider.enabled = false;

        yield return _AddedExtraTime;

        isDashing = false;
        _doubleDash = false;
    }

    private IEnumerator AllowDoubleDashCoroutine()
    {
        yield return _WaitToAllowDoubleDash;
        canDoubleDash = true;
        _playerCamera.SpawnDashEffect();
        yield return _WaitToDisableDoubleDash;
        canDoubleDash = false;
    }

    private IEnumerator AllowToDashAgainCoroutine()
    {
        canDashAgain = false;
        yield return _WaitToAllowDashAgain;
        canDashAgain = true;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position - transform.right);
    }
}