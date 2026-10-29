using System;
using Core;
using Helpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Player
{
    public class PlayerController : CustomCollider
    {
        [Header("Movement Settings")]
        [SerializeField] float moveSpeed = 8f;
        [SerializeField] float jumpForce = 14f;
        [SerializeField] float gravity = 32f;

        Vector2 _velocity;
        Vector2 _moveInput;

        public Vector2 Velocity => _velocity;
        public Vector2 MoveInput => _moveInput;

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

        #region Input System SendMessages Handlers

        public void OnMove(InputValue value)
        {
            if (value != null)
            {
                SetMoveInput(value.Get<Vector2>());
            }
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                Jump();
            }
        }

        public void OnJump() => Jump();

        #endregion

        void Update()
        {
            _velocity.x = Mathf.Clamp(_moveInput.x, -1f, 1f) * moveSpeed;
        }

        private void FixedUpdate()
        {
            ApplyPhysics();
        }

        public void ApplyPhysics()
        {
            _velocity.y -= gravity * Time.fixedDeltaTime;

            Vector2 pos = transform.position;
            float deltaX = _velocity.x * Time.fixedDeltaTime;
            float deltaY = _velocity.y * Time.fixedDeltaTime;
            pos.x += deltaX;
            pos.y += deltaY;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            HandleCollisions(deltaX, deltaY);
        }

        void HandleCollisions(float deltaX, float deltaY)
        {
            var solids = CollisionHandler.GetColliders(layersToCheck);
            Vector3 playerHalf = HalfSize;

            for (int i = 0; i < solids.Count; i++)
            {
                var solid = solids[i];
                if (solid == null || !solid.isActiveAndEnabled || solid == this || solid.layer == CollisionLayer.DeathZone) continue;

                Vector3 playerPos = Position;
                Vector3 solidPos = solid.Position;
                Vector3 solidHalf = solid.HalfSize;

                float horizontalOverlap = Mathf.Min(playerPos.x + playerHalf.x, solidPos.x + solidHalf.x) -
                                          Mathf.Max(playerPos.x - playerHalf.x, solidPos.x - solidHalf.x);

                if (horizontalOverlap <= 0.0001f) continue;

                float verticalOverlap = Mathf.Min(playerPos.y + playerHalf.y, solidPos.y + solidHalf.y) -
                                        Mathf.Max(playerPos.y - playerHalf.y, solidPos.y - solidHalf.y);

                if (verticalOverlap <= 0.0001f) continue;

                if (horizontalOverlap < verticalOverlap)
                {
                    if (deltaX > 0f || (deltaX == 0f && playerPos.x < solidPos.x))
                    {
                        float targetX = solidPos.x - solidHalf.x - playerHalf.x - offset.x;
                        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
                    }
                    else if (deltaX < 0f || (deltaX == 0f && playerPos.x > solidPos.x))
                    {
                        float targetX = solidPos.x + solidHalf.x + playerHalf.x - offset.x;
                        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
                    }
                    _velocity.x = 0f;
                }
                else
                {
                    if (deltaY <= 0f || playerPos.y > solidPos.y)
                    {
                        float targetY = solidPos.y + solidHalf.y + playerHalf.y - offset.y;
                        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                        _velocity.y = 0f;
                    }
                    else
                    {
                        float targetY = solidPos.y - solidHalf.y - playerHalf.y - offset.y;
                        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                        _velocity.y = 0f;
                    }
                }
            }
        }

        public bool IsGrounded()
        {
            Vector3 playerPos = Position;
            Vector3 playerHalf = HalfSize;
            Vector3 probeCenter = new Vector3(playerPos.x, playerPos.y - playerHalf.y - 0.05f, playerPos.z);
            Vector3 probeHalfSize = new Vector3(playerHalf.x * 0.9f, 0.05f, playerHalf.z > 0f ? playerHalf.z : 1f);

            var candidates = CollisionHandler.GetColliders(layersToCheck);
            
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate == this || candidate.layer == CollisionLayer.DeathZone) continue;

                if (candidate.colliderType == ColliderType.Sphere)
                {
                    if (CollisionHelper.BoxToSphere(probeCenter, probeHalfSize, candidate.Position, candidate.radius))
                    {
                        return true;
                    }
                }
                else
                {
                    if (CollisionHelper.BoxToBox(probeCenter, probeHalfSize, candidate.Position, candidate.HalfSize))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void SetVelocity(Vector2 velocity) => _velocity = velocity;

        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
            _velocity.x = Mathf.Clamp(moveInput.x, -1f, 1f) * moveSpeed;
        }

        public bool Jump()
        {
            if (IsGrounded())
            {
                _velocity.y = jumpForce;
                return true;
            }
            return false;
        }
    }
}