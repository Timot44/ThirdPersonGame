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

    /* [Header("Camera Parameters")] [SerializeField]
     private bool isInvertedY;
     [SerializeField] private CinemachineFreeLook cameraFreeLook;
     [SerializeField] private float lookSpeed;*/

    private PlayerInputActions _playerInputActions;

    [Header("Brut Parameters")] [SerializeField]
    private Vector2 inputsVector;

    [SerializeField] private Vector3 currentMovement, currentRunMovement, appliedMovement;
    [SerializeField] private bool isMovementPressed, isRunPressed;
    [SerializeField] private Animator animator;
    private Quaternion _camRot;
    [SerializeField] private Transform camera;

    [Header("Multiplier Parameters")] [SerializeField]
    private float rotationPower = 1f;

    [SerializeField] private float runMultiplier = 3f;
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

    private void Awake()
    {
        _characterController = gameObject.GetComponent<CharacterController>();
        //Player Inputs callback 
        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Movement.started += OnMovementInput;
        _playerInputActions.Player.Movement.canceled += OnMovementInput;
        _playerInputActions.Player.Movement.performed += OnMovementInput;
        _playerInputActions.Player.Run.started += OnRun;
        _playerInputActions.Player.Run.canceled += OnRun;
        _playerInputActions.Player.Jump.started += OnJump;
        _playerInputActions.Player.Jump.canceled += OnJump;


        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _jumpCountHash = Animator.StringToHash("jumpCount");
        SetJumpVariables();
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
            currentMovement.y = _initialJumpVelocities[_jumpCount];
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
            appliedMovement.x = currentRunMovement.x;
            appliedMovement.z = currentRunMovement.z;
        }
        else
        {
            appliedMovement.x = currentMovement.x;
            appliedMovement.z = currentRunMovement.z;
        }

        Vector3 movement = _camRot * appliedMovement;
        _characterController.Move(movement * Time.deltaTime);
       

        HandleGravity();
        HandleJump();
       
    }

    void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    void HandleGravity()
    {
        bool isFalling = currentMovement.y <= 0.0f || !isJumpPressed;
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
          
            currentMovement.y = _groundedGravity;
            appliedMovement.y = _groundedGravity;
        }
        else if (isFalling) //Le joueur est entrain de tomber
        {
            float previousYVelocity = currentMovement.y;
            currentMovement.y = currentMovement.y + (_jumpGravities[_jumpCount] * fallMultiplier * Time.deltaTime);
            appliedMovement.y = Mathf.Max((previousYVelocity + currentMovement.y) * 0.5f, -20.0f);
        }
        else
        {
            //Velocity Verlet intégration
            //On récupère la vélocité Y du player donc l'ancienne
            float previousYVelocity = currentMovement.y;
            // On calcule la nouvelle vélocité
            currentMovement.y = currentMovement.y + (_jumpGravities[_jumpCount] * Time.deltaTime);
            //On calcule la next velocité en combinant l'ancienne et la nouvelle
            appliedMovement.y = (previousYVelocity + currentMovement.y) * 0.5f;
        }
    }

    void HandleRotation()
    {
        Vector3 camForward = camera.forward;
        camForward.y = 0f;
        _camRot = Quaternion.LookRotation(camForward); 
        
        //rotation actuel du joueur
        Quaternion currentRotation = transform.rotation;
        if (isMovementPressed)
        {
            
            float targetAngle = Mathf.Atan2(inputsVector.x, inputsVector.y) * Mathf.Rad2Deg + camera.eulerAngles.y;
            //Rotation créer avec le movement du joueur
            Quaternion rot = Quaternion.Euler(0f, targetAngle, 0f);
            //Rotation final slerp
            transform.rotation = Quaternion.Slerp(currentRotation, rot, rotationPower * Time.deltaTime);
            
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
        inputsVector = context.ReadValue<Vector2>();
        currentMovement.x = inputsVector.x;
        currentMovement.z = inputsVector.y;
        currentRunMovement.x = inputsVector.x * runMultiplier;
        currentRunMovement.z = inputsVector.y * runMultiplier;
        isMovementPressed = inputsVector.x != 0 || inputsVector.y != 0;
    }


    private void OnEnable()
    {
        _playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions.Player.Disable();
    }
}