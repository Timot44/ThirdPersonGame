using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCommands : MonoBehaviour
{
    private CharacterController _characterController;
    private PlayerThirdPersonShoot _playerThirdPersonShoot;
    public PlayerInputActions playerInputActions;

    [Header("Brut Parameters")] private Vector2 _inputsVector;

    public Vector3 _currentMovement, _currentRunMovement;
    [SerializeField] private Vector3 appliedMovement;
    [SerializeField] public bool isMovementPressed, isRunPressed;
    public Animator animator;
    private Quaternion _camRot;
    private Transform _camera;

    [Header("Multiplier Parameters")] [SerializeField]
    private float rotationPower = 1f;

    [SerializeField] private float runMultiplier = 3f;
    [SerializeField] private float walkMultiplier = 2f;
    private float _gravity = -9.8f;
    private float _groundedGravity = -0.5f;

    private int _isWalkingHash;
    private int _isRunningHash;
   
    [Header("Jump Parameters")] [SerializeField]
    private bool isJumpPressed;

    private float _initialJumpForce;
    [SerializeField] private float maxJumpHeight = 4.0f;
    [SerializeField] private float maxJumpTime = 0.75f;
    private bool _isJumping;
    private int _isJumpingHash;
    private bool _isJumpAnimating;
    private int _jumpCount = 0;
    private int _jumpCountHash;
    private Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
    private Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();
    private Coroutine _currentJumpResetRoutine;

    [Header("Camera Parameters")]
    [SerializeField] private GameObject cinemachineVirtualCameraFollowTarget;
    private Vector2 _lookInputs;
    private float _threshold = 0.01f;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    [Tooltip("How far in degrees can you move the camera up")]
    public float topClamp = 70.0f;
    public float cameraRotationSpeed;
    [Tooltip("How far in degrees can you move the camera down")]
    public float bottomClamp = -30.0f;
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float cameraAngleOverride = 0.0f;

    private void Awake()
    {
        _characterController = gameObject.GetComponent<CharacterController>();
        _playerThirdPersonShoot = GetComponent<PlayerThirdPersonShoot>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        //Player Inputs callback 
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Movement.started += OnMovementInput;
        playerInputActions.Player.Movement.canceled += OnMovementInput;
        playerInputActions.Player.Movement.performed += OnMovementInput;
        playerInputActions.Player.Run.started += OnRun;
        playerInputActions.Player.Run.canceled += OnRun;
        playerInputActions.Player.Jump.started += OnJump;
        playerInputActions.Player.Jump.canceled += OnJump;
        playerInputActions.Player.Look.started += LookOnstarted;
        playerInputActions.Player.Look.canceled += LookOnstarted;
        playerInputActions.Player.Look.performed += LookOnstarted;

        _camera = Camera.main.transform;
        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _jumpCountHash = Animator.StringToHash("jumpCount");
        SetJumpVariables();
    }

    private void LookOnstarted(InputAction.CallbackContext context)
    {
        _lookInputs = context.ReadValue<Vector2>();
    }


    void SetJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        _gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        _initialJumpForce = (2 * maxJumpHeight) / timeToApex;
        float secondJumpGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpForce = (2 * (maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
        float thirdJumpForce = (2 * (maxJumpHeight + 4) / (timeToApex * 1.5f));

        _initialJumpVelocities.Add(1, _initialJumpForce);
        _initialJumpVelocities.Add(2, secondJumpForce);
        _initialJumpVelocities.Add(3, thirdJumpForce);

        _jumpGravities.Add(0, _gravity);
        _jumpGravities.Add(1, _gravity);
        _jumpGravities.Add(2, secondJumpGravity);
        _jumpGravities.Add(3, thirdJumpGravity);
    }

    void HandleJump()
    {
        if (!_isJumping && _characterController.isGrounded && isJumpPressed)
        {
            if (_jumpCount < 3 && _currentJumpResetRoutine != null)
            {
                StopCoroutine(_currentJumpResetRoutine);
            }

            animator.SetBool(_isJumpingHash, true);
            _isJumpAnimating = true;
            _isJumping = true;
            _jumpCount += 1;
            animator.SetInteger(_jumpCountHash, _jumpCount);
            _currentMovement.y = _initialJumpVelocities[_jumpCount];
            appliedMovement.y = _initialJumpVelocities[_jumpCount];
        }

        else if (!isJumpPressed && _isJumping && _characterController.isGrounded)
        {
            _isJumping = false;
        }
    }

    IEnumerator JumpResetRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        _jumpCount = 0;
    }

    void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
    }

    private void Update()
    {
        HandleAnimation();
        HandleRotation();

        if (isRunPressed)
        {
            appliedMovement.x = _currentRunMovement.x;
            appliedMovement.z = _currentRunMovement.z;
        }
        else
        {
            appliedMovement.x = _currentMovement.x;
            appliedMovement.z = _currentMovement.z;
        }

        if (_playerThirdPersonShoot.isAiming)
        {
            _characterController.Move(transform.TransformDirection(appliedMovement) * Time.deltaTime);
        }

        Vector3 movement = _camRot * appliedMovement;
        _characterController.Move(movement * Time.deltaTime);


        HandleGravity();
        HandleJump();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    void CameraRotation()
    {
        if (_lookInputs.sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += _lookInputs.x * cameraRotationSpeed * Time.deltaTime;
            _cinemachineTargetPitch += _lookInputs.y * cameraRotationSpeed * Time.deltaTime;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);
        //Cinemachine will follow this target
        cinemachineVirtualCameraFollowTarget.transform.rotation =
            Quaternion.Euler(_cinemachineTargetPitch + cameraAngleOverride, _cinemachineTargetYaw, 0.0f);
    }

    void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    void HandleGravity()
    {
        bool isFalling = _currentMovement.y <= 0.0f || !isJumpPressed;
        float fallMultiplier = 2.0f;

        //Permet d'appliquer de la gravité dépendant si le joueur en contact avec le sol ou pas
        if (_characterController.isGrounded)
        {
            if (_isJumpAnimating)
            {
                animator.SetBool(_isJumpingHash, false);
                _isJumpAnimating = false;
                _currentJumpResetRoutine = StartCoroutine(JumpResetRoutine());
                if (_jumpCount == 3)
                {
                    _jumpCount = 0;
                    animator.SetInteger(_jumpCountHash, _jumpCount);
                }
            }

            _currentMovement.y = _groundedGravity;
            appliedMovement.y = _groundedGravity;
        }
        else if (isFalling) //Le joueur est entrain de tomber
        {
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * fallMultiplier * Time.deltaTime);
            appliedMovement.y = Mathf.Max((previousYVelocity + _currentMovement.y) * 0.5f, -20.0f);
        }
        else
        {
            //Velocity Verlet intégration
            //On récupère la vélocité Y du player donc l'ancienne
            float previousYVelocity = _currentMovement.y;
            // On calcule la nouvelle vélocité
            _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * Time.deltaTime);
            //On calcule la next velocité en combinant l'ancienne et la nouvelle
            appliedMovement.y = (previousYVelocity + _currentMovement.y) * 0.5f;
        }
    }

    void HandleRotation()
    {
        Vector3 camForward = _camera.forward;
        camForward.y = 0f;
        _camRot = Quaternion.LookRotation(camForward);

        //rotation actuel du joueur
        Quaternion currentRotation = transform.rotation;
        if (isMovementPressed && !_playerThirdPersonShoot.isAiming)
        {
            float targetAngle = Mathf.Atan2(_inputsVector.x, _inputsVector.y) * Mathf.Rad2Deg + _camera.eulerAngles.y;
            //Rotation créer avec le movement du joueur
            Quaternion rot = Quaternion.Euler(0f, targetAngle, 0f);
            //Rotation final slerp
            if (!_playerThirdPersonShoot.isAiming)
            {
                transform.rotation = Quaternion.Slerp(currentRotation, rot, rotationPower * Time.deltaTime);
            }
           
            
        }
    }

    // Update is called once per frame
    void HandleAnimation()
    {
        bool isWalking = animator.GetBool(_isWalkingHash);
        bool isRunning = animator.GetBool(_isRunningHash);
     
        if (isMovementPressed && !isWalking)
        {
            animator.SetBool(_isWalkingHash, true);
           
        }
        else if (!isMovementPressed && isWalking)
        {
            animator.SetBool(_isWalkingHash, false);
        }
        
        if (isMovementPressed && isRunPressed && !isRunning)
        {
            animator.SetBool(_isRunningHash, true);
        }
        else if ((!isMovementPressed || !isRunPressed) && isRunning)
        {
            animator.SetBool(_isRunningHash, false);
        }
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        //On récupère grace au context la valeur des inputs
        _inputsVector = context.ReadValue<Vector2>();
        _currentMovement.x = _inputsVector.x * walkMultiplier;
        _currentMovement.z = _inputsVector.y * walkMultiplier;
        _currentRunMovement.x = _inputsVector.x * runMultiplier;
        _currentRunMovement.z = _inputsVector.y * runMultiplier;
        isMovementPressed = _inputsVector.x != 0 || _inputsVector.y != 0;
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    public void SetSensitivity(float sensitivity)
    {
        cameraRotationSpeed = sensitivity;
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
    }
}