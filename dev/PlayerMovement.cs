using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour, ICustomTeleport
{
    /* REFERENCES */
    private PlayerAnimationHandler _playerAnimationHandler;
    private Dash _dash;
    public static PlayerMovement instance;
    [SerializeField] private Rigidbody _myRigidbody;
    [SerializeField] private PlayerCamera pC;
    [SerializeField] private PlayerSoundManager pS;
    [SerializeField] private PlayerInteractionHandler pI;
    private ChargeShot _chargeShot;
    private PlayerReceiveInput _playerReceiveInput;

    private float TimeToSwitch = .35f;


    [HideInInspector] public float frozen;
    [HideInInspector] public int isFacingRight = 1;


    public GameObject my3DModel;


    [HideInInspector] public float lastBumpTime;


    [HideInInspector] public Vector2 inputDirection;
    [HideInInspector] public Vector2 inputDirectionTo;
    [HideInInspector] public float hSpeed;
    [HideInInspector] public float vSpeed;
    [HideInInspector] public float zDepthTo;
    [HideInInspector] public float zDepth;
    public float zDepthOffset = 1.6f;
    public LayerMask backgroundLayer;


    [SerializeField] private Collider dashCollider;
    [SerializeField] private STATS stats;
    public Vector3 windForceTo;
    public float currentlyInWind;

    private Vector3 customForce = Vector3.zero;
    private bool canFlip = true;

    [SerializeField] private SmartData.SmartEvent.EventDispatcher onPlayerDodge;
    [SerializeField] private SmartData.SmartVector3.Vector3Writer smartDodgeDir;

    private Vector3 lastLocalPosInsideLevel;

    private readonly float acceleration = .1f;

    private float _modelRotation;

    private PlayerInput _playerInput;
    private float _startingZDepth;
    private float _timeAlive;
    public float _timeBackwards;

    //DASH /////////////////////


    private Transform _transform;

    private Vector3 _windForce;
    private float _zRot;
    private float _zRotTo;
    private bool imDisabled;
    private bool canCheckZDepth = false;

    [HideInInspector] public Vector3 velocity;


    private void OnEnable()
    {
        PlayerReceiveInput playerReceiveInput = GetComponent<PlayerReceiveInput>();
        playerReceiveInput.OnMove += OnMove;
    }

    private void OnDisable()
    {
        PlayerReceiveInput playerReceiveInput = GetComponent<PlayerReceiveInput>();
        playerReceiveInput.OnMove -= OnMove;
    }

    private void OnMove(Vector2 direction)
    {
        inputDirectionTo = direction;
    }

    private void Awake()
    {
        _dash = GetComponent<Dash>();
        _playerAnimationHandler = GetComponent<PlayerAnimationHandler>();
        _playerReceiveInput = GetComponent<PlayerReceiveInput>();
        _playerInput = GetComponent<PlayerInput>();
        _chargeShot = GetComponent<ChargeShot>();
    }


    private void Start()
    {
        inputDirectionTo = Vector2.zero;
        hSpeed = 0;
        vSpeed = 0;
        instance = this;
        inputDirectionTo = Vector2.zero;
        _transform = transform;
        _startingZDepth = _transform.position.z;
        zDepth = _startingZDepth;
        zDepthTo = _startingZDepth;

        canCheckZDepth = false;
        DOVirtual.DelayedCall(0.5f, () => { canCheckZDepth = true; });
    }

    // Update is called once per frame
    private void Update()
    {
        lastBumpTime -= Time.deltaTime;

        if (stats.ST_Invincibility)
        {
            _windForce = Vector3.Lerp(_windForce, Vector3.zero, Time.deltaTime * 10f);
            windForceTo = Vector3.Lerp(windForceTo, Vector3.zero, Time.deltaTime * 10f);
        }
        else
        {
            _windForce = Vector3.Lerp(_windForce, windForceTo, Time.deltaTime * 10f);
            currentlyInWind += Time.deltaTime;

            if (currentlyInWind > 0.05f)
                windForceTo = Vector3.Lerp(windForceTo, Vector3.zero, Time.deltaTime * 10f);
            else
                windForceTo = Vector3.Lerp(windForceTo, Vector3.zero, Time.deltaTime * 4f);
        }

        frozen -= Time.deltaTime;


        //

        hSpeed += (inputDirectionTo.x - hSpeed) * acceleration * Time.deltaTime * 50f;
        vSpeed += (inputDirectionTo.y - vSpeed) * acceleration * Time.deltaTime * 50f;

        inputDirection = new Vector2(hSpeed, vSpeed);

        CheckIfPlayerShouldFlip();

        if (canCheckZDepth)
            CheckZDepth();

        UpdatePlayerRotation();
    }


    private void FixedUpdate()
    {
        Move();
    }

    public void Move()
    {
        velocity = Vector3.zero;

        if (frozen <= 0)
            if (_chargeShot.chargeShot == 0)
                velocity = new Vector3(
                    inputDirection.x * stats.ST_Speed * stats.ST_SpeedBoost + _windForce.x + customForce.x,
                    inputDirection.y * stats.ST_Speed * stats.ST_SpeedBoost + _windForce.y + customForce.y, 0);
            else
                velocity = Mathf.Lerp(_myRigidbody.velocity.x, 0, Time.deltaTime * 10f) *
                           Vector3.right +
                           Mathf.Lerp(_myRigidbody.velocity.y, 0, Time.deltaTime * 10f) *
                           Vector3.up;

        _myRigidbody.velocity = velocity;
    }

    public static Transform ReturnPlayerTransform()
    {
        return instance._transform;
    }


    public GameObject ReturnGameobject()
    {
        return gameObject;
    }

    private void CheckZDepth()
    {
        var foundWall = false;
        //cast a ray from the player to the ground
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 6f,
                backgroundLayer))
        {
            zDepthTo = hit.distance;
            foundWall = true;
            lastLocalPosInsideLevel = transform.localPosition;
        }
        else
        {
            //zDepthTo = _startingZDepth + zDepthOffset;
            foundWall = false;
        }

        zDepth = Mathf.Lerp(zDepth, zDepthTo, Time.deltaTime * 50f);
        Vector3 position = transform.position;
        position = transform.position + transform.forward * (zDepth - zDepthOffset);
        transform.position = position;


        if (foundWall)
        {
            Quaternion rotationToLook = Quaternion.LookRotation(-hit.normal, Vector3.up);
            _zRotTo = rotationToLook.eulerAngles.y;
        }
        else
        {
            _zRotTo = 0;
        }

        _zRot = Mathf.Lerp(_zRot, _zRotTo, Time.deltaTime * 30f);

        /*
        if (!foundWall && isDodging == false)
            transform.localPosition = Vector3.Lerp(transform.localPosition, lastLocalPosInsideLevel,
                Time.deltaTime * 10f);
                */
    }

    private void UpdatePlayerRotation()
    {
        float rot = _zRotTo;
        if (_dash.isDashing || _chargeShot.chargeShot != 0)
        {
            _modelRotation = Mathf.LerpAngle(_modelRotation,
                Mathf.Atan2(vSpeed, hSpeed) * Mathf.Rad2Deg -
                180 * (isFacingRight == 1 ? 0 : 1), .9f);


            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, rot, _modelRotation),
                Time.deltaTime * 7f); //15f
        } /*
        else if (_dash.isDodging)
        {
            
            _modelRotation = Mathf.LerpAngle(_modelRotation,
                Mathf.Atan2(_dash._dodgeDirection.x, _dash._dodgeDirection.y) * Mathf.Rad2Deg, .9f);

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, rot, _modelRotation),
                Time.deltaTime * 15f);
                
        }*/
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, rot, 0),
                Time.deltaTime * 4f);
        }
    }

    private void CheckIfPlayerShouldFlip()
    {
        TimeToSwitch = _chargeShot.chargeShot == 0 ? 0.35f : 0;

        if (!canFlip) return;

        if (hSpeed >= .25f && isFacingRight != 1)
            _timeBackwards += Time.deltaTime;

        if (hSpeed <= -.25f && isFacingRight == 1)
            _timeBackwards += Time.deltaTime;

        if (Math.Abs(hSpeed) < 0.2f) _timeBackwards = 0;

        if (_timeBackwards > TimeToSwitch)
            FlipPlayer(hSpeed > 0 ? 1 : -1, 0.35f);
    }

    public void CustomTeleport(Transform teleporterTransform, Transform originalTeleporterTransform)
    {
        float zDifference = transform.position.z - teleporterTransform.position.z;
        Vector3 originalTeleporterPosition = transform.position;


        transform.position = teleporterTransform.position;
        canFlip = false;
        if (Mathf.Sign(isFacingRight) == Mathf.Sign(teleporterTransform.right.x))
            customForce = teleporterTransform.right * 13f;
        else
            customForce = teleporterTransform.right * 30f;

        /*
        if(teleporterTransform.rotation.eulerAngles.z < 90 && teleporterTransform.rotation.eulerAngles.z > -90)
        */
        if (teleporterTransform.right.x > 0)
            FlipPlayer(1);
        else
            FlipPlayer(-1);


        DOTween.To(() => customForce, x => customForce = x, Vector3.zero, 0.6f).onComplete += () => { canFlip = true; };
    }


    public void FlipPlayer(int direction, float speed = 0.5f)
    {
        if (direction == 1)
        {
            if (isFacingRight == 1) return;
            my3DModel.transform.DOLocalRotate(new Vector3(0, 90, -45f), speed * 0.5f).SetEase(Ease.Linear).onComplete +=
                () => { my3DModel.transform.DOLocalRotate(new Vector3(0, 0, 0), speed * 0.5f).SetEase(Ease.Linear); };
            _timeBackwards = 0;
            isFacingRight = 1;
        }
        else
        {
            if (isFacingRight == -1) return;
            my3DModel.transform.DOLocalRotate(new Vector3(0, 90, -45f), speed * 0.5f).SetEase(Ease.Linear).onComplete +=
                () =>
                {
                    my3DModel.transform.DOLocalRotate(new Vector3(0, 180f, 0), speed * 0.5f).SetEase(Ease.Linear);
                };
            _timeBackwards = 0;
            isFacingRight = -1;
        }
    }


    public void DisablePlayer()
    {
        //hSpeed = 0;
        //vSpeed = 0;
        inputDirection = Vector2.zero;
        inputDirectionTo = Vector2.zero;
        //_myRigidbody.velocity = Vector3.zero;
        imDisabled = true;
        _playerReceiveInput.enabled = false;
    }

    public void EnablePlayer()
    {
        imDisabled = false;
        _playerReceiveInput.enabled = true;
    } /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistanceToCheckpoint);
    }
    */
}