using System;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class SeaVoyageMotor : MonoBehaviour
    {
        [SerializeField] private Transform fleetSpawnRoot;
        [SerializeField] private Transform followingCamera;
        [SerializeField] private Transform backgroundRoot;
        [SerializeField] private Transform entryRoot;
        private Vector3 fleetOrigin;
        private Vector3 cameraOrigin;
        private Vector3 backgroundOrigin;
        private Vector3 entryOrigin;
        public float Distance { get; private set; }

        private void Awake()
        {
            if (fleetSpawnRoot == null || followingCamera == null || backgroundRoot == null || entryRoot == null)
                throw new InvalidOperationException("SeaVoyageMotor requires fleet, camera, background and entry roots.");
            fleetOrigin = fleetSpawnRoot.position;
            cameraOrigin = followingCamera.position;
            backgroundOrigin = backgroundRoot.position;
            entryOrigin = entryRoot.position;
        }

        public void SetDistance(float distance)
        {
            Distance = distance;
            Vector3 travel = Vector3.right * distance;
            fleetSpawnRoot.position = fleetOrigin + travel;
            followingCamera.position = cameraOrigin + travel;
            backgroundRoot.position = backgroundOrigin + travel;
            entryRoot.position = entryOrigin + travel;
        }

        private void OnDisable()
        {
            if (fleetSpawnRoot != null && followingCamera != null && backgroundRoot != null && entryRoot != null)
                SetDistance(0f);
        }
    }
}
