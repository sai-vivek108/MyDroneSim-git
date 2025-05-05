using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace RageRunGames.EasyFlyingSystem
{
    public class DroneManager : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private DroneVideoPanel videoPanel;

        // Stores drones mapped by their owner client ID
        private Dictionary<ulong, DroneController> drones = new Dictionary<ulong, DroneController>();

        public static DroneManager Instance { get; private set; }
        public event System.Action<DroneController> OnDroneAdded;
        public event System.Action<DroneController> OnDroneRemoved;

        private void Awake()
        {
            // Ensure singleton instance
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            // Subscribe to network events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from network events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void Start()
        {
            // Auto-assign panel if missing
            if (videoPanel == null)
            {
                videoPanel = FindFirstObjectByType<DroneVideoPanel>();
            }

            // Register all existing drones in the scene
            DroneController[] allDrones = FindObjectsByType<DroneController>(FindObjectsSortMode.None);
            foreach (var drone in allDrones)
            {
                RegisterDrone(drone);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            // Check if drone already exists for this client
            DroneController[] allDrones = FindObjectsByType<DroneController>(FindObjectsSortMode.None);
            bool hasDrone = false;

            foreach (var drone in allDrones)
            {
                var netObj = drone.GetComponent<NetworkObject>();
                if (netObj != null && netObj.OwnerClientId == clientId)
                {
                    hasDrone = true;
                    if (!drones.ContainsKey(clientId))
                        RegisterDrone(drone);
                    break;
                }
            }

            // If no drone found, spawn a new one
            if (!hasDrone)
            {
                SpawnDroneForClient(clientId);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            // Clean up drone when client disconnects
            if (drones.TryGetValue(clientId, out var drone))
            {
                var netObj = drone.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Despawn();
                    Destroy(drone.gameObject);
                }

                drones.Remove(clientId);
                videoPanel?.RemoveDroneView(drone);
                OnDroneRemoved?.Invoke(drone);
            }
        }

        private void SpawnDroneForClient(ulong clientId)
        {
            if (!IsServer) return;

            // Grab prefab from NetworkManager config
            NetworkObject prefab = NetworkManager.Singleton.NetworkConfig?.Prefabs?.Prefabs[0]?.Prefab?.GetComponent<NetworkObject>();
            if (prefab == null)
            {
                Debug.LogWarning("Drone prefab not set in NetworkManager.");
                return;
            }

            Vector3 spawnPos = GetRandomSpawnPosition();
            var instance = Instantiate(prefab, spawnPos, Quaternion.identity);
            instance.SpawnWithOwnership(clientId);

            var drone = instance.GetComponent<DroneController>();
            if (drone != null)
            {
                RegisterDrone(drone);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float radius = 5f;
            Vector2 offset = Random.insideUnitCircle * radius;
            return new Vector3(offset.x, 1f, offset.y); // Spawn slightly above ground
        }

        public void RegisterDrone(DroneController drone)
        {
            if (drone == null) return;

            var netObj = drone.GetComponent<NetworkObject>();
            if (netObj == null) return;

            ulong clientId = netObj.OwnerClientId;
            if (drones.ContainsKey(clientId))
            {
                drones[clientId] = drone;
            }
            else
            {
                drones.Add(clientId, drone);
            }

            videoPanel?.AddDroneView(drone);
            OnDroneAdded?.Invoke(drone);

            // Register message listener
            var comm = drone.GetComponent<DroneCommunication>();
            if (comm != null)
            {
                comm.OnMessageReceived += HandleMessageReceived;
            }
        }

        public void UnregisterDrone(DroneController drone)
        {
            if (drone == null) return;

            var netObj = drone.GetComponent<NetworkObject>();
            if (netObj == null) return;

            ulong clientId = netObj.OwnerClientId;

            if (drones.ContainsKey(clientId))
            {
                drones.Remove(clientId);
                videoPanel?.RemoveDroneView(drone);
                OnDroneRemoved?.Invoke(drone);
            }
        }

        public DroneController GetDrone(ulong clientId)
        {
            drones.TryGetValue(clientId, out DroneController drone);
            return drone;
        }

        public IEnumerable<DroneController> GetAllDrones()
        {
            return drones.Values;
        }

        public void UpdateDroneStatus(ulong droneId, Vector3 position, Vector3 velocity, bool isGrounded)
        {
            foreach (var drone in drones.Values)
            {
                var netObj = drone.GetComponent<NetworkObject>();
                if (netObj != null && netObj.NetworkObjectId == droneId)
                {
                    drone.UpdateStatus(position, velocity, isGrounded);
                    return;
                }
            }
        }

        private void HandleMessageReceived(ulong senderId, string message)
        {
            Debug.Log($"[DroneManager] Received message from Drone {senderId}: {message}");
        }
    }
}
