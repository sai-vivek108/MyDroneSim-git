using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Serialization;


namespace RageRunGames.EasyFlyingSystem
{
    public class AIInputHandler : BaseInputHandler, IInputHandler
    {
        public enum AIType
        {
            Follow,
            Wander,
            Waypoint,
            WaypointRandom,
            WaypointPingPong
        }

        [SerializeField] private AIType aiType = AIType.Waypoint;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float lerpSpeed = 5f;
        //[SerializeField] private float releaseLerpSpeed = 10f;
        [SerializeField] private float stopDistance = 1f;

        [Header("Wander Settings")] [SerializeField]
        private float wanderRadius = 5f;

        [SerializeField] private float wanderTimer = 3f;
        private float wanderElapsedTime;

        [Header("Patrol Settings")] [SerializeField]
        private Transform wayPointHolder;

        [Header("Obstacle Avoidance")] [SerializeField]
        private LayerMask obstacleLayer;

        [SerializeField] private float obstacleDetectionDistance = 5f;
        [SerializeField] private int numberOfRays = 5;
        [SerializeField] private float raySpreadAngle = 45f;
        [SerializeField] private bool useLocalForwardRay;

        [SerializeField] bool usePIDController;

        private int currentWaypointIndex = 0;
        private bool pingPongDirection = true;
        private Vector3 wanderTarget;

        private PIDController rollPID = new PIDController();
        private PIDController pitchPID = new PIDController();
        private PIDController yawPID = new PIDController();
        private PIDController liftPID = new PIDController();

        private void Start()
        {
            wanderElapsedTime = wanderTimer;
            GameObject wayPointObject = GameObject.Find("Waypoints (01)");  // Make sure to name the object properly in Unity
            if (wayPointObject != null)
            {
                wayPointHolder = wayPointObject != null ? wayPointObject.transform : null;
                //    Debug.Log("waypointholder is : "+wayPointHolder.childCount);
            }
            if (wayPointHolder.childCount == 0)
            {
                Debug.LogWarning("No patrol points assigned, disabling AI");
                enabled = false;
            }
        }

        public void HandleInputs()
        {
            Vector3 avoidanceSteering = DetectObstacles();

            switch (aiType)
            {
                case AIType.Follow:
                    HandleFollow(avoidanceSteering);
                    break;
                case AIType.Wander:
                    HandleWander(avoidanceSteering);
                    break;
                case AIType.Waypoint:
                    HandlePatrol(avoidanceSteering);
                    break;
                case AIType.WaypointRandom:
                    HandleWaypointRandom(avoidanceSteering);
                    break;
                case AIType.WaypointPingPong:
                    HandleWaypointPingPong(avoidanceSteering);
                    break;
            }

            EvaluateAnyKeyDown();
        }

        private Vector3 DetectObstacles()
        {
            Vector3 avoidance = Vector3.zero;
            float angleIncrement = raySpreadAngle / (numberOfRays - 1);

            for (int i = 0; i < numberOfRays; i++)
            {
                float angle = -raySpreadAngle / 2 + angleIncrement * i;

                Vector3 rayDir = Quaternion.Euler(0, angle, 0) * transform.forward;

                if (!useLocalForwardRay)
                {
                    rayDir.y = 0f;
                }

                Ray ray = new Ray(transform.position, rayDir);
                if (Physics.SphereCast(ray, 0.5f, out RaycastHit hit, obstacleDetectionDistance, obstacleLayer))
                {
                    // Vector3 awayFromObstacle = (transform.position - hit.point).normalized  +
                    //                          Vector3.Reflect(rayDir, hit.normal);

                    Vector3 hitDirection = (transform.position - hit.point).normalized;

                    if (useLocalForwardRay)
                    {
                        hitDirection += Vector3.Reflect(rayDir, hit.normal);
                    }

                    float distanceFactor = (obstacleDetectionDistance - hit.distance) / obstacleDetectionDistance;


                    if (useLocalForwardRay)
                    {
                        avoidance += hitDirection * distanceFactor;
                    }
                    else
                    {
                        float side = Vector3.SignedAngle(transform.forward, hitDirection, Vector3.up);

                        var reflect = Vector3.zero;

                        if (side > 0)
                        {
                            Debug.Log(" Right");
                            reflect = transform.right;
                        }
                        else
                        {
                            Debug.Log(" Left");
                            reflect = -transform.right;
                        }

                        avoidance += reflect * distanceFactor;
                    }

                    avoidance.y = 0f;
                    Debug.DrawRay(transform.position, rayDir * hit.distance, Color.red); // Red ray to the hit point
                    Debug.DrawRay(transform.position, avoidance.normalized * obstacleDetectionDistance,
                        Color.yellow); // Yellow ray for avoidance direction
                }
                else
                {
                    Debug.DrawRay(transform.position, rayDir * obstacleDetectionDistance, Color.green);
                }
            }

            return avoidance;
        }

