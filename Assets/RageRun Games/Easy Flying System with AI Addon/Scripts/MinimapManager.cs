using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RageRunGames.EasyFlyingSystem;
using Unity.Netcode;

public class MinimapManager : MonoBehaviour
{
    [Header("Minimap Settings")]
    [SerializeField] private RectTransform minimapRect;
    [SerializeField] private GameObject droneIconPrefab;
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f); // Size of your map in world units
    [SerializeField] private float updateInterval = 0.1f;

    [Header("Icon Settings")]
    [SerializeField] private Color hostDroneColor = Color.green;
    [SerializeField] private Color clientDroneColor = Color.blue;
    [SerializeField] private float iconSize = 20f;

    private Dictionary<ulong, RectTransform> droneIcons = new Dictionary<ulong, RectTransform>();
    private DroneManager droneManager;
    private float updateTimer;

    private void Start()
    {
        droneManager = FindObjectOfType<DroneManager>();
        if (droneManager == null)
        {
            Debug.LogError("DroneManager not found in scene!");
            return;
        }

        // Subscribe to drone events
        droneManager.OnDroneAdded += OnDroneAdded;
        droneManager.OnDroneRemoved += OnDroneRemoved;

        // Initialize existing drones
        foreach (var drone in droneManager.GetAllDrones())
        {
            OnDroneAdded(drone);
        }
    }

    private void OnDestroy()
    {
        if (droneManager != null)
        {
            droneManager.OnDroneAdded -= OnDroneAdded;
            droneManager.OnDroneRemoved -= OnDroneRemoved;
        }
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateDronePositions();
            updateTimer = 0f;
        }
    }

    private void OnDroneAdded(DroneController drone)
    {
        // Get the NetworkObject component
        NetworkObject networkObject = drone.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"NetworkObject component not found on drone {drone.name}");
            return;
        }

        ulong networkId = networkObject.NetworkObjectId;
        if (droneIcons.ContainsKey(networkId)) return;

        // Create new drone icon
        GameObject icon = Instantiate(droneIconPrefab, minimapRect);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        
        // Set icon properties
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        
        // Set color based on ownership
        Image iconImage = icon.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = networkObject.IsOwner ? hostDroneColor : clientDroneColor;
        }

        droneIcons[networkId] = iconRect;
    }

    private void OnDroneRemoved(DroneController drone)
    {
        NetworkObject networkObject = drone.GetComponent<NetworkObject>();
        if (networkObject == null) return;

        ulong networkId = networkObject.NetworkObjectId;
        if (droneIcons.TryGetValue(networkId, out RectTransform icon))
        {
            Destroy(icon.gameObject);
            droneIcons.Remove(networkId);
        }
    }

    private void UpdateDronePositions()
    {
        foreach (var drone in droneManager.GetAllDrones())
        {
            NetworkObject networkObject = drone.GetComponent<NetworkObject>();
            if (networkObject == null) continue;

            ulong networkId = networkObject.NetworkObjectId;
            if (droneIcons.TryGetValue(networkId, out RectTransform icon))
            {
                // Convert world position to minimap position
                Vector2 normalizedPosition = new Vector2(
                    (drone.transform.position.x + mapSize.x / 2) / mapSize.x,
                    (drone.transform.position.z + mapSize.y / 2) / mapSize.y
                );

                // Update icon position
                icon.anchoredPosition = new Vector2(
                    (normalizedPosition.x - 0.5f) * minimapRect.rect.width,
                    (normalizedPosition.y - 0.5f) * minimapRect.rect.height
                );

                // Update icon rotation
                icon.localRotation = Quaternion.Euler(0, 0, -drone.transform.eulerAngles.y);
            }
        }
    }
} 