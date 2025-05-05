using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.AI;

namespace RageRunGames.EasyFlyingSystem
{
    public class AIInputHandler : BaseInputHandler, IInputHandler
    {
        public enum AIType
        {
            Follow,
            Wander,
            Waypoint,
            WaypointRandom
        }

        [SerializeField] private AIType aiType = AIType.Wander;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float lerpSpeed = 5f;
        [SerializeField] private float stopDistance = 1f;

        [Header("Wander Settings")] 
        [SerializeField] private float wanderRadius = 50f;
        [SerializeField] private float wanderTimer = 3f;
        private float wanderElapsedTime;

        [Header("Patrol Settings")] 
        [SerializeField] private Transform wayPointHolder;
        [SerializeField] private bool useNavMesh = false;
        [SerializeField] private float navMeshSampleDistance = 10f;

        [Header("Obstacle Avoidance")] 
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float obstacleDetectionDistance = 5f;
        [SerializeField] private int numberOfRays = 5;
        [SerializeField] private float raySpreadAngle = 45f;
        [SerializeField] private bool useLocalForwardRay;

        [SerializeField] private bool usePIDController;

        private int currentWaypointIndex = 0;
        private Vector3 wanderTarget;
        private NavMeshPath navMeshPath;
        private int currentPathIndex = 0;

        private PIDController rollPID = new PIDController();
        private PIDController pitchPID = new PIDController();
        private PIDController yawPID = new PIDController();
        private PIDController liftPID = new PIDController();

        [Header("AI Settings")]
        [SerializeField] private float inputSmoothness = 5f;
        [SerializeField] private float pitchSensitivity = 1f;
        [SerializeField] private float rollSensitivity = 1f;
        [SerializeField] private float liftSensitivity = 1f;
        [SerializeField] private float arrivalThreshold = 0.5f;
        [SerializeField] private float areaSize = 50f;
        [SerializeField] private float minHeight = 5f;
        [SerializeField] private float maxHeight = 20f;

        private Vector3 targetPosition;
        private float targetPitch;
        private float targetRoll;
        private float targetLift;
        private float targetYaw;
        private bool isMovingToTarget = false;
        private float timeSinceLastTargetChange = 0f;
        private float targetChangeInterval = 5f;

        private void Start()
        {
            wanderElapsedTime = wanderTimer;
            navMeshPath = new NavMeshPath();

            if (useNavMesh)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    Debug.LogWarning("AI cannot find NavMesh at starting position. Disabling NavMesh.");
                    useNavMesh = false;
                }
            }

            // Initialize with random target
            GenerateNewTarget();
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
                        var reflect = side > 0 ? transform.right : -transform.right;
                        avoidance += reflect * distanceFactor;
                    }

                    avoidance.y = 0f;
                    Debug.DrawRay(transform.position, rayDir * hit.distance, Color.red);
                    Debug.DrawRay(transform.position, avoidance.normalized * obstacleDetectionDistance, Color.yellow);
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
            if (targetTransform == null) return;

            Vector3 targetPos = targetTransform.position;
            if (useNavMesh)
            {
                if (NavMesh.CalculatePath(transform.position, targetPos, NavMesh.AllAreas, navMeshPath))
                {
                    if (navMeshPath.corners.Length > 1)
                    {
                        targetPos = navMeshPath.corners[1];
                    }
                }
            }
            MoveTowardsTarget(targetPos, avoidanceSteering);
        }

        private void HandleWander(Vector3 avoidanceSteering)
        {
            wanderElapsedTime += Time.deltaTime;

            if (wanderElapsedTime >= wanderTimer)
            {
                Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderRadius;
                randomDirection += transform.position;
                
                if (useNavMesh)
                {
                    if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                    {
                        wanderTarget = hit.position;
                    }
                }
                else
                {
                    wanderTarget = randomDirection;
                }
                
                wanderElapsedTime = 0;
            }

            MoveTowardsTarget(wanderTarget, avoidanceSteering);
        }

        private void HandlePatrol(Vector3 avoidanceSteering)
        {
            if (wayPointHolder == null || wayPointHolder.childCount == 0) return;

            Vector3 targetPos = wayPointHolder.GetChild(currentWaypointIndex).position;
            
            if (useNavMesh)
            {
                if (NavMesh.CalculatePath(transform.position, targetPos, NavMesh.AllAreas, navMeshPath))
                {
                    if (navMeshPath.corners.Length > 1)
                    {
                        targetPos = navMeshPath.corners[1];
                    }
                }
            }

            MoveTowardsTarget(targetPos, avoidanceSteering);

            if (Vector3.Distance(transform.position, targetPos) < stopDistance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % wayPointHolder.childCount;
            }
        }

        private void HandleWaypointRandom(Vector3 avoidanceSteering)
        {
            if (wayPointHolder == null || wayPointHolder.childCount == 0) return;

            Vector3 targetPos = wayPointHolder.GetChild(currentWaypointIndex).position;
            
            if (useNavMesh)
            {
                if (NavMesh.CalculatePath(transform.position, targetPos, NavMesh.AllAreas, navMeshPath))
                {
                    if (navMeshPath.corners.Length > 1)
                    {
                        targetPos = navMeshPath.corners[1];
                    }
                }
            }

            MoveTowardsTarget(targetPos, avoidanceSteering);

            if (Vector3.Distance(transform.position, targetPos) < stopDistance)
            {
                currentWaypointIndex = UnityEngine.Random.Range(0, wayPointHolder.childCount);
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

        public override void UpdateInputs()
        {
            if (!isMovingToTarget)
            {
                GenerateNewTarget();
                isMovingToTarget = true;
            }

            // Calculate direction to target
            Vector3 directionToTarget = targetPosition - transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            // Calculate target inputs based on direction
            targetPitch = Mathf.Clamp(directionToTarget.y * pitchSensitivity, -1f, 1f);
            targetRoll = Mathf.Clamp(directionToTarget.x * rollSensitivity, -1f, 1f);
            
            // Calculate lift based on height difference
            float heightDifference = targetPosition.y - transform.position.y;
            targetLift = Mathf.Clamp(heightDifference * liftSensitivity, -1f, 1f);

            // Smoothly interpolate current inputs to target values
            Pitch = Mathf.Lerp(Pitch, targetPitch, Time.deltaTime * inputSmoothness);
            Roll = Mathf.Lerp(Roll, targetRoll, Time.deltaTime * inputSmoothness);
            Lift = Mathf.Lerp(Lift, targetLift, Time.deltaTime * inputSmoothness);
            Yaw = Mathf.Lerp(Yaw, targetYaw, Time.deltaTime * inputSmoothness);

            // Check if we've arrived at the target
            if (distanceToTarget < arrivalThreshold)
            {
                isMovingToTarget = false;
            }

            // Periodically generate new targets
            timeSinceLastTargetChange += Time.deltaTime;
            if (timeSinceLastTargetChange >= targetChangeInterval)
            {
                GenerateNewTarget();
                timeSinceLastTargetChange = 0f;
            }

            EvaluateAnyKeyDown();
        }

        private void GenerateNewTarget()
        {
            // Generate random position within the defined area
            float randomX = UnityEngine.Random.Range(-areaSize, areaSize);
            float randomY = UnityEngine.Random.Range(minHeight, maxHeight);
            float randomZ = UnityEngine.Random.Range(-areaSize, areaSize);

            targetPosition = new Vector3(randomX, randomY, randomZ);
            Debug.Log($"AI: New target position set to {targetPosition}");
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

            if (isMovingToTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetPosition);
                Gizmos.DrawSphere(targetPosition, 1f);
            }

            if (useNavMesh && navMeshPath != null)
            {
                Gizmos.color = Color.magenta;
                for (int i = 0; i < navMeshPath.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(navMeshPath.corners[i], navMeshPath.corners[i + 1]);
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