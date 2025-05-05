using UnityEngine;

namespace RageRunGames.EasyFlyingSystem
{
    public abstract class BaseInputHandler : MonoBehaviour, IInputHandler
    {
        public float Pitch { get; protected set; }
        
        public float Roll { get; protected set; }
        
        public float Yaw { get; protected set; }
        public float Lift { get; protected set; }
        
        public bool checkInputs { get; protected set; }

        protected virtual void Update()
        {
            UpdateInputs();
        }

        public virtual void UpdateInputs()
        {
            // Base implementation does nothing
            // Override this in derived classes
        }

        public virtual void HandleInputs()
        {
            // Base implementation does nothing
            // Override this in derived classes
        }

        public void EvaluateAnyKeyDown()
        {
            checkInputs = Mathf.Abs(Pitch) <= 0.05f && Mathf.Abs(Lift) <= 0.05f && Mathf.Abs(Yaw) <= 0.05f && Mathf.Abs(Roll) <= 0.05f;
        }
    }
}