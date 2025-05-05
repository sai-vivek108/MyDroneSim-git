using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace RageRunGames.EasyFlyingSystem
{
    public class DroneVideoPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform videoGrid;
        [SerializeField] private GameObject videoPanelPrefab;
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [SerializeField] private int maxPanels = 4;
        [SerializeField] private Vector2 panelSize = new Vector2(200, 150);
        [SerializeField] private float panelSpacing = 10f;

        private Dictionary<DroneController, GameObject> dronePanels = new Dictionary<DroneController, GameObject>();

        private void Awake()
        {
            gameObject.SetActive(true);

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }

            if (videoGrid == null)
            {
                videoGrid = transform;
            }
        }

        private void Start()
        {
            gameObject.SetActive(true);
        }

        // Add a new drone view to the panel
        public void AddDroneView(DroneController drone)
        {
            if (drone == null || dronePanels.ContainsKey(drone)) return;

            if (videoPanelPrefab == null) return;

            // Create new panel
            GameObject panel = Instantiate(videoPanelPrefab, videoGrid);
            panel.SetActive(true);

            // Get the DroneVideoPanelItem component
            DroneVideoPanelItem panelItem = panel.GetComponent<DroneVideoPanelItem>();
            if (panelItem != null)
            {
                panelItem.Initialize(drone);
            }

            // Set up panel components
            RawImage rawImage = panel.GetComponentInChildren<RawImage>();
            TextMeshProUGUI statusText = panel.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI batteryText = panel.transform.Find("Battery")?.GetComponent<TextMeshProUGUI>();
            Button panelCloseButton = panel.transform.Find("Close Button")?.GetComponent<Button>();

            if (panelCloseButton != null)
            {
                panelCloseButton.onClick.AddListener(() => RemoveDroneView(drone));
            }

            // Set up camera feed
            if (rawImage != null)
            {
                Camera droneCamera = drone.GetComponentInChildren<Camera>();
                if (droneCamera != null)
                {
                    // Create a new render texture for this drone
                    RenderTexture renderTexture = new RenderTexture(640, 480, 24);
                    droneCamera.targetTexture = renderTexture;
                    rawImage.texture = renderTexture;
                    
                    // Ensure the camera is enabled
                    droneCamera.enabled = true;
                }
            }

            dronePanels[drone] = panel;
            UpdatePanelLayout();

            // Subscribe to drone events
            drone.OnStatusUpdate += (position, velocity, isGrounded) => UpdateDronePanel(drone, statusText, batteryText);
        }

        // Remove a drone view from the panel
        public void RemoveDroneView(DroneController drone)
        {
            if (drone == null || !dronePanels.ContainsKey(drone)) return;

            // Unsubscribe from drone events
            drone.OnStatusUpdate -= (position, velocity, isGrounded) => UpdateDronePanel(drone, null, null);

            // Clean up render texture
            if (dronePanels.TryGetValue(drone, out GameObject panel))
            {
                RawImage rawImage = panel.GetComponentInChildren<RawImage>();
                if (rawImage != null && rawImage.texture != null)
                {
                    RenderTexture renderTexture = rawImage.texture as RenderTexture;
                    if (renderTexture != null)
                    {
                        renderTexture.Release();
                        Destroy(renderTexture);
                    }
                }
            }

            // Destroy panel
            Destroy(dronePanels[drone]);
            dronePanels.Remove(drone);
            UpdatePanelLayout();
        }

        // Update the UI elements for a specific drone panel
        private void UpdateDronePanel(DroneController drone, TextMeshProUGUI statusText, TextMeshProUGUI batteryText)
        {
            if (statusText != null)
            {
                statusText.text = $"Status: {(drone.IsGrounded ? "Grounded" : "Flying")}";
            }

            if (batteryText != null)
            {
                batteryText.text = "Battery: 100%";
            }
        }

        // Update the layout of all panels in the grid
        private void UpdatePanelLayout()
        {
            int activePanels = dronePanels.Count;
            if (activePanels == 0) return;

            // Limit the number of panels to maxPanels
            if (activePanels > maxPanels)
            {
                // Remove excess panels
                int panelsToRemove = activePanels - maxPanels;
                var keysToRemove = new List<DroneController>(dronePanels.Keys);
                for (int i = 0; i < panelsToRemove; i++)
                {
                    RemoveDroneView(keysToRemove[i]);
                }
                activePanels = maxPanels;
            }

            // Calculate layout
            int columns = Mathf.CeilToInt(Mathf.Sqrt(activePanels));
            int rows = Mathf.CeilToInt((float)activePanels / columns);

            // Position panels
            int index = 0;
            foreach (var kvp in dronePanels)
            {
                int row = index / columns;
                int col = index % columns;

                float xPos = col * (panelSize.x + panelSpacing);
                float yPos = -row * (panelSize.y + panelSpacing);

                RectTransform rt = kvp.Value.GetComponent<RectTransform>();
                rt.sizeDelta = panelSize;
                rt.anchoredPosition = new Vector2(xPos, yPos);

                index++;
            }
        }

        private void Update()
        {
            // Update camera feeds
            foreach (var kvp in dronePanels)
            {
                DroneController drone = kvp.Key;
                GameObject panel = kvp.Value;

                if (drone != null && panel != null)
                {
                    RawImage rawImage = panel.GetComponentInChildren<RawImage>();
                    if (rawImage != null)
                    {
                        Camera droneCamera = drone.GetComponentInChildren<Camera>();
                        if (droneCamera != null)
                        {
                            // Ensure the camera is enabled
                            if (!droneCamera.enabled)
                            {
                                droneCamera.enabled = true;
                            }

                            // Update the render texture
                            if (droneCamera.targetTexture != null)
                            {
                                rawImage.texture = droneCamera.targetTexture;
                            }
                            else
                            {
                                // Create a new render texture if needed
                                RenderTexture renderTexture = new RenderTexture(640, 480, 24);
                                droneCamera.targetTexture = renderTexture;
                                rawImage.texture = renderTexture;
                            }
                        }
                    }
                }
            }
        }
    }
} 