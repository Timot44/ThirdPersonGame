using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThirdPersonShoot : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    private PlayerCommands _playerCommands;
    public bool isAiming;
    public float aimSensitivity;
    public float normalSensitivity;
    private void Start()
    {
        _playerCommands = GetComponent<PlayerCommands>();
        _playerCommands.playerInputActions.Player.Aim.started += AimOnstarted;
        _playerCommands.playerInputActions.Player.Aim.canceled += AimOnstarted;
        normalSensitivity = _playerCommands.cameraRotationSpeed;
    }

    private void AimOnstarted(InputAction.CallbackContext context)
    {
        Debug.Log(context);
        isAiming = context.ReadValueAsButton();
    }

    private void Update()
    {
        HandleAim();
    }


    void HandleAim()
    {
        if (isAiming)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            _playerCommands.SetSensitivity(aimSensitivity);
        }
        else if (!isAiming)
        {
            aimVirtualCamera.gameObject.SetActive(false);
            _playerCommands.SetSensitivity(normalSensitivity);
        }
    }
}