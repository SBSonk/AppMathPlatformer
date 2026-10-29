using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public enum CollisionLayer
    {
        Player,
        Enemy,
        Projectile,
        Environment,
        Pickup,
        Obstacle,
        DeathZone,
    }

    [Flags]
    public enum CollisionLayerMask
    {
        None = 0,
        Player = 1 << 0,
        Enemy = 1 << 1,
        Projectile = 1 << 2,
        Environment = 1 << 3,
        Pickup = 1 << 4,
        Obstacle = 1 << 5,
        DeathZone = 1 << 6,
        All = ~0
    }

    public enum ColliderType
    {
        Sphere = 0,
        Cube = 1
    }

    public class CollisionHandler : MonoBehaviour
    {
        public static CollisionHandler Instance { get; set; }

        static readonly List<CustomCollider> AllColliders = new List<CustomCollider>();
        static List<CustomCollider>[] _layerGroups;

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

            if (!AllColliders.Contains(collider))
            {
                AllColliders.Add(collider);
            }

            int layerIndex = (int)collider.layer;
            if (layerIndex >= 0 && layerIndex < _layerGroups.Length)
            {
                if (!_layerGroups[layerIndex].Contains(collider))
                {
                    _layerGroups[layerIndex].Add(collider);
                }
            }
        }

        public static void Unregister(CustomCollider collider)
        {
            if (collider == null) return;
            EnsureLayerGroupsInitialized();

            AllColliders.Remove(collider);

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
            return AllColliders;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            EnsureLayerGroupsInitialized();
        }

        void Update()
        {
            PerformCollisionChecks();
        }

        public static void PerformCollisionChecks()
        {
            EnsureLayerGroupsInitialized();

            int count = AllColliders.Count;
            for (int i = 0; i < count; i++)
            {
                CustomCollider col = AllColliders[i];
                if (col != null)
                {
                    col.BeginCollisionFrame();
                }
            }

            // Iterate over all unique pairs (i, j) with i < j
            for (int i = 0; i < count; i++)
            {
                CustomCollider colA = AllColliders[i];
                if (colA == null || !colA.isActiveAndEnabled) continue;

                for (int j = i + 1; j < count; j++)
                {
                    CustomCollider colB = AllColliders[j];
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

            // Trigger col events
            for (int i = 0; i < count; i++)
            {
                CustomCollider col = AllColliders[i];
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
