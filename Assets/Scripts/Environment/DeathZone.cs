using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Environment
{
    [RequireComponent(typeof(CustomCollider))]
    public class DeathZone : MonoBehaviour
    {
        private CustomCollider _collider;

        public CustomCollider Collider => _collider;

        private void Awake()
        {
            _collider = GetComponent<CustomCollider>();
            if (_collider != null)
            {
                _collider.layer = CollisionLayer.DeathZone;
                _collider.colliderType = ColliderType.Cube;
                _collider.layersToCheck = CollisionLayerMask.Player;
            }
        }

        private void Start()
        {
            if (_collider != null)
            {
                _collider.CollisionEnter += OnCustomCollisionEnter;
            }
        }

        private void OnDestroy()
        {
            if (_collider != null)
            {
                _collider.CollisionEnter -= OnCustomCollisionEnter;
            }
        }

        private void Reset()
        {
            _collider = GetComponent<CustomCollider>();
            if (_collider != null)
            {
                _collider.layer = CollisionLayer.DeathZone;
                _collider.colliderType = ColliderType.Cube;
                _collider.layersToCheck = CollisionLayerMask.Player;
            }
        }

        private void OnCustomCollisionEnter(CustomCollider other)
        {
            if (other == null) return;
            if (other.layer == CollisionLayer.Player)
            {
                RestartGame();
            }
        }

        public static void RestartGame()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
        }
    }
}
