using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace RageRunGames.EasyFlyingSystem
{
    public class DroneVideoPanelItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage videoFeedImage;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI batteryText;
        [SerializeField] private TextMeshProUGUI altitudeText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private Image batteryFill;
        [SerializeField] private Image statusIndicator;
        [SerializeField] private Button selectButton;

        [Header("Settings")]
        [SerializeField] private Color normalStatusColor = Color.green;
        [SerializeField] private Color warningStatusColor = Color.yellow;
        [SerializeField] private Color dangerStatusColor = Color.red;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private Color selectedColor = Color.yellow;

        private DroneController associatedDrone;
        private Camera droneCamera;
        private RenderTexture renderTexture;
        private float updateTimer;
        private bool isSelected = false;

        public static event System.Action<DroneController> OnDroneSelected;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }
        }

        // Initialize the panel with a specific drone
        public void Initialize(DroneController drone)
        {
            associatedDrone = drone;
            
            if (drone.GetComponentInChildren<Camera>() is Camera cam)
            {
                droneCamera = cam;
                SetupVideoFeed();
            }

            // Subscribe to drone status updates
            if (associatedDrone != null)
            {
                // Remove any existing subscription first
                associatedDrone.OnStatusUpdate -= UpdateStatus;
                // Add new subscription
                associatedDrone.OnStatusUpdate += UpdateStatus;
                
                // Initial status update
                UpdateStatus(associatedDrone.transform.position, 
                           associatedDrone.GetComponent<Rigidbody>().linearVelocity, 
                           associatedDrone.IsGrounded);
            }
        }

        private void OnSelectButtonClicked()
        {
            if (associatedDrone != null)
            {
                DeselectAllDrones();
                
                isSelected = true;
                UpdateSelectionVisual();
                
                OnDroneSelected?.Invoke(associatedDrone);
            }
        }

        private void UpdateSelectionVisual()
        {
            if (selectButton != null)
            {
                Image buttonImage = selectButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isSelected ? selectedColor : Color.white;
                }
            }
        }

        private void DeselectAllDrones()
        {
            DroneVideoPanelItem[] allPanels = FindObjectsOfType<DroneVideoPanelItem>();
            foreach (var panel in allPanels)
            {
                panel.isSelected = false;
                panel.UpdateSelectionVisual();
            }
        }

        private void SetupVideoFeed()
        {
            if (droneCamera != null && videoFeedImage != null)
            {
                // Create render texture if it doesn't exist
                if (renderTexture == null)
                {
                    renderTexture = new RenderTexture(200, 200, 16);
                }
                
                // Set up camera
                droneCamera.targetTexture = renderTexture;
                
                // Set up video feed
                videoFeedImage.texture = renderTexture;
                videoFeedImage.SetNativeSize();
            }
        }

        // Update all UI elements with current drone status
        public void UpdateStatus(Vector3 position, Vector3 velocity, bool isGrounded)
        {
            // Update altitude and speed
            float altitude = position.y;
            float speed = velocity.magnitude;

            if (altitudeText != null)
            {
                altitudeText.text = $"Alt: {altitude:F1}m";
            }

            if (speedText != null)
            {
                speedText.text = $"Vel: {speed:F1}m/s";
            }

            if (statusText != null)
            {
                NetworkObject networkObject = associatedDrone.GetComponent<NetworkObject>();
                ulong networkId = networkObject != null ? networkObject.NetworkObjectId : 0;
                
                statusText.text = $"Drone {networkId}\n" +
                                 $"Status: {(isGrounded ? "Grounded" : "Flying")}";
            }

            // Update battery with default value (100%)
            if (batteryText != null)
            {
                batteryText.text = "100%";
            }

            if (batteryFill != null)
            {
                batteryFill.fillAmount = 1f;
            }

            if (statusIndicator != null)
            {
                statusIndicator.color = isGrounded ? warningStatusColor : normalStatusColor;
            }
        }

        private void OnDestroy()
        {
            if (associatedDrone != null)
            {
                associatedDrone.OnStatusUpdate -= UpdateStatus;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        private void Start()
        {
            if (associatedDrone == null)
            {
                Debug.LogError("DroneVideoPanelItem: No DroneController found!");
                return;
            }

            // Initial status update
            UpdateStatus(associatedDrone.transform.position, 
                       associatedDrone.GetComponent<Rigidbody>().linearVelocity, 
                       associatedDrone.IsGrounded);
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                if (associatedDrone != null)
                {
                    UpdateStatus(associatedDrone.transform.position,
                               associatedDrone.GetComponent<Rigidbody>().linearVelocity,
                               associatedDrone.IsGrounded);
                }
                updateTimer = 0f;
            }
        }
    }
} 