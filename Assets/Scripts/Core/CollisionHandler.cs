using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class CollisionHandler : MonoBehaviour
    {
        private static CollisionHandler _instance;
        private static bool _isQuitting;

        public static CollisionHandler Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    _instance = FindFirstObjectByType<CollisionHandler>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[CollisionHandler]");
                        _instance = go.AddComponent<CollisionHandler>();
                    }
                }
                return _instance;
            }
        }

        private static readonly List<CustomCollider> _allColliders = new List<CustomCollider>();
        private static List<CustomCollider>[] _layerGroups;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            EnsureLayerGroupsInitialized();
        }

        public static void EnsureLayerGroupsInitialized()
        {
            if (_layerGroups == null)
            {
                int layerCount = Enum.GetValues(typeof(CollisionLayer)).Length;
                _layerGroups = new List<CustomCollider>[layerCount];
                for (int i = 0; i < layerCount; i++)
                {
                    _layerGroups[i] = new List<CustomCollider>();
                }
            }
        }

        public static List<CustomCollider>[] LayerGroups
        {
            get
            {
                EnsureLayerGroupsInitialized();
                return _layerGroups;
            }
        }

        public static void Register(CustomCollider collider)
        {
            if (collider == null) return;
            EnsureLayerGroupsInitialized();

            if (!_allColliders.Contains(collider))
            {
                _allColliders.Add(collider);
            }

            int layerIndex = (int)collider.layer;
            if (layerIndex >= 0 && layerIndex < _layerGroups.Length)
            {
                if (!_layerGroups[layerIndex].Contains(collider))
                {
                    _layerGroups[layerIndex].Add(collider);
                }
            }

            if (Application.isPlaying)
            {
                var inst = Instance;
            }
        }

        public static void Unregister(CustomCollider collider)
        {
            if (collider == null) return;
            EnsureLayerGroupsInitialized();

            _allColliders.Remove(collider);

            int layerIndex = (int)collider.layer;
            if (layerIndex >= 0 && layerIndex < _layerGroups.Length)
            {
                _layerGroups[layerIndex].Remove(collider);
            }

            collider.HandleUnregistered();
        }

        public static List<CustomCollider> GetColliders(CollisionLayerMask mask)
        {
            EnsureLayerGroupsInitialized();
            List<CustomCollider> colliders = new List<CustomCollider>();
            for (int i = 0; i < _layerGroups.Length; i++)
            {
                if (((int)mask & (1 << i)) != 0)
                {
                    colliders.AddRange(_layerGroups[i]);
                }
            }
            return colliders;
        }

        public static List<CustomCollider> GetAllColliders()
        {
            return _allColliders;
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            EnsureLayerGroupsInitialized();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void Update()
        {
            PerformCollisionChecks();
        }

        /// <summary>
        /// Centrally evaluates collisions between all registered colliders.
        /// Evaluates each unique pair at most once to eliminate redundant computations.
        /// </summary>
        public static void PerformCollisionChecks()
        {
            EnsureLayerGroupsInitialized();

            // Clear frame collisions for all active colliders
            int count = _allColliders.Count;
            for (int i = 0; i < count; i++)
            {
                CustomCollider col = _allColliders[i];
                if (col != null)
                {
                    col.BeginCollisionFrame();
                }
            }

            // Iterate over all unique pairs (i, j) with i < j
            for (int i = 0; i < count; i++)
            {
                CustomCollider colA = _allColliders[i];
                if (colA == null || !colA.isActiveAndEnabled) continue;

                for (int j = i + 1; j < count; j++)
                {
                    CustomCollider colB = _allColliders[j];
                    if (colB == null || !colB.isActiveAndEnabled) continue;

                    bool aChecksB = ((int)colA.layersToCheck & (1 << (int)colB.layer)) != 0;
                    bool bChecksA = ((int)colB.layersToCheck & (1 << (int)colA.layer)) != 0;

                    // If neither collider is interested in the other's layer, skip calculation
                    if (!aChecksB && !bChecksA) continue;

                    if (CustomCollider.CheckOverlap(colA, colB))
                    {
                        if (aChecksB)
                        {
                            colA.ProcessOverlap(colB);
                        }
                        if (bChecksA)
                        {
                            colB.ProcessOverlap(colA);
                        }
                    }
                }
            }

            // Dispatch Stay and Exit events
            for (int i = 0; i < count; i++)
            {
                CustomCollider col = _allColliders[i];
                if (col != null && col.isActiveAndEnabled)
                {
                    col.EndCollisionFrame();
                }
            }
        }

        public static bool OverlapBox(Vector3 center, Vector3 halfSize, CollisionLayerMask mask, List<CustomCollider> results = null)
        {
            results?.Clear();
            var candidates = GetColliders(mask);
            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                CustomCollider col = candidates[i];
                if (col == null || !col.isActiveAndEnabled) continue;

                bool overlap = false;
                if (col.colliderType == ColliderType.Sphere)
                {
                    overlap = Helpers.CollisionHelper.BoxToSphere(center, halfSize, col.Position, col.radius);
                }
                else
                {
                    overlap = Helpers.CollisionHelper.BoxToBox(center, halfSize, col.Position, col.HalfSize);
                }

                if (overlap)
                {
                    found = true;
                    results?.Add(col);
                }
            }
            return found;
        }

        public static bool OverlapSphere(Vector3 center, float radius, CollisionLayerMask mask, List<CustomCollider> results = null)
        {
            results?.Clear();
            var candidates = GetColliders(mask);
            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                CustomCollider col = candidates[i];
                if (col == null || !col.isActiveAndEnabled) continue;

                bool overlap = false;
                if (col.colliderType == ColliderType.Sphere)
                {
                    overlap = Helpers.CollisionHelper.SphereToSphere(center, radius, col.Position, col.radius);
                }
                else
                {
                    overlap = Helpers.CollisionHelper.BoxToSphere(col.Position, col.HalfSize, center, radius);
                }

                if (overlap)
                {
                    found = true;
                    results?.Add(col);
                }
            }
            return found;
        }
    }
}
