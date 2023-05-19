using UnityEngine;

public class BotMovement : MonoBehaviour
{
    [SerializeField] private string iaState;
    private float _shootCooldown;
    private Transform _target;
    private float _waitTime;
    private Vector3 _chargeBarScale;
    private Vector3 _forceDirection;
    private Collider _lastTouchedPlayer;
    private PlayerStats _pS;
    public float lastCollisionTime;
    private Vector3 _targetPosition;
    private Vector3 _transformPosition;
    private float _decidedCharge;

    // Start is called before the first frame update
    void Start()
    {
        _pS = GetComponent<PlayerStats>();
        lastCollisionTime = 0;
        ChangeIaState("search-target");
        _pS.UpdateChargeBar(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        switch (iaState)
        {
            case "search-target":
                SearchTarget();
                break;
            case "wait":
                BotWait();
                break;
            case "charge-attack":
                ChargeAttack();
                break;
            case "shoot-target":
                ShootTarget();
                break;
        }

        if (_target != null)
        {
            _targetPosition = _target.position;
            _transformPosition = transform.position;

            _pS.UpdateChargeBar(_targetPosition.z - _transformPosition.z,
                _targetPosition.x - _transformPosition.x, _pS.chargeDirection);
        }

        if (_shootCooldown > 0) _shootCooldown -= Time.deltaTime;
    }

    private void ChangeIaState(string newIaState)
    {
        iaState = newIaState;
        //print(newIaState);
    }

    private void ShootTarget()
    {
        _pS.Shoot();
        _pS.isCharging = false;
        ChangeIaState("wait");
    }

    private void SearchTarget()
    {
        _target = null;

        float bestDistance = 100000f;
        int bestIndex = -1;
        for (int i = 0; i < InputManagerScript.players.Count; i++)
        {
            var potentialTarget = InputManagerScript.players[i];
            if (Vector3.Distance(potentialTarget.position, transform.position) < bestDistance)
            {
                if (InputManagerScript.players[i].transform.position != transform.position)
                {
                    bestDistance = Vector3.Distance(potentialTarget.position, transform.position);
                    bestIndex = i;
                }
            }
            if(bestIndex != -1)
                _target = InputManagerScript.players[bestIndex];

            if (_target != null)
            {
                _decidedCharge = Random.Range(.65f, _pS.maxCharge);
                ChangeIaState("charge-attack");
            }
        }

        if (_target == null)
            ChangeIaState("wait");
    }

    private void BotWait()
    {
        _waitTime += Time.deltaTime;
        if (_waitTime > 1f)
        {
            ChangeIaState("search-target");
            _waitTime = 0;
        }
    }

    private void ChargeAttack()
    {
        if (_shootCooldown > 0) return;
        if (_target == null || _target.position.y < -.25f)
        {
            ChangeIaState("search-target");
            return;
        }

        _pS.isCharging = true;
        if (_pS.chargeAmount >= _decidedCharge) 
        {
            ChangeIaState("shoot-target");
        }
    }
}