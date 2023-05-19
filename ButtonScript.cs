using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ButtonScript : MonoBehaviour, IGenericInteractable
{
    [SerializeField] private Transform button;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SimpleAudioEvent clockSound;
    [SerializeField] private SimpleAudioEvent buttonSound;
    private float _buttonStartPos;

    [SerializeField] private bool CanBePressedMultipleTimes = false;
    [SerializeField] private bool isPressed = false;


    [Space(30)] [SerializeField] private float timer = 0;
    private float _currentTimer;
    [SerializeField] private AudioSource stopwatchSound;

    private WaitForSeconds _wait = new(1f);
    private WaitForSeconds _wait_half = new(0.5f);
    private WaitForSeconds _wait_quarter = new(0.25f);

    [SerializeField] private CableGenerator cableGenerator;

    [Space(25)] public UnityEvent OnPress;

    [Space(10)] public bool hasReleaseEvent = false;
    public UnityEvent OnRelease;


    private void Start()
    {
        _buttonStartPos = button.localPosition.y;

        var eventCount = OnPress.GetPersistentEventCount();
        if (eventCount > 0)
        {
            cableGenerator.targets = new Transform[eventCount];
            for (var i = 0; i < eventCount; i++)
                cableGenerator.targets[i] = ((Component)OnPress.GetPersistentTarget(i)).transform;

            cableGenerator.CreateCable();
        }
    }

    public void Press()
    {
        if (isPressed) return;
        isPressed = true;
        OnPress.Invoke();
        buttonSound.Play(audioSource);

        button.DOLocalMoveY(_buttonStartPos + 0.3f, 0.25f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            if (timer != 0)
                StartCoroutine(Timer());
            else if (CanBePressedMultipleTimes)
                ResetButton();
        });
    }

    public void Interact(Vector3 pos)
    {
        Press();
    }


    private void ResetButton()
    {
        button.DOLocalMoveY(_buttonStartPos, 0.65f).SetEase(Ease.InOutQuad).SetEase(Ease.OutBounce)
            .SetDelay(0.3f).OnComplete(() => { isPressed = false; });
    }

    private IEnumerator Timer()
    {
        _currentTimer = timer;
        StartCoroutine(TimerTicks());
        stopwatchSound.Play();
        while (_currentTimer > 0)
        {
            _currentTimer -= Time.deltaTime;
            yield return null;
        }

        stopwatchSound.Stop();
        if (!hasReleaseEvent)
            OnPress.Invoke();
        else
            OnRelease.Invoke();

        ResetButton();
        StopAllCoroutines();
    }

    private IEnumerator TimerTicks()
    {
        clockSound.Play(audioSource);

        if (_currentTimer > timer / 1.5f)
            yield return _wait;
        else if (_currentTimer > timer / 3)
            yield return _wait_half;
        else
            yield return _wait_quarter;

        StartCoroutine(TimerTicks());
    }

    private void OnDrawGizmos()
    {
        var eventCount = OnPress.GetPersistentEventCount();
        if (eventCount > 0)
        {
            var targets = new Transform[eventCount];
            Gizmos.color = Color.red;

            for (var i = 0; i < eventCount; i++)
            {
                if (OnPress.GetPersistentTarget(i) == null) continue;
                targets[i] = ((Component)OnPress.GetPersistentTarget(i)).transform;

                if (targets[i] != null)
                    Gizmos.DrawLine(transform.position, targets[i].position);
            }
        }
    }
}