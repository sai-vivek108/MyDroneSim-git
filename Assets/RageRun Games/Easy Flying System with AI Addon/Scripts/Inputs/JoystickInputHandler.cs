using UnityEngine;
using UnityEngine.Serialization;
using Unity.Netcode;

namespace RageRunGames.EasyFlyingSystem
{
    [DisallowMultipleComponent]
    public class JoystickInputHandler : BaseInputHandler, IInputHandler
    {
        [Header("Input Settings")]
        [SerializeField] private float inputSmoothness = 5f;
        [SerializeField] private float inputAcceleration = 2f;
        [SerializeField] private float inputDeceleration = 3f;
        [SerializeField] private float maxInputValue = 1f;
        [SerializeField] private float minInputValue = -1f;
        [SerializeField] private float deadZone = 0.1f;

        [Header("Rotor Response")]
        [SerializeField] private float rotorResponseDelay = 0.1f;
        [SerializeField] private float rotorSmoothness = 8f;
        [SerializeField] private float rotorAcceleration = 2f;
        [SerializeField] private float rotorDeceleration = 3f;

        private float currentPitch;
        private float currentRoll;
        private float currentYaw;
        private float currentLift;

        private float targetPitch;
        private float targetRoll;
        private float targetYaw;
        private float targetLift;

        private float rotorPitch;
        private float rotorRoll;
        private float rotorYaw;
        private float rotorLift;

        private void Start()
        {
            // Initialize current values
            currentPitch = 0f;
            currentRoll = 0f;
            currentYaw = 0f;
            currentLift = 0f;

            // Initialize rotor values
            rotorPitch = 0f;
            rotorRoll = 0f;
            rotorYaw = 0f;
            rotorLift = 0f;
        }

        public void HandleInputs()
        {
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) return;

            // Get raw input from joystick axes
            float rawPitch = Input.GetAxis("Vertical");
            float rawRoll = Input.GetAxis("Horizontal");
            float rawYaw = Input.GetAxis("Yaw");
            float rawLift = Input.GetAxis("Lift");

            // Apply deadzone
            rawPitch = ApplyDeadzone(rawPitch);
            rawRoll = ApplyDeadzone(rawRoll);
            rawYaw = ApplyDeadzone(rawYaw);
            rawLift = ApplyDeadzone(rawLift);

            // Calculate target values with acceleration/deceleration
            targetPitch = CalculateSmoothedInput(targetPitch, rawPitch);
            targetRoll = CalculateSmoothedInput(targetRoll, rawRoll);
            targetYaw = CalculateSmoothedInput(targetYaw, rawYaw);
            targetLift = CalculateSmoothedInput(targetLift, rawLift);

            // Smooth current values towards targets
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * inputSmoothness);
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * inputSmoothness);
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * inputSmoothness);
            currentLift = Mathf.Lerp(currentLift, targetLift, Time.deltaTime * inputSmoothness);

            // Apply rotor response delay and smoothing
            rotorPitch = CalculateRotorResponse(rotorPitch, currentPitch);
            rotorRoll = CalculateRotorResponse(rotorRoll, currentRoll);
            rotorYaw = CalculateRotorResponse(rotorYaw, currentYaw);
            rotorLift = CalculateRotorResponse(rotorLift, currentLift);

            // Set final values
            Pitch = rotorPitch;
            Roll = rotorRoll;
            Yaw = rotorYaw;
            Lift = rotorLift;

            EvaluateAnyKeyDown();
        }

        private float ApplyDeadzone(float value)
        {
            if (Mathf.Abs(value) < deadZone)
                return 0f;
            
            // Remap the value to account for deadzone
            float sign = Mathf.Sign(value);
            float normalizedValue = (Mathf.Abs(value) - deadZone) / (1f - deadZone);
            return sign * normalizedValue;
        }

        private float CalculateSmoothedInput(float current, float target)
        {
            float acceleration = Mathf.Abs(target) > 0.1f ? inputAcceleration : inputDeceleration;
            return Mathf.MoveTowards(current, target, Time.deltaTime * acceleration);
        }

        private float CalculateRotorResponse(float current, float target)
        {
            // Add slight delay to rotor response
            float delayedTarget = Mathf.Lerp(current, target, Time.deltaTime / rotorResponseDelay);
            
            // Apply rotor-specific smoothing
            float acceleration = Mathf.Abs(target) > 0.1f ? rotorAcceleration : rotorDeceleration;
            return Mathf.Lerp(current, delayedTarget, Time.deltaTime * rotorSmoothness * acceleration);
        }

        private void OnValidate()
        {
            // Ensure values are within valid ranges
            inputSmoothness = Mathf.Max(0.1f, inputSmoothness);
            inputAcceleration = Mathf.Max(0.1f, inputAcceleration);
            inputDeceleration = Mathf.Max(0.1f, inputDeceleration);
            deadZone = Mathf.Clamp(deadZone, 0f, 0.9f);
            rotorResponseDelay = Mathf.Max(0.01f, rotorResponseDelay);
            rotorSmoothness = Mathf.Max(0.1f, rotorSmoothness);
            rotorAcceleration = Mathf.Max(0.1f, rotorAcceleration);
            rotorDeceleration = Mathf.Max(0.1f, rotorDeceleration);
        }
    }
}