        private void HandleFollow(Vector3 avoidanceSteering)
        {
            Vector3 targetPosition = targetTransform.position;
            MoveTowardsTarget(targetPosition, avoidanceSteering);
        }

        private void HandleWander(Vector3 avoidanceSteering)
        {
            wanderElapsedTime += Time.deltaTime;

            if (wanderElapsedTime >= wanderTimer)
            {
                Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderRadius;
                randomDirection += transform.position;
                wanderTarget = randomDirection;
                wanderElapsedTime = 0;
            }

            MoveTowardsTarget(wanderTarget, avoidanceSteering);
        }

        private void HandlePatrol(Vector3 avoidanceSteering)
        {
            if (wayPointHolder.childCount == 0) return;

            Vector3 targetPosition = wayPointHolder.GetChild(currentWaypointIndex).position;
            MoveTowardsTarget(targetPosition, avoidanceSteering);

            if (Vector3.Distance(transform.position, targetPosition) < stopDistance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % wayPointHolder.childCount;
            }
        }

        private void HandleWaypointRandom(Vector3 avoidanceSteering)
        {
            if (wayPointHolder.childCount == 0) return;

            Vector3 targetPosition = wayPointHolder.GetChild(currentWaypointIndex).position;
            MoveTowardsTarget(targetPosition, avoidanceSteering);

            if (Vector3.Distance(transform.position, targetPosition) < stopDistance)
            {
                currentWaypointIndex = UnityEngine.Random.Range(0, wayPointHolder.childCount);
            }
        }

        private void HandleWaypointPingPong(Vector3 avoidanceSteering)
        {
            if (wayPointHolder.childCount == 0) return;

            Vector3 targetPosition = wayPointHolder.GetChild(currentWaypointIndex).position;
            MoveTowardsTarget(targetPosition, avoidanceSteering);

            if (Vector3.Distance(transform.position, targetPosition) < stopDistance)
            {
                if (pingPongDirection)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= wayPointHolder.childCount)
                    {
                        currentWaypointIndex = wayPointHolder.childCount - 2;
                        pingPongDirection = false;
                    }
                }
                else
                {
                    currentWaypointIndex--;
                    if (currentWaypointIndex < 0)
                    {
                        currentWaypointIndex = 1;
                        pingPongDirection = true;
                    }
                }
            }
        }

        private void MoveTowardsTarget(Vector3 targetPosition, Vector3 avoidanceSteering)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized + avoidanceSteering;

            float desiredRoll = Vector3.Dot(transform.right, directionToTarget);
            float desiredPitch = Vector3.Dot(transform.forward, directionToTarget);
            float desiredLift = directionToTarget.y;
            float desiredYaw = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up) / 360f * 2f;

            if (usePIDController)
            {
                desiredRoll = rollPID.Seek(desiredRoll, Roll);
                desiredPitch = pitchPID.Seek(desiredPitch, Pitch);
                desiredLift = liftPID.Seek(desiredLift, Lift);
                desiredYaw = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up) / 360f;
            }

            desiredRoll = Mathf.Clamp(desiredRoll, -1f, 1f);
            desiredPitch = Mathf.Clamp(desiredPitch, -1f, 1f);
            desiredLift = Mathf.Clamp(desiredLift, -1f, 1f);
            desiredYaw = Mathf.Clamp(desiredYaw, -1f, 1f);

            Roll = Mathf.Lerp(Roll, desiredRoll, lerpSpeed * Time.deltaTime);
            Pitch = Mathf.Lerp(Pitch, desiredPitch, lerpSpeed * Time.deltaTime);
            Yaw = Mathf.Lerp(Yaw, desiredYaw, lerpSpeed * Time.deltaTime);
            Lift = Mathf.Lerp(Lift, desiredLift, lerpSpeed * Time.deltaTime);
        }


        private void OnDrawGizmos()
        {
            if (wayPointHolder != null)
            {
                Gizmos.color = Color.cyan;

                for (int i = 0; i < wayPointHolder.childCount; i++)
                {
                    Gizmos.DrawSphere(wayPointHolder.GetChild(i).position, 0.5f);
                    Gizmos.DrawLine(wayPointHolder.GetChild(i).position,
                        wayPointHolder.GetChild((i + 1) % wayPointHolder.childCount).position);
                }
            }
        }
    }

    [System.Serializable]
    public class PIDController
    {
        public float pCoeff = .8f;
        public float iCoeff = .0002f;
        public float dCoeff = .2f;
        public float minimum = -1;
        public float maximum = 1;

        float integral;
        float lastProportional;

        public float Seek(float seekValue, float currentValue)
        {
            float deltaTime = Time.fixedDeltaTime;
            float proportional = seekValue - currentValue;

            float derivative = (proportional - lastProportional) / deltaTime;
            integral += proportional * deltaTime;
            lastProportional = proportional;

            float value = pCoeff * proportional + iCoeff * integral + dCoeff * derivative;
            value = Mathf.Clamp(value, minimum, maximum);

            return value;
        }
    }
}