using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

namespace RageRunGames.EasyFlyingSystem
{
    // Network-serializable message structure for drone communication
    [Serializable]
    public struct NetworkChatMessage : INetworkSerializable
    {
        public ulong SenderId;        // Network ID of the sending drone
        public string Content;        // Message content
        public long Ticks;            // Timestamp in ticks

        // Serialization method for network transmission
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SenderId);
            serializer.SerializeValue(ref Content);
            serializer.SerializeValue(ref Ticks);
        }

        // Convert ticks to DateTime
        public DateTime Timestamp => new DateTime(Ticks);
    }

    // Handles network communication between drones
    public class DroneCommunication : NetworkBehaviour
    {
        private DroneController droneController;    // Reference to the drone's controller
        private NetworkObject networkObject;        // Network component for multiplayer

        // Event triggered when a message is received
        public event Action<ulong, string> OnMessageReceived;

        // Initialize component references
        private void Awake()
        {
            droneController = GetComponent<DroneController>();
            networkObject = GetComponent<NetworkObject>();
        }

        // Register drone with the manager when starting
        private void Start()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && DroneManager.Instance != null)
            {
                DroneManager.Instance.RegisterDrone(droneController);
            }
        }

        // Send a chat message from this drone
        public void SendChatMessage(string message)
        {
            if (IsOwner)
            {
                // Send to server
                SendMessageToAllServerRpc(message);
            }
        }

        // Server-side RPC to handle message broadcasting
        [ServerRpc(RequireOwnership = false)]
        private void SendMessageToAllServerRpc(string message, ServerRpcParams serverRpcParams = default)
        {
            ulong senderId = serverRpcParams.Receive.SenderClientId;

            NetworkChatMessage networkMessage = new NetworkChatMessage
            {
                SenderId = senderId,
                Content = message,
                Ticks = DateTime.UtcNow.Ticks
            };

            // Broadcast to all clients including sender
            BroadcastMessageClientRpc(networkMessage);
        }

        // Client-side RPC to receive and process broadcasted messages
        [ClientRpc]
        private void BroadcastMessageClientRpc(NetworkChatMessage message)
        {
            // Format and display the message
            string formattedMessage = FormatMessage(message.SenderId, message.Content);
            OnMessageReceived?.Invoke(message.SenderId, formattedMessage);
        }

        // Format message with appropriate styling and prefixes
        public string FormatMessage(ulong senderId, string rawContent)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string label = $"Drone {NetworkObjectId}";
            string prefix = "";
            string color = "#FFFFFF";

            // Apply different formatting based on message type
            if (rawContent.Contains("EMERGENCY"))
            {
                prefix = "⚠️ ";
                color = "#FF4C4C";
            }
            else if (rawContent.StartsWith("POSITION") || rawContent.Contains("|X:"))
            {
                prefix = "📍 ";
                color = "#FFD700";
            }
            else if (rawContent.StartsWith("STATUS"))
            {
                color = "#00BFFF";
            }

            return $"<color=#888>[{time}]</color> <color={color}>{prefix}{label}: {rawContent}</color>";
        }

        // Helper method to send emergency signal
        public void SendEmergencySignal() => SendChatMessage("EMERGENCY_SIGNAL");

        // Helper method to send position report
        public void SendPositionReport()
        {
            Vector3 pos = transform.position;
            string msg = $"POSITION|X:{pos.x:F2}|Y:{pos.y:F2}|Z:{pos.z:F2}";
            SendChatMessage(msg);
        }

        // Helper method to send status update
        public void SendStatus(string status)
        {
            SendChatMessage($"STATUS:{status}");
        }
    }
}
