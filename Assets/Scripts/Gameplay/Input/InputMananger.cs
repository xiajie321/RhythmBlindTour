using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputControl = Gameplay.Input.InputControl;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public InputControl inputMap;

    private void Awake()
    {
        if (inputMap == null)
        {
            inputMap = new();
        }

        Instance = this;
    }

    private void OnEnable()
    {
        inputMap.Enable();
    }

    private void OnDisable()
    {
        inputMap.Disable();
    }

    public bool CheckTap()
    {
        return inputMap.Gameplay.Tap.WasPressedThisFrame();
    }

    public bool CheckSlideLeft()
    {
        return CheckSlide() == new Vector2Int(-1, 0) || inputMap.Gameplay.Left.WasPressedThisFrame();
    }

    public bool CheckSlideRight()
    {
        return CheckSlide() == new Vector2Int(1, 0) || inputMap.Gameplay.Right.WasPressedThisFrame();
    }

    public bool CheckSlideUp()
    {
        return CheckSlide() == new Vector2Int(0, 1) || inputMap.Gameplay.Up.WasPressedThisFrame();
    }

    public bool CheckSlideDown()
    {
        return CheckSlide() == new Vector2Int(0, -1) || inputMap.Gameplay.Down.WasPressedThisFrame();
    }

    private Vector2Int CheckSlide()
    {
        Vector2 delta = inputMap.Gameplay.Slide.ReadValue<Vector2>();
        if (delta == Vector2.zero)
        {
            return Vector2Int.zero;
        }

        delta.Normalize();
        var (x, y) = (delta.x, delta.y);
        Vector2Int temp = Mathf.Abs(x) > Mathf.Abs(y)
            ? new Vector2Int((int)Mathf.Sign(x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(y));

        return temp;
    }
}