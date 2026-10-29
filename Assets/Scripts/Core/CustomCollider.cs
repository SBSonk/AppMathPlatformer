using System;
using System.Collections.Generic;
using Helpers;
using NaughtyAttributes;
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
        Ground,
        Obstacle,
        DeathZone,
        FallCollider = DeathZone
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
        Ground = 1 << 5,
        Obstacle = 1 << 6,
        DeathZone = 1 << 7,
        FallCollider = DeathZone,
        All = ~0
    }

    public enum ColliderType
    {
        Sphere = 0,
        Cube = 1
    }

    public class CustomCollider : MonoBehaviour
    {
        public static List<CustomCollider>[] LayerGroups => CollisionHandler.LayerGroups;

        [Header("Collision Settings")]
        public CollisionLayer layer = CollisionLayer.Enemy;
        public ColliderType colliderType = ColliderType.Sphere;

        [ShowIf("colliderType", ColliderType.Sphere)]
        public float radius = 0.5f;

        [ShowIf("colliderType", ColliderType.Cube)]
        public Vector3 size = Vector3.one;

        public Vector3 offset = Vector3.zero;
        public CollisionLayerMask layersToCheck;

        public event Action<CustomCollider> CollisionEnter;
        public event Action<CustomCollider> CollisionStay;
        public event Action<CustomCollider> CollisionExit;

        private readonly HashSet<CustomCollider> _currentFrameCollisions = new HashSet<CustomCollider>();
        private readonly HashSet<CustomCollider> _activeCollisions = new HashSet<CustomCollider>();
        private readonly List<CustomCollider> _exitBuffer = new List<CustomCollider>();

        public Vector3 Position => transform.position + offset;

        public Vector3 HalfSize
        {
            get
            {
                if (colliderType == ColliderType.Cube)
                {
                    float z = Mathf.Approximately(size.z, 0f) ? 1f : Mathf.Abs(size.z);
                    return new Vector3(Mathf.Abs(size.x) * 0.5f, Mathf.Abs(size.y) * 0.5f, z * 0.5f);
                }
                return Vector3.one * radius;
            }
        }

        public Bounds Bounds => new Bounds(Position, colliderType == ColliderType.Cube ? (Mathf.Approximately(size.z, 0f) ? new Vector3(size.x, size.y, 1f) : size) : Vector3.one * (radius * 2f));

        public IReadOnlyCollection<CustomCollider> ActiveCollisions => _activeCollisions;

        protected virtual void Awake()
        {
            CollisionHandler.EnsureLayerGroupsInitialized();
        }

        protected virtual void OnEnable()
        {
            CollisionHandler.Register(this);
        }

        protected virtual void OnDisable()
        {
            CollisionHandler.Unregister(this);
        }

        protected virtual void OnDestroy()
        {
            CollisionHandler.Unregister(this);
        }

        internal void BeginCollisionFrame()
        {
            _currentFrameCollisions.Clear();
        }

        internal void ProcessOverlap(CustomCollider other)
        {
            if (other == null) return;
            _currentFrameCollisions.Add(other);
            if (_activeCollisions.Add(other))
            {
                CollisionEnter?.Invoke(other);
            }
        }

        internal void EndCollisionFrame()
        {
            // Trigger Stay on all active collisions that occurred this frame
            foreach (var other in _currentFrameCollisions)
            {
                if (other != null && other.isActiveAndEnabled)
                {
                    CollisionStay?.Invoke(other);
                }
            }

            // Trigger Exit on collisions that ended
            if (_activeCollisions.Count > 0)
            {
                _exitBuffer.Clear();
                foreach (var other in _activeCollisions)
                {
                    if (other == null || !other.isActiveAndEnabled || !_currentFrameCollisions.Contains(other))
                    {
                        _exitBuffer.Add(other);
                    }
                }

                for (int i = 0; i < _exitBuffer.Count; i++)
                {
                    var other = _exitBuffer[i];
                    _activeCollisions.Remove(other);
                    if (other != null)
                    {
                        CollisionExit?.Invoke(other);
                    }
                }
            }
        }

        internal void HandleUnregistered()
        {
            if (_activeCollisions.Count > 0)
            {
                _exitBuffer.Clear();
                _exitBuffer.AddRange(_activeCollisions);
                _activeCollisions.Clear();

                for (int i = 0; i < _exitBuffer.Count; i++)
                {
                    var other = _exitBuffer[i];
                    if (other != null)
                    {
                        CollisionExit?.Invoke(other);
                    }
                }
            }
            _currentFrameCollisions.Clear();
            _exitBuffer.Clear();
        }

        public static bool CheckOverlap(CustomCollider a, CustomCollider b)
        {
            if (a == null || b == null) return false;

            bool aIsSphere = a.colliderType == ColliderType.Sphere;
            bool bIsSphere = b.colliderType == ColliderType.Sphere;

            if (aIsSphere && bIsSphere)
                return CollisionHelper.SphereToSphere(a.Position, a.radius, b.Position, b.radius);

            if (!aIsSphere && !bIsSphere)
                return CollisionHelper.BoxToBox(a.Position, a.HalfSize, b.Position, b.HalfSize);

            if (!aIsSphere && bIsSphere)
                return CollisionHelper.BoxToSphere(a.Position, a.HalfSize, b.Position, b.radius);

            return CollisionHelper.BoxToSphere(b.Position, b.HalfSize, a.Position, a.radius);
        }

        public static List<CustomCollider> GetColliders(CollisionLayerMask mask)
        {
            return CollisionHandler.GetColliders(mask);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 pos = Position;
            if (colliderType == ColliderType.Sphere)
            {
                Gizmos.DrawWireSphere(pos, radius);
            }
            else
            {
                Vector3 boxSize = Mathf.Approximately(size.z, 0f) ? new Vector3(size.x, size.y, 1f) : size;
                Gizmos.DrawWireCube(pos, boxSize); 
            }
        }
    }
}