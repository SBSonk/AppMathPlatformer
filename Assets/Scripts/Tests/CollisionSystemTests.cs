using System;
using System.Linq;
using Core;
using Environment;
using Helpers;
using Player;
using UnityEngine;

namespace Tests
{
    public static class CollisionSystemTests
    {
        public static void RunAllTests()
        {
            Debug.Log("Starting Collision System Tests...");
            TestCollisionHelper();
            TestCentralCollisionHandler();
            TestCustomColliderProperties();
            TestPlayerInputAndMovement();
            TestPlayerGroundedAndJump();
            TestPlayerLandingAndGroundMovementNoPush();
            TestPlayerDoubleJumpForbidden();
            TestPlayerObstacleCollision();
            TestPlayerDeathZone();
            Debug.Log("All Collision System Tests Passed Successfully!");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Test assertion failed: {message}");
            }
        }

        private static void TestCollisionHelper()
        {
            // Sphere to Sphere
            Assert(CollisionHelper.SphereToSphere(new Vector3(0, 0, 0), 1f, new Vector3(1.5f, 0, 0), 1f), "Spheres overlap");
            Assert(!CollisionHelper.SphereToSphere(new Vector3(0, 0, 0), 1f, new Vector3(3f, 0, 0), 1f), "Spheres separated");

            // Box to Box 3D
            Assert(CollisionHelper.BoxToBox(Vector3.zero, new Vector3(1, 1, 1), new Vector3(1.5f, 0, 0), new Vector3(1, 1, 1)), "Boxes overlap 3D");
            Assert(!CollisionHelper.BoxToBox(Vector3.zero, new Vector3(1, 1, 1), new Vector3(3f, 0, 0), new Vector3(1, 1, 1)), "Boxes separated 3D");

            // Box to Sphere 3D
            Assert(CollisionHelper.BoxToSphere(Vector3.zero, new Vector3(1, 1, 1), new Vector3(1.5f, 0, 0), 1f), "Box and Sphere overlap 3D");
            Assert(!CollisionHelper.BoxToSphere(Vector3.zero, new Vector3(1, 1, 1), new Vector3(3f, 0, 0), 1f), "Box and Sphere separated 3D");

            // 3D AABB overlap penetration
            Assert(CollisionHelper.GetAABBOverlap(Vector3.zero, new Vector3(1, 1, 1), new Vector3(1.5f, 0, 0), new Vector3(1, 1, 1), out Vector3 pen), "AABB overlap 3D");
            Assert(Mathf.Approximately(pen.x, -0.5f), $"Penetration along X should be -0.5f, got {pen.x}");
        }

        private static void TestCentralCollisionHandler()
        {
            GameObject goA = new GameObject("TestColliderA");
            CustomCollider colA = goA.AddComponent<CustomCollider>();
            colA.layer = CollisionLayer.Player;
            colA.colliderType = ColliderType.Cube;
            colA.size = Vector3.one;
            colA.layersToCheck = CollisionLayerMask.Ground;

            GameObject goB = new GameObject("TestColliderB");
            CustomCollider colB = goB.AddComponent<CustomCollider>();
            colB.layer = CollisionLayer.Ground;
            colB.colliderType = ColliderType.Cube;
            colB.size = Vector3.one;

            int enterA = 0;
            int stayA = 0;
            int exitA = 0;

            colA.CollisionEnter += _ => enterA++;
            colA.CollisionStay += _ => stayA++;
            colA.CollisionExit += _ => exitA++;

            // Position overlapping
            goA.transform.position = new Vector3(0, 0, 0);
            goB.transform.position = new Vector3(0.5f, 0, 0);

            CollisionHandler.PerformCollisionChecks();
            Assert(enterA == 1, "CollisionEnter should fire once on first overlap");
            Assert(colA.ActiveCollisions.Contains(colB), "colA should have colB in ActiveCollisions");

            // Next frame still overlapping
            CollisionHandler.PerformCollisionChecks();
            Assert(enterA == 1, "CollisionEnter should not fire again");
            Assert(stayA == 1, "CollisionStay should fire on second overlapping frame");

            // Move apart
            goB.transform.position = new Vector3(10f, 0, 0);
            CollisionHandler.PerformCollisionChecks();
            Assert(exitA == 1, "CollisionExit should fire when separating");
            Assert(!colA.ActiveCollisions.Contains(colB), "colA should not have colB in ActiveCollisions");

            GameObject.DestroyImmediate(goA);
            GameObject.DestroyImmediate(goB);
        }

