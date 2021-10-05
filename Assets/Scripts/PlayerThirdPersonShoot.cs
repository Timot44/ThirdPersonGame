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
    public bool isAiming, isShooting;
    public float aimSensitivity;
    public float normalSensitivity;
    [SerializeField] private LayerMask layers;
    [SerializeField]
    private float aimRotatePower = 0.15f;
    
    [SerializeField] private GameObject crosshairImg;
    private Vector3 worldMousePosition;
    [SerializeField] private GameObject projectile;
    [SerializeField] private Transform projectileSpawnPosition;
    private void Start()
    {
        _playerCommands = GetComponent<PlayerCommands>();
        _playerCommands.playerInputActions.Player.Aim.started += AimOnstarted;
        _playerCommands.playerInputActions.Player.Aim.canceled += AimOnstarted;
        _playerCommands.playerInputActions.Player.Shoot.started += ShootOnstarted;
        _playerCommands.playerInputActions.Player.Shoot.canceled += ShootOnstarted;
        normalSensitivity = _playerCommands.cameraRotationSpeed;
    }

    private void ShootOnstarted(InputAction.CallbackContext context)
    {
        isShooting = context.ReadValueAsButton();
    }

    private void AimOnstarted(InputAction.CallbackContext context)
    {
        isAiming = context.ReadValueAsButton();
    }

    private void Update()
    {
        HandleAim();
        HandleShoot();
    }


    void HandleAim()
    {
        if (isAiming)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            _playerCommands.SetSensitivity(aimSensitivity);
            crosshairImg.SetActive(true);
            worldMousePosition = Vector3.zero;
            //Récupère la pos du milieux de l'écran
             Vector2 middleScreenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = Camera.main.ScreenPointToRay(middleScreenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 9999, layers))
            {
                worldMousePosition = raycastHit.point;
            }

            Vector3 aimTargetPosition = worldMousePosition;
            aimTargetPosition.y = transform.position.y;
            Vector3 aimDirection = (aimTargetPosition - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, aimRotatePower * Time.deltaTime);
        }
        else if (!isAiming)
        {
            aimVirtualCamera.gameObject.SetActive(false);
            crosshairImg.SetActive(false);
            _playerCommands.SetSensitivity(normalSensitivity);
        }
    }

    void HandleShoot()
    {
        if (isShooting)
        {
            Vector3 aimDirection = (worldMousePosition - projectileSpawnPosition.position).normalized;
            Instantiate(projectile, projectileSpawnPosition.position, Quaternion.LookRotation(aimDirection, Vector3.up));
            isShooting = false;
        }
    }
}