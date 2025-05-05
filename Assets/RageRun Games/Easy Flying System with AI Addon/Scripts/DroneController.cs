using UnityEngine;
using Unity.Netcode;

namespace RageRunGames.EasyFlyingSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class DroneController : BaseFlyController
    {
        [Header("Input Settings")]
        [HideInInspector] public InputType inputType;
        [SerializeField] private NetworkVariable<InputType> networkInputType = new NetworkVariable<InputType>(InputType.AI);

        [Header("Controller Settings")]
        [SerializeField] private bool maintainAltitude = true;
        [SerializeField] protected bool useGravityOnNoInput;
        
        [Header("Hover Settings")]
        [SerializeField] protected bool enableHover;
        [Range(0, 10)]
        [SerializeField] protected float hoverAmplitude = 1.25f;
        [Range(0, 10)]
        [SerializeField] protected float hoverFrequency = 2f;

        [Header("Ground Settings")]
        [SerializeField] protected float groundCheckDistance = 0.2f;
        [SerializeField] protected bool decelerateOnGround;
        [SerializeField] protected float decelSpeedOnGround = 4f;

        [Header("Network Settings")]
        [SerializeField] private float networkUpdateInterval = 0.1f;

        private float timer;
        private float networkUpdateTimer;
        private BaseInputHandler currentInputHandler;
        private DroneCommunication droneCommunication;
        private bool isSelected = false;

        public bool IsGrounded { get; private set; } = true;
        public Vector3 LocalVelocity => transform.InverseTransformDirection(rb.linearVelocity);
        public float MaxSpeed => maxSpeed;
        public bool IsSelected => isSelected;

        public event System.Action<Vector3, Vector3, bool> OnStatusUpdate;
        public event System.Action<bool> OnSelectionChanged;

        private void Awake()
        {
            networkInputType.OnValueChanged += OnInputTypeChanged;
            DroneVideoPanelItem.OnDroneSelected += HandleDroneSelection;
        }

        private void OnInputTypeChanged(InputType oldValue, InputType newValue)
        {
            //Debug.Log($"Input type changed from {oldValue} to {newValue} - IsOwner: {networkObject.IsOwner}, IsClient: {NetworkManager.Singleton.IsClient}");
            inputType = newValue;
            ApplyInputType();
        }

        private void ApplyInputType()
        {
            //Debug.Log($"Applying input type: {inputType} - IsOwner: {networkObject.IsOwner}, IsClient: {NetworkManager.Singleton.IsClient}");
            switch (inputType)
            {
                case InputType.Keyboard:
                    AddKeyboardInputs();
                    break;
                case InputType.AI:
                    AddAIInputs();
                    break;
                case InputType.Joystick:
                    AddJoystickInputs();
                    break;
                case InputType.Mobile:
                    AddMobileInputs();
                    break;
                case InputType.Mouse:
                    AddMouseInputs();
                    break;
            }
        }

        public void SetInputType(InputType newInputType)
        {
            if (networkObject.IsOwner || NetworkManager.Singleton.IsServer)
            {
                networkInputType.Value = newInputType;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            droneCommunication = GetComponent<DroneCommunication>();

            if (InputHandler == null)
            {
                Debug.LogWarning("No input is added or selected, adding keyboard input as default");
                InputHandler = gameObject.AddComponent<KeyboardInputHandler>();
            }

            if (DroneManager.Instance != null)
            {
                DroneManager.Instance.RegisterDrone(this);
            }

            // Send initial status when spawned
            if (droneCommunication != null)
            {
                droneCommunication.SendStatus("Drone Spawned");
            }

            // Apply initial input type
            ApplyInputType();
        }

        private void OnDestroy()
        {
            DroneVideoPanelItem.OnDroneSelected -= HandleDroneSelection;
            if (DroneManager.Instance != null)
            {
                DroneManager.Instance.UnregisterDrone(this);
            }
        }

        private void HandleDroneSelection(DroneController selectedDrone)
        {
            bool wasSelected = isSelected;
            isSelected = selectedDrone == this;
            
            if (wasSelected != isSelected)
            {
                OnSelectionChanged?.Invoke(isSelected);
                
                if (NetworkManager.Singleton.IsHost)
                {
                    if (isSelected)
                    {
                        // If this drone is selected, make it the owner and give it keyboard control
                        if (!networkObject.IsOwner)
                        {
                            networkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
                        }
                        SetInputType(InputType.Keyboard);
                        Debug.Log($"Drone {gameObject.name} selected and given keyboard control");
                    }
                    else
                    {
                        // If deselected, switch to AI control
                        SetInputType(InputType.AI);
                        Debug.Log($"Drone {gameObject.name} deselected and switched to AI control");
                    }
                }
            }
        }

        protected override void Update()
        {
            IsGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);

            if (IsGrounded && rb.linearVelocity != Vector3.zero && decelerateOnGround)
            {
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, decelSpeedOnGround * Time.deltaTime);
            }

            base.Update();

            // Send status update for both owner and non-owner
            OnStatusUpdate?.Invoke(transform.position, GetComponent<Rigidbody>().linearVelocity, IsGrounded);
        }

        protected override void FixedUpdate()
        {
            //Debug.Log($"FixedUpdate called - IsOwner: {networkObject.IsOwner}, IsClient: {NetworkManager.Singleton.IsClient}, InputHandler: {InputHandler != null}");
            // Only apply movement forces if we're the owner or if we're the server
            if (NetworkManager.Singleton.IsServer)
            {
                HandleThrottle();
                HandleRotations();
            }
        }

        protected override void HandleRotations()
        {
            base.HandleRotations();
            
            if (autoForwardMovement)
            {
                currentPitch = pitchAmount;
            }

            Quaternion currentRotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);
            rb.MoveRotation(currentRotation);
        }

        protected override void UpdateMovement(IInputHandler inputHandler)
        {
            //Debug.Log($"UpdateMovement called - IsOwner: {networkObject.IsOwner}, IsClient: {NetworkManager.Singleton.IsClient}, InputHandler: {inputHandler != null}");
            // Only apply movement if we're the owner or if we're the server
            if (!NetworkManager.Singleton.IsServer) return;

            Vector3 upVector = Vector3.up;
            upVector.x = 0f;
            upVector.z = 0f;

            float upVectorMagnitude = 1 - upVector.magnitude;
            float gravityMagnitude = Physics.gravity.magnitude * upVectorMagnitude;

            float upwardForce = !useGravityOnNoInput
                ? rb.mass * Physics.gravity.magnitude + gravityMagnitude + inputHandler.Lift * maxSpeed
                : inputHandler.Lift * maxSpeed;

            Vector3 liftForce = Vector3.up * upwardForce;
            Vector3 forwardForce = disablePitch ? Vector3.zero : inputHandler.Pitch * maxSpeed * transform.forward;
            Vector3 sidewaysForce = disableRoll ? Vector3.zero : inputHandler.Roll * maxSpeed * transform.right;

            if (autoForwardMovement)
            {
                forwardForce = maxSpeed * transform.forward;
            }

            if (maintainAltitude)
            {
                forwardForce.y = 0f;
                sidewaysForce.y = 0f;
            }

            if (enableHover && inputHandler.checkInputs)
            {
                timer += Time.deltaTime;
                float hoverForce = Mathf.Sin(timer * hoverFrequency) * hoverAmplitude;
                liftForce += Vector3.up * hoverForce;
            }

            rb.AddForce(forwardForce + liftForce + sidewaysForce, ForceMode.Force);
        }

        public InputType GetInputType()
        {
            return inputType;
        }

        #region Input Helpers

        private void CleanupExistingInputHandler()
        {
            currentInputHandler = GetComponent<BaseInputHandler>();
            if (currentInputHandler != null)
            {
                GameObject mobileControls = GameObject.Find("Mobile Controls UI Holder");
                if (mobileControls != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(mobileControls);
                    }
                    else
                    {
                        DestroyImmediate(mobileControls);
                    }
                }
                DestroyImmediate(currentInputHandler);
            }
        }

        public void AddKeyboardInputs()
        {
            inputType = InputType.Keyboard;
            CleanupExistingInputHandler();
            InputHandler = gameObject.AddComponent<KeyboardInputHandler>();
            currentInputHandler = (BaseInputHandler)InputHandler;
        }

        public void AddAIInputs()
        {
            Debug.Log($"Adding AI inputs - IsOwner: {networkObject.IsOwner}, IsClient: {NetworkManager.Singleton.IsClient}");
            inputType = InputType.AI;
            CleanupExistingInputHandler();
            InputHandler = gameObject.AddComponent<AIInputHandler>();
            currentInputHandler = (BaseInputHandler)InputHandler;
        }

        public void AddJoystickInputs()
        {
            inputType = InputType.Joystick;
            CleanupExistingInputHandler();
            InputHandler = gameObject.AddComponent<JoystickInputHandler>();
            currentInputHandler = (BaseInputHandler)InputHandler;
        }

        public void AddMobileInputs()
        {
            inputType = InputType.Mobile;
            CleanupExistingInputHandler();

            GameObject mobileControls = GameObject.Find("Mobile Controls UI Holder");
            if (mobileControls == null)
            {
                mobileControls = Instantiate(Resources.Load<GameObject>("Mobile Controls UI Holder"), transform, true);
                mobileControls.name = "Mobile Controls UI Holder";
            }

            InputHandler = gameObject.AddComponent<MobileInputHandler>();
            currentInputHandler = (BaseInputHandler)InputHandler;

            MobileController[] controllers = GetComponentsInChildren<MobileController>();
            ((MobileInputHandler)InputHandler).SetMobileInputControllers(controllers);
        }

        public void AddMouseInputs()
        {
            inputType = InputType.Mouse;
            CleanupExistingInputHandler();
            InputHandler = gameObject.AddComponent<MouseInputHandler>();
            currentInputHandler = (BaseInputHandler)InputHandler;
        }

        #endregion

        public void SlowDown(float speed)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero,  speed * Time.deltaTime);
        }

        public void UpdateStatus(Vector3 position, Vector3 velocity, bool isGrounded)
        {
            OnStatusUpdate?.Invoke(position, velocity, isGrounded);
        }
    }
}