        private static void TestCustomColliderProperties()
        {
            GameObject sphereGo = new GameObject("SphereTest");
            CustomCollider sphereCol = sphereGo.AddComponent<CustomCollider>();
            sphereCol.colliderType = ColliderType.Sphere;
            sphereCol.radius = 1.5f;
            Assert(sphereCol.HalfSize == Vector3.one * 1.5f, "Sphere HalfSize should match radius");
            Assert(sphereCol.Bounds.size == Vector3.one * 3f, "Sphere Bounds size should match diameter");

            GameObject cubeGo = new GameObject("CubeTest");
            CustomCollider cubeCol = cubeGo.AddComponent<CustomCollider>();
            cubeCol.colliderType = ColliderType.Cube;
            cubeCol.size = new Vector3(2f, 4f, 6f);
            Assert(cubeCol.HalfSize == new Vector3(1f, 2f, 3f), "Cube HalfSize should match half dimensions");
            Assert(cubeCol.Bounds.size == new Vector3(2f, 4f, 6f), "Cube Bounds size should match full dimensions");

            GameObject.DestroyImmediate(sphereGo);
            GameObject.DestroyImmediate(cubeGo);
        }

        private static void TestPlayerInputAndMovement()
        {
            GameObject playerGo = new GameObject("PlayerInputTest");
            PlayerController player = playerGo.AddComponent<PlayerController>();

            player.SetMoveInput(new Vector2(1f, 0f));
            Assert(Mathf.Approximately(player.MoveInput.x, 1f), "MoveInput X should be 1f");
            Assert(Mathf.Approximately(player.Velocity.x, player.MoveSpeed), "Velocity X should match MoveSpeed");

            player.SetMoveInput(new Vector2(-0.5f, 0f));
            Assert(Mathf.Approximately(player.MoveInput.x, -0.5f), "MoveInput X should be -0.5f");
            Assert(Mathf.Approximately(player.Velocity.x, -0.5f * player.MoveSpeed), "Velocity X should match negative fractional speed");

            GameObject.DestroyImmediate(playerGo);
        }

        private static void TestPlayerGroundedAndJump()
        {
            GameObject playerGo = new GameObject("PlayerTest");
            PlayerController player = playerGo.AddComponent<PlayerController>();
            playerGo.transform.position = new Vector3(0, 3f, 0);

            GameObject groundGo = new GameObject("GroundTest");
            CustomCollider groundCollider = groundGo.AddComponent<CustomCollider>();
            groundCollider.layer = CollisionLayer.Ground;
            groundCollider.colliderType = ColliderType.Cube;
            groundCollider.size = Vector3.one;
            groundGo.transform.position = new Vector3(0, 0, 0);

            // Frame 1: in air falling
            player.ApplyPhysics(0.1f);
            Assert(!player.IsGrounded, "Player should not be grounded while in mid-air");

            // Simulate falling until player lands on ground (top of ground at y = 0.5, player half-height = 0.5 -> expected player y = 1.0)
            for (int i = 0; i < 20; i++)
            {
                player.ApplyPhysics(0.1f);
                if (player.IsGrounded) break;
            }

            Assert(player.IsGrounded, "Player must land on ground and become grounded");
            Assert(Mathf.Approximately(playerGo.transform.position.y, 1.0f), $"Player Y should land on top of ground (1.0), got {playerGo.transform.position.y}");

            // Jump
            bool jumped = player.Jump();
            Assert(jumped, "Jump should succeed when grounded");
            Assert(player.Velocity.y > 0f, "Vertical velocity must be positive after jump");
            Assert(!player.IsGrounded, "Player must no longer be grounded right after jump");

            GameObject.DestroyImmediate(playerGo);
            GameObject.DestroyImmediate(groundGo);
        }

