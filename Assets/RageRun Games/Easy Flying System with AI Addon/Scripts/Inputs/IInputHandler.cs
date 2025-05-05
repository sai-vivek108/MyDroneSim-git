using UnityEngine;

namespace RageRunGames.EasyFlyingSystem
{
    public interface IInputHandler
    {
        float Pitch { get; }
        float Roll { get; }
        float Yaw { get; }
        float Lift { get; }
        bool checkInputs { get; }

        void HandleInputs();
    }
}