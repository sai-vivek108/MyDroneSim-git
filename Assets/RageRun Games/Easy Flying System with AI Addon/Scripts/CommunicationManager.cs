using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RageRunGames.EasyFlyingSystem;
using Unity.Netcode;
using System.Linq;

// Manages the communication UI and message handling between drones
public class CommunicationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField messageInput;        // Input field for typing messages
    [SerializeField] private Button sendButton;                  // Button to send messages
    [SerializeField] private Button emergencyButton;            // Button to send emergency signals
    [SerializeField] private Button positionRequestButton;      // Button to request position updates
    [SerializeField] private TMP_Dropdown droneDropdown;        // Dropdown to select target drone
    [SerializeField] private TextMeshProUGUI communicationLog;  // Text area for message history
    [SerializeField] private ScrollRect communicationScroll;    // Scroll view for message history
    [SerializeField] private RectTransform logContent;          // Content container for messages

    [Header("Settings")]
    [SerializeField] private float scrollDelay = 0.1f;          // Delay before auto-scrolling
    [SerializeField] private int maxLogEntries = 50;            // Maximum number of messages to keep
    [SerializeField] private float logEntryHeight = 30f;        // Height of each log entry

    private List<string> logEntries = new List<string>();       // List of message entries
    private float lastScrollTime;                               // Time of last scroll operation
    private Dictionary<ulong, DroneCommunication> droneMap = new Dictionary<ulong, DroneCommunication>(); // Map of drone IDs to their communication components
    private ulong selectedDroneId = 0;                          // Currently selected drone ID
    private bool broadcastToAll = true;                         // Whether to broadcast to all drones

    // Initialize UI components and event handlers
    private void Start()
    {
        InitializeUIComponents();

        DroneManager.Instance.OnDroneAdded += OnDroneAdded;
        DroneManager.Instance.OnDroneRemoved += OnDroneRemoved;

        foreach (var drone in DroneManager.Instance.GetAllDrones())
        {
            OnDroneAdded(drone);
        }

        sendButton.onClick.AddListener(SendMessage);
        emergencyButton.onClick.AddListener(SendEmergency);
        positionRequestButton.onClick.AddListener(RequestPosition);
        droneDropdown.onValueChanged.AddListener(OnDroneDropdownChanged);
    }

    // Set up UI components and their properties
    private void InitializeUIComponents()
    {
        if (communicationLog == null)
            communicationLog = GetComponentInChildren<TextMeshProUGUI>();
        if (communicationScroll == null)
            communicationScroll = GetComponentInChildren<ScrollRect>();
        if (logContent == null && communicationScroll != null)
            logContent = communicationScroll.content;

        if (communicationScroll != null)
        {
            communicationScroll.vertical = true;
            communicationScroll.horizontal = false;
            communicationScroll.movementType = ScrollRect.MovementType.Clamped;
            communicationScroll.inertia = true;
            communicationScroll.decelerationRate = 0.135f;
            communicationScroll.scrollSensitivity = 20f;
        }

        if (logContent != null)
        {
            logContent.anchorMin = new Vector2(0, 0);
            logContent.anchorMax = new Vector2(1, 1);
            logContent.pivot = new Vector2(0.5f, 1f);
            logContent.sizeDelta = new Vector2(0, 0);
        }
    }

    // Handle new drone registration
    private void OnDroneAdded(DroneController drone)
    {
        var comm = drone.GetComponent<DroneCommunication>();
        if (comm == null) return;

        var id = drone.GetComponent<NetworkObject>().NetworkObjectId;
        droneMap[id] = comm;
        comm.OnMessageReceived += HandleIncomingMessage;

        UpdateDroneDropdown();
    }

    // Handle drone removal
    private void OnDroneRemoved(DroneController drone)
    {
        var id = drone.GetComponent<NetworkObject>().NetworkObjectId;
        if (droneMap.TryGetValue(id, out var comm))
        {
            comm.OnMessageReceived -= HandleIncomingMessage;
            droneMap.Remove(id);
        }

        UpdateDroneDropdown();
    }

    // Update the drone selection dropdown
    private void UpdateDroneDropdown()
    {
        droneDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Broadcast to All")
        };

        foreach (var kvp in droneMap)
        {
            options.Add(new TMP_Dropdown.OptionData($"Drone {kvp.Key}"));
        }

        droneDropdown.AddOptions(options);
        droneDropdown.value = 0;
        broadcastToAll = true;
    }

    // Handle dropdown selection change
    private void OnDroneDropdownChanged(int index)
    {
        if (index == 0)
        {
            broadcastToAll = true;
            AddLogEntry("Selected: Broadcast to All");
        }
        else
        {
            selectedDroneId = droneMap.Keys.ElementAt(index - 1);
            broadcastToAll = false;
            AddLogEntry($"Selected Drone {selectedDroneId}");
        }
    }

    // Send a message to selected drone(s)
    private void SendMessage()
    {
        string msg = messageInput.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        if (broadcastToAll)
        {
            foreach (var comm in droneMap.Values)
            {
                comm.SendChatMessage(msg);
            }
        }
        else if (droneMap.TryGetValue(selectedDroneId, out var comm))
        {
            comm.SendChatMessage(msg);
        }

        messageInput.text = string.Empty;
    }

    // Send emergency signal to selected drone(s)
    private void SendEmergency()
    {
        if (broadcastToAll)
        {
            foreach (var comm in droneMap.Values)
            {
                comm.SendEmergencySignal();
            }
        }
        else if (droneMap.TryGetValue(selectedDroneId, out var comm))
        {
            comm.SendEmergencySignal();
        }
    }

    // Request position update from selected drone(s)
    private void RequestPosition()
    {
        if (broadcastToAll)
        {
            foreach (var comm in droneMap.Values)
            {
                comm.SendPositionReport();
            }
        }
        else if (droneMap.TryGetValue(selectedDroneId, out var comm))
        {
            comm.SendPositionReport();
        }
    }

    // Handle incoming messages from drones
    private void HandleIncomingMessage(ulong senderId, string formattedMessage)
    {
        AddLogEntry(formattedMessage);
    }

    // Add a new message to the log
    private void AddLogEntry(string message)
    {
        if (logEntries.Count > 0 && logEntries[^1] == message) return;

        logEntries.Add(message);

        if (logEntries.Count > maxLogEntries)
            logEntries.RemoveAt(0);

        communicationLog.text = string.Join("\n", logEntries);
        UpdateContentSize();

        if (Time.time - lastScrollTime > scrollDelay)
        {
            StartCoroutine(ScrollToBottom());
            lastScrollTime = Time.time;
        }
    }

    // Update the size of the log content area
    private void UpdateContentSize()
    {
        if (logContent == null) return;

        float requiredHeight = logEntries.Count * logEntryHeight;
        float viewportHeight = communicationScroll.viewport.rect.height;
        requiredHeight = Mathf.Max(requiredHeight, viewportHeight);

        logContent.sizeDelta = new Vector2(0, requiredHeight);
    }

    // Scroll the log to the bottom
    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        communicationScroll.verticalNormalizedPosition = 0f;
    }

    // Add a new drone to the communication system
    public void AddDrone(DroneController drone)
    {
        if (drone == null || !drone.TryGetComponent<DroneCommunication>(out var comm))
        {
            Debug.LogError("Invalid drone or missing DroneCommunication component");
            return;
        }

        var networkObject = drone.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("Drone is missing NetworkObject component");
            return;
        }

        if (!droneMap.ContainsKey(networkObject.NetworkObjectId))
        {
            droneMap[networkObject.NetworkObjectId] = comm;
            comm.OnMessageReceived += HandleIncomingMessage;

            // Add to dropdown with sequential number
            int sequentialNumber = droneMap.Count;
            string displayName = $"Drone {sequentialNumber}";
            
            if (droneDropdown != null)
            {
                var option = new TMP_Dropdown.OptionData(displayName);
                droneDropdown.options.Add(option);
            }
        }
    }
}