        private static void TestPlayerLandingAndGroundMovementNoPush()
        {
            GameObject groundGo = new GameObject("WideGroundTest");
            CustomCollider groundCollider = groundGo.AddComponent<CustomCollider>();
            groundCollider.layer = CollisionLayer.Ground;
            groundCollider.colliderType = ColliderType.Cube;
            groundCollider.size = new Vector3(10f, 1f, 1f);
            groundGo.transform.position = new Vector3(0, 0, 0);

            GameObject playerGo = new GameObject("PlayerLandingTest");
            PlayerController player = playerGo.AddComponent<PlayerController>();
            // Start falling from above the center of the ground
            playerGo.transform.position = new Vector3(0, 2f, 0);
            player.SetHorizontalInput(1f); // Moving right while falling

            // Fall and land
            for (int i = 0; i < 20; i++)
            {
                player.ApplyPhysics(0.05f);
            }

            Assert(player.IsGrounded, "Player should be grounded after landing");
            Assert(Mathf.Approximately(playerGo.transform.position.y, 1.0f), $"Player Y should be 1.0, got {playerGo.transform.position.y}");
            Assert(playerGo.transform.position.x > 0.5f && playerGo.transform.position.x < 4.5f,
                $"Player X position should move naturally right without being pushed to platform edges (-5.5 or 5.5), got {playerGo.transform.position.x}");

            // Continue moving right on the ground
            float startX = playerGo.transform.position.x;
            for (int i = 0; i < 5; i++)
            {
                player.ApplyPhysics(0.05f);
            }

            Assert(playerGo.transform.position.x > startX, "Player should continue moving right across ground");
            Assert(player.IsGrounded, "Player should stay grounded while walking on ground");

            GameObject.DestroyImmediate(playerGo);
            GameObject.DestroyImmediate(groundGo);
        }

        private static void TestPlayerDoubleJumpForbidden()
        {
            GameObject playerGo = new GameObject("PlayerDoubleJumpTest");
            PlayerController player = playerGo.AddComponent<PlayerController>();

            player.SetGrounded(true);
            bool firstJump = player.Jump();
            Assert(firstJump, "First jump when grounded should succeed");

            bool secondJump = player.Jump();
            Assert(!secondJump, "Double jump in mid-air must be strictly forbidden");

            GameObject.DestroyImmediate(playerGo);
        }

        private static void TestPlayerObstacleCollision()
        {
            GameObject playerGo = new GameObject("PlayerObstacleTest");
            PlayerController player = playerGo.AddComponent<PlayerController>();
            playerGo.transform.position = new Vector3(0, 0, 0);

            GameObject obsGo = new GameObject("ObstacleTest");
            CustomCollider obsCollider = obsGo.AddComponent<CustomCollider>();
            obsCollider.layer = CollisionLayer.Obstacle;
            obsCollider.colliderType = ColliderType.Cube;
            obsCollider.size = Vector3.one;
            obsGo.transform.position = new Vector3(1.2f, 0, 0);

            // Move player right toward obstacle
            player.SetHorizontalInput(1f);
            player.ApplyPhysics(0.5f);

            // Player half size is 0.5, obstacle is at 1.2 with half size 0.5 (left edge is 0.7) -> player x should be <= 0.2
            Assert(playerGo.transform.position.x <= 0.201f, $"Player must not move past obstacle, position: {playerGo.transform.position.x}");
            Assert(player.Velocity.x == 0f, "Horizontal velocity must be stopped against obstacle");

            GameObject.DestroyImmediate(playerGo);
            GameObject.DestroyImmediate(obsGo);
        }

        private static void TestPlayerDeathZone()
        {
            GameObject deathZoneGo = new GameObject("DeathZoneTest");
            DeathZone deathZone = deathZoneGo.AddComponent<DeathZone>();
            Assert(deathZone.Collider.layer == CollisionLayer.DeathZone, "Death zone collider must be on DeathZone layer");

            GameObject.DestroyImmediate(deathZoneGo);
        }
    }
}
