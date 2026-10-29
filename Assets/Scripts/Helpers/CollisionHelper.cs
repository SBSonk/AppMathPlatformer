using UnityEngine;

namespace Helpers
{
    public static class CollisionHelper
    {
        public static bool SphereToSphere(Vector3 posA, float radiusA, Vector3 posB, float radiusB)
        {
            float dx = posA.x - posB.x;
            float dy = posA.y - posB.y;
            float dz = posA.z - posB.z;
            float radSum = radiusA + radiusB;
            
            return (dx * dx + dy * dy + dz * dz) < (radSum * radSum);
        }

        public static bool BoxToBox(Vector3 posA, float radiusA, Vector3 posB, float radiusB)
        {
            return Mathf.Abs(posA.x - posB.x) < (radiusA + radiusB) &&
                   Mathf.Abs(posA.y - posB.y) < (radiusA + radiusB) &&
                   Mathf.Abs(posA.z - posB.z) < (radiusA + radiusB);
        }

        public static bool BoxToBox(Vector3 posA, Vector3 halfSizeA, Vector3 posB, Vector3 halfSizeB)
        {
            return Mathf.Abs(posA.x - posB.x) < (halfSizeA.x + halfSizeB.x) &&
                   Mathf.Abs(posA.y - posB.y) < (halfSizeA.y + halfSizeB.y) &&
                   Mathf.Abs(posA.z - posB.z) < (halfSizeA.z + halfSizeB.z);
        }

        public static bool BoxToSphere(Vector3 boxPos, float boxRadius, Vector3 spherePos, float sphereRadius)
        {
            float closestX = Mathf.Clamp(spherePos.x, boxPos.x - boxRadius, boxPos.x + boxRadius);
            float closestY = Mathf.Clamp(spherePos.y, boxPos.y - boxRadius, boxPos.y + boxRadius);
            float closestZ = Mathf.Clamp(spherePos.z, boxPos.z - boxRadius, boxPos.z + boxRadius);

            float dx = spherePos.x - closestX;
            float dy = spherePos.y - closestY;
            float dz = spherePos.z - closestZ;

            return (dx * dx + dy * dy + dz * dz) < (sphereRadius * sphereRadius);
        }

        public static bool BoxToSphere(Vector3 boxPos, Vector3 boxHalfSize, Vector3 spherePos, float sphereRadius)
        {
            float closestX = Mathf.Clamp(spherePos.x, boxPos.x - boxHalfSize.x, boxPos.x + boxHalfSize.x);
            float closestY = Mathf.Clamp(spherePos.y, boxPos.y - boxHalfSize.y, boxPos.y + boxHalfSize.y);
            float closestZ = Mathf.Clamp(spherePos.z, boxPos.z - boxHalfSize.z, boxPos.z + boxHalfSize.z);

            float dx = spherePos.x - closestX;
            float dy = spherePos.y - closestY;
            float dz = spherePos.z - closestZ;

            return (dx * dx + dy * dy + dz * dz) < (sphereRadius * sphereRadius);
        }

        public static bool SphereToBox(Vector3 spherePos, float sphereRadius, Vector3 boxPos, Vector3 boxHalfSize)
        {
            return BoxToSphere(boxPos, boxHalfSize, spherePos, sphereRadius);
        }

        public static bool GetAABBOverlap(Vector3 posA, Vector3 halfSizeA, Vector3 posB, Vector3 halfSizeB, out Vector3 minPenetration)
        {
            minPenetration = Vector3.zero;
            float dx = posA.x - posB.x;
            float px = (halfSizeA.x + halfSizeB.x) - Mathf.Abs(dx);
            if (px <= 0f) return false;

            float dy = posA.y - posB.y;
            float py = (halfSizeA.y + halfSizeB.y) - Mathf.Abs(dy);
            if (py <= 0f) return false;

            float dz = posA.z - posB.z;
            float pz = (halfSizeA.z + halfSizeB.z) - Mathf.Abs(dz);
            if (pz <= 0f) return false;

            if (px <= py && px <= pz)
            {
                float signX = dx >= 0f ? 1f : -1f;
                minPenetration = new Vector3(px * signX, 0f, 0f);
            }
            else if (py <= px && py <= pz)
            {
                float signY = dy >= 0f ? 1f : -1f;
                minPenetration = new Vector3(0f, py * signY, 0f);
            }
            else
            {
                float signZ = dz >= 0f ? 1f : -1f;
                minPenetration = new Vector3(0f, 0f, pz * signZ);
            }
            return true;
        }
    }
}