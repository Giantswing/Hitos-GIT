using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerReceiveInput : MonoBehaviour
{
    public Action OnDash;
    public Action<Vector2> OnMove;
    public Action OnChargeShot;
    public Action OnChargeRelease;
    public Vector2 inputDirectionTo;


    private void Start()
    {
    }


    public void InputMove(InputAction.CallbackContext context)
    {
        if (!enabled) return;
        inputDirectionTo = context.ReadValue<Vector2>();
        OnMove?.Invoke(inputDirectionTo);
    }

    public void InputDash(InputAction.CallbackContext context)
    {
        if (!enabled) return;
        if (context.performed) OnDash?.Invoke();
    }

    public void InputShoot(InputAction.CallbackContext context)
    {
        if (!enabled) return;
        if (context.performed) OnChargeShot?.Invoke();
        else if (context.canceled) OnChargeRelease?.Invoke();
    }
}