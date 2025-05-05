using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.UI;

namespace RageRunGames.EasyFlyingSystem
{
    public class NetworkManagerUI : MonoBehaviour
    {
        [Header("Network Buttons")]
        [SerializeField] private Button serverBtn;
        [SerializeField] private Button clientBtn;
        [SerializeField] private Button hostBtn;

        private void Awake()
        {
            if (serverBtn == null || clientBtn == null || hostBtn == null)
            {
                Debug.LogError("Network buttons are not assigned in NetworkManagerUI!");
                return;
            }

            // Setup network buttons
            serverBtn.onClick.AddListener(StartServer);
            hostBtn.onClick.AddListener(StartHost);
            clientBtn.onClick.AddListener(StartClient);
        }

        private void StartServer()
        {
            if (!NetworkManager.Singleton.StartServer())
            {
                Debug.LogError("Failed to start server!");
            }
        }

        private void StartHost()
        {
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("Failed to start host!");
            }
        }

        private void StartClient()
        {
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("Failed to start client!");
            }
        }
    }
}
