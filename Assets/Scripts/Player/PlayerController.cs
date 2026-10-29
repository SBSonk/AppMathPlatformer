using System.Collections.Generic;
using Core;
using Helpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Player
{
    [RequireComponent(typeof(CustomCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 14f;
        [SerializeField] private float gravity = 32f;
        [SerializeField] private float fallThreshold = -12f;

        [Header("Collision Masks")]
        [SerializeField] private CollisionLayerMask solidMask = CollisionLayerMask.Ground | CollisionLayerMask.Obstacle;
        [SerializeField] private CollisionLayerMask deathZoneMask = CollisionLayerMask.DeathZone;

        private CustomCollider _collider;
        private Vector2 _velocity;
        private Vector2 _moveInput;
        private bool _isGrounded;

        public bool IsGrounded => _isGrounded;
        public Vector2 Velocity => _velocity;
        public Vector2 MoveInput => _moveInput;
        public CustomCollider Collider => _collider;

        public CollisionLayerMask DeathZoneMask
        {
            get => deathZoneMask;
            set => deathZoneMask = value;
        }

        public CollisionLayerMask FallMask
        {
            get => deathZoneMask;
            set => deathZoneMask = value;
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        public float JumpForce
        {
            get => jumpForce;
            set => jumpForce = value;
        }

        public float Gravity
        {
            get => gravity;
            set => gravity = value;
        }

        public float FallThreshold
        {
            get => fallThreshold;
            set => fallThreshold = value;
        }

        private void Awake()
        {
            _collider = GetComponent<CustomCollider>();
            if (_collider != null)
            {
                _collider.layer = CollisionLayer.Player;
                _collider.colliderType = ColliderType.Cube;
                _collider.layersToCheck = solidMask | deathZoneMask;
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

        private void OnCustomCollisionEnter(CustomCollider other)
        {
            if (other == null) return;
            if (other.layer == CollisionLayer.DeathZone || other.layer == CollisionLayer.FallCollider)
            {
                RestartGame();
            }
        }

        #region Input System SendMessages Handlers

        public void OnMove(InputValue value)
        {
            if (value != null)
            {
                _moveInput = value.Get<Vector2>();
            }
        }

        public void OnJump(InputValue value)
        {
            if (value != null && value.isPressed)
            {
                Jump();
            }
        }

        public void OnJump()
        {
            Jump();
        }

        #endregion

        private void Update()
        {
            _velocity.x = Mathf.Clamp(_moveInput.x, -1f, 1f) * moveSpeed;
            ApplyPhysics(Time.deltaTime);
            CheckFallThreshold();
        }

        public void ApplyPhysics(float dt)
        {
            if (dt <= 0f) return;

            // Apply gravity
            _velocity.y -= gravity * dt;

            // Step X movement and resolve collisions
            Vector2 pos = transform.position;
            float deltaX = _velocity.x * dt;
            pos.x += deltaX;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            ResolveHorizontalCollisions(deltaX);

            // Step Y movement and resolve collisions
            pos = transform.position;
            float deltaY = _velocity.y * dt;
            pos.y += deltaY;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            ResolveVerticalCollisions(deltaY);

            // Check if player is still grounded (e.g. walked off ledge)
            CheckGroundedStatus();
        }

        private void ResolveHorizontalCollisions(float deltaX)
        {
            if (_collider == null) return;

            var solids = CollisionHandler.GetColliders(solidMask);
            Vector3 playerHalf = _collider.HalfSize;

            for (int i = 0; i < solids.Count; i++)
            {
                var solid = solids[i];
                if (solid == null || !solid.isActiveAndEnabled || solid == _collider) continue;

                Vector3 playerPos = _collider.Position;
                Vector3 solidPos = solid.Position;
                Vector3 solidHalf = solid.HalfSize;

                // Check vertical overlap: player must meaningfully overlap vertically with the solid to be horizontally blocked
                float verticalOverlap = Mathf.Min(playerPos.y + playerHalf.y, solidPos.y + solidHalf.y) -
                                        Mathf.Max(playerPos.y - playerHalf.y, solidPos.y - solidHalf.y);

                // If player is on top of or below the solid (touching surface), ignore horizontal blocking
                if (verticalOverlap <= 0.001f) continue;

                // Check horizontal overlap
                float horizontalOverlap = Mathf.Min(playerPos.x + playerHalf.x, solidPos.x + solidHalf.x) -
                                          Mathf.Max(playerPos.x - playerHalf.x, solidPos.x - solidHalf.x);

                if (horizontalOverlap > 0.0001f)
                {
                    if (deltaX > 0f || (deltaX == 0f && playerPos.x < solidPos.x))
                    {
                        // Moving right or approaching from left -> push to left edge of solid
                        float targetX = solidPos.x - solidHalf.x - playerHalf.x - _collider.offset.x;
                        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
                    }
                    else if (deltaX < 0f || (deltaX == 0f && playerPos.x > solidPos.x))
                    {
                        // Moving left or approaching from right -> push to right edge of solid
                        float targetX = solidPos.x + solidHalf.x + playerHalf.x - _collider.offset.x;
                        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
                    }
                    _velocity.x = 0f;
                }
            }
        }

        private void ResolveVerticalCollisions(float deltaY)
        {
            if (_collider == null) return;

            var solids = CollisionHandler.GetColliders(solidMask);
            Vector3 playerHalf = _collider.HalfSize;

            for (int i = 0; i < solids.Count; i++)
            {
                var solid = solids[i];
                if (solid == null || !solid.isActiveAndEnabled || solid == _collider) continue;

                Vector3 playerPos = _collider.Position;
                Vector3 solidPos = solid.Position;
                Vector3 solidHalf = solid.HalfSize;

                // Check horizontal overlap: player must overlap horizontally with the solid to collide vertically
                float horizontalOverlap = Mathf.Min(playerPos.x + playerHalf.x, solidPos.x + solidHalf.x) -
                                          Mathf.Max(playerPos.x - playerHalf.x, solidPos.x - solidHalf.x);

                if (horizontalOverlap <= 0.001f) continue;

                // Check vertical overlap
                float verticalOverlap = Mathf.Min(playerPos.y + playerHalf.y, solidPos.y + solidHalf.y) -
                                        Mathf.Max(playerPos.y - playerHalf.y, solidPos.y - solidHalf.y);

                if (verticalOverlap > 0.0001f)
                {
                    if (deltaY <= 0f || playerPos.y > solidPos.y)
                    {
                        // Falling onto top of solid / ground
                        float targetY = solidPos.y + solidHalf.y + playerHalf.y - _collider.offset.y;
                        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                        _velocity.y = 0f;
                        _isGrounded = true;
                    }
                    else
                    {
                        // Moving upward and hit head / bottom of solid
                        float targetY = solidPos.y - solidHalf.y - playerHalf.y - _collider.offset.y;
                        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                        _velocity.y = 0f;
                    }
                }
            }
        }

        private void CheckGroundedStatus()
        {
            if (_collider == null) return;

            // Probe slightly below player's feet
            Vector3 playerPos = _collider.Position;
            Vector3 playerHalf = _collider.HalfSize;
            Vector3 probeCenter = new Vector3(playerPos.x, playerPos.y - playerHalf.y - 0.05f, playerPos.z);
            Vector3 probeHalfSize = new Vector3(playerHalf.x * 0.9f, 0.05f, playerHalf.z > 0f ? playerHalf.z : 1f);

            bool groundBelow = CollisionHandler.OverlapBox(probeCenter, probeHalfSize, solidMask);

            if (!groundBelow && _velocity.y > 0.01f)
            {
                _isGrounded = false;
            }
            else if (!groundBelow && _isGrounded && _velocity.y <= 0f)
            {
                _isGrounded = false;
            }
        }

        private void CheckFallThreshold()
        {
            if (transform.position.y < fallThreshold)
            {
                RestartGame();
            }
        }

        public void RestartGame()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
        }

        // Methods for testing and external driving
        public void SetGrounded(bool grounded) => _isGrounded = grounded;
        public void SetVelocity(Vector2 velocity) => _velocity = velocity;
        public void SetHorizontalInput(float horizontal)
        {
            _moveInput.x = horizontal;
            _velocity.x = Mathf.Clamp(horizontal, -1f, 1f) * moveSpeed;
        }

        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
            _velocity.x = Mathf.Clamp(moveInput.x, -1f, 1f) * moveSpeed;
        }

        public bool Jump() => TryJump();

        public bool TryJump()
        {
            if (_isGrounded)
            {
                _velocity.y = jumpForce;
                _isGrounded = false;
                return true;
            }
            return false;
        }
    }
}
