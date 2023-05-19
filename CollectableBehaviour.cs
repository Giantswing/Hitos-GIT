using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class CollectableBehaviour : MonoBehaviour
{
    private bool _isShrinking = false;
    protected Tweener _tweener;
    protected bool isPickedUp = false;

    [HideInInspector] public int isFollowing = 0;
    [HideInInspector] public Transform _whoToFollow;
    [HideInInspector] public Transform _whoToFollowTrue;

    public Transform displayObject;
    [SerializeField] protected BoxCollider myCollider;
    [SerializeField] private SmartData.SmartInt.IntWriter playerHealth;
    [SerializeField] private SmartData.SmartEvent.EventDispatcher onCollect;
    [SerializeField] private Color pickUpCoinColor;

    [Space(10)] [SerializeField] private SimpleAudioEvent collectSound;

    private HoldableItem _holdableItem;

    private bool hasAddedScore = false;


    [HideInInspector]
    public enum PickUpAnimation
    {
        Rotatable,
        RotatableSlow,
        Static,
        Floating
    }

    public Collider getCollider()
    {
        return myCollider;
    }

    public PickUpAnimation pickUpAnimation;

    [HideInInspector]
    public enum PickUp
    {
        Coin,
        Poop,
        Holder,
        Throwable,
        HeartContainer
    }

    public PickUp pickUp;

    public int scoreValue = 1;


    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        switch (pickUpAnimation)
        {
            case PickUpAnimation.Rotatable:
                displayObject.DOLocalRotate(new Vector3(0, 360, 0), 2f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
                break;

            case PickUpAnimation.RotatableSlow:

                displayObject.DOLocalRotate(new Vector3(0, 360, 0), 8f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
                break;
            case PickUpAnimation.Floating:
                displayObject.DOLocalMoveY(0.2f, 1f).SetLoops(-1, LoopType.Yoyo);
                break;
        }


        myCollider.enabled = false;
        StartCoroutine(AllowCollection());

        if (pickUp == PickUp.Holder)
        {
            _holdableItem = new HoldableItem();
            _holdableItem.itemTransform = transform;
            _holdableItem.isThrowable = false;
        }

        else if (pickUp == PickUp.Throwable)
        {
            _holdableItem = new HoldableItem();
            _holdableItem.itemTransform = transform;
            _holdableItem.isThrowable = true;
        }

        isPickedUp = false;
        //
    }

    private IEnumerator AllowCollection()
    {
        yield return new WaitForSeconds(0.5f);
        myCollider.enabled = true;
    }


    public void Update()
    {
        if (isFollowing == 2)
        {
            Vector3 position = _whoToFollow.position;
            _tweener.ChangeEndValue(position, true);
            float distance = Vector3.Distance(transform.position, _whoToFollowTrue.position);
            if (distance < .6f && _isShrinking == false)
            {
                transform.SetParent(null);
                if (pickUp == PickUp.Throwable)
                {
                    PlayerInteractionHandler playerInteraction =
                        _whoToFollowTrue.gameObject.GetComponent<PlayerInteractionHandler>();
                    if (playerInteraction == null) return;
                    isFollowing = 3;
                    transform.DOLocalRotate(Vector3.zero, 0.5f);
                    playerInteraction.holdingItems.Add(_holdableItem);
                    isPickedUp = true;
                    SoundMaster.PlayTargetSound(transform.position, collectSound, true);
                }
                else if (pickUp == PickUp.Holder)
                {
                    if (collectSound != null)
                        SoundMaster.PlayTargetSound(transform.position, collectSound, true);
                    PlayerInteractionHandler playerInteraction =
                        _whoToFollowTrue.gameObject.GetComponent<PlayerInteractionHandler>();
                    if (playerInteraction == null) return;
                    isFollowing = 3;
                    myCollider.size = new Vector3(3, 3, 3);
                    playerInteraction.holdingItems.Add(_holdableItem);
                    transform.DOLocalRotate(Vector3.zero, 0.5f);
                }
                else
                {
                    _isShrinking = true;
                    transform.DOScale(0, .1f).OnComplete(() =>
                    {
                        SoundMaster.PlayTargetSound(transform.position, collectSound, true);
                        Destroy(gameObject);
                        GlowHandler.GlowStatic(pickUpCoinColor);
                        DoFinalCollectibleEffect();
                    });
                }
            }
        }
    }

    public void RemoveHolder()
    {
        PlayerInteractionHandler playerInteraction =
            _whoToFollowTrue.gameObject.GetComponent<PlayerInteractionHandler>();
        if (playerInteraction == null) return;

        playerInteraction.holdingItems.Remove(_holdableItem);
    }

    public void DoFinalCollectibleEffect()
    {
        if (pickUp == PickUp.Coin)
        {
            FXMaster.SpawnFX(transform.position, (int)FXListAuto.Coin);
        }

        else if (pickUp == PickUp.Poop)
        {
            FXMaster.SpawnFX(transform.position, (int)FXListAuto.Heal);

            playerHealth.value++;
            onCollect.Dispatch();
        }

        else if (pickUp == PickUp.HeartContainer)
        {
            /*
            int heartId = GetComponent<HeartContainer>().HeartContainerID;
            LevelManager.IncreaseHeartContainers(heartId);
            FXMaster.SpawnFX(transform.position, (int)FXListAuto.HeartContainer,
                PlayerMovement.ReturnPlayerTransform());

            SoundMaster.PlaySound(transform.position, (int)SoundListAuto.HeartContainerCollect, true);
        */
        }


        DOTween.Kill(transform);
        DOTween.Kill(displayObject);
    }

    public void Collect(Transform follow, Transform truePlayer)
    {
        if (isFollowing == 0)
        {
            isFollowing = 1;
            _whoToFollow = follow;
            _whoToFollowTrue = truePlayer;

            Vector3 awayDirection = (transform.position - follow.position).normalized;
            Vector3 awayPosition = transform.position + awayDirection * 1.4f;


            transform.DOMove(awayPosition, Random.Range(0.15F, 0.35F), false).SetEase(Ease.InOutCubic).onComplete +=
                () => { StartFollowingPlayer(follow, truePlayer); };
        }
    }

    public void StartFollowingPlayer(Transform follow, Transform truePlayer)
    {
        isFollowing = 1;
        _whoToFollow = follow;
        _whoToFollowTrue = truePlayer;

        Transform tempTransform = transform;
        Vector3 tempPosition = tempTransform.position;
        Vector3 awayDirection = (tempPosition - follow.position).normalized;
        Vector3 position = tempPosition + awayDirection * 2;

        _tweener = transform.DOMove(_whoToFollow.position, .2f, false).SetEase(Ease.OutCubic);
        isFollowing = 2;
        //_tweener.Play();
    }
}