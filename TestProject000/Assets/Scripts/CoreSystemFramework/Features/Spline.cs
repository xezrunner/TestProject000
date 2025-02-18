using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

using static CoreSystemFramework.Logging;

namespace CoreSystemFramework {

    public struct GPUSplinePoint {
        public GPUSplinePoint(Vector3 pos, Quaternion rot, Quaternion bankingRot) {
            this.pos = pos;
            this.rot = rot;
            this.bankingRot = bankingRot;
        }
        
        Vector3    pos;
        Quaternion rot;
        Quaternion bankingRot;
    };

    [Serializable]
    public class SplinePoint {
        static int globalCounter = -1;

        public int id;

        public Vector3    pos;
        public Quaternion rot;
        public Quaternion bankingRot;

        public SplinePoint(float x, float y, float z) {
            id = ++globalCounter;

            pos = new(x, y, z);
            rot = bankingRot = Quaternion.identity;
        }
        public SplinePoint(Vector3 pos, Quaternion rot = default, Quaternion bankingRot = default) {
            id = ++globalCounter;
            
            this.pos  = pos;
            
            if (rot        == default) rot        = Quaternion.identity;
            if (bankingRot == default) bankingRot = Quaternion.identity;

            this.rot        = rot;
            this.bankingRot = bankingRot;
        }

        // public bool Equals(SplinePoint other) => id == other.id;
        // public override bool Equals(object other)      => other is SplinePoint sp && id == sp.id;

        // public override int GetHashCode() => id.GetHashCode();

        // public static bool operator ==(SplinePoint lhs, SplinePoint rhs) => lhs.Equals(rhs);

        // public static bool operator !=(SplinePoint lhs, SplinePoint rhs) => !(lhs == rhs);
    }

    [ExecuteInEditMode]
    public class Spline : MonoBehaviour {
        // [SerializeField] public List<SplinePoint> points = new(capacity: 100) {
        //     new(0.5f, 0, 0f),
        //     new(0, 0, 4f),
        //     new(8.5f, 2f, 7f),
        //     new(3f, 0f, 13f),
        //     new(3f, -2.5f, 23.5f),
        // };

        public List<SplinePoint> points = new(capacity: 100) {
            new(0, 0, 0),
            new(0, 0, 16),
            new(0, 0, 32),
            new(0, 0, 64),
            new(0, 0, 128),
        };

        public Vector3 globalBankOffset = Vector3.zero;
        public float globalBankAngle = 0f; // in degrees


        void OnEnable() {
            refreshSplinePoints();
        }

        public GPUSplinePoint[] getGPUSplinePoints() {
            int count = points.Count;
            var array = new GPUSplinePoint[count];
            for (int i = 0; i < count; ++i) {
                var it = points[i];
                array[i] = new(it.pos, it.rot, it.bankingRot);
            }
            return array;
        }

        const int resolution = 200;
        [NonSerialized] public float totalLength = 0f;
        [NonSerialized] public float[] arcLengths = new float[resolution + 1];

        public void refreshSplinePoints() {
            // log("refreshing spline points...");
            // if (count < 4) logWarning("  < 4 points! At least 4 points are required for a catmull-rom spline to work.");

            // Build the arc-length table.
            totalLength = 0f;
            SplinePoint lastPoint = GetPoint(0f);
            arcLengths = new float[resolution + 1];
            arcLengths[0] = 0f;

            for (int i = 1; i <= resolution; i++) {
                float tSample = i / (float)resolution;
                SplinePoint currentPoint = GetPoint(tSample);
                totalLength += Vector3.Distance(lastPoint.pos, currentPoint.pos);
                arcLengths[i] = totalLength;
                lastPoint = currentPoint;
            }
        }

        public SplinePoint GetPointByDistance(float dist) {
            // Clamp the requested distance to the total length of the spline.
            if (dist <= 0f) return GetPoint(0f);
            if (dist >= totalLength) return GetPoint(1f);

            // Binary search to find the smallest index such that arcLengths[index] >= dist.
            int left = 0, right = resolution;
            while (left < right) {
                int mid = (left + right) / 2;
                if (arcLengths[mid] < dist)
                    left = mid + 1;
                else
                    right = mid;
            }

            int indexFound = left;
            float segmentStartDist = arcLengths[indexFound - 1];
            float segmentEndDist = arcLengths[indexFound];

            // Determine the local fraction within this segment.
            float localFraction = (dist - segmentStartDist) / (segmentEndDist - segmentStartDist);

            // Map the interval back to the global t parameter.
            float t0 = (indexFound - 1) / (float)resolution;
            float t1 = indexFound / (float)resolution;
            float targetT = Mathf.Lerp(t0, t1, localFraction);

            return GetPoint(targetT);
        }


        void OnDrawGizmos() {
            if (points.Count == 0) return;

            Vector3 prevPos = GetPoint(0f).pos;
            int max = 50;
            for (int i = 1; i <= max; i++) {
                float t = i / (float)max;
                var spPoint = GetPoint(t);
                var pos = spPoint.pos;
                var rot = spPoint.rot;

                Gizmos.color = Color.white;
                Gizmos.DrawLine(prevPos, pos);

                Gizmos.color = Color.red;
                float angle = Quaternion.Angle(Quaternion.identity, rot);
                Gizmos.DrawLine(pos, pos + (rot * Vector3.up * (angle / 180f) * 2.5f));

                // Gizmos.color = Color.green;
                // for (int x = 0; x < 6; ++x) {
                //     var offset = new Vector3(-3 + x, 0, 0);
                //     //var bankRot = GetBankingAngle(spPoint.pos + offset);
                //     //var bankPos = spPoint.pos + (bankRot * offset);
                //     //Gizmos.DrawLine(offset, bankPos);
                //     var pos2 = spPoint.pos + (spPoint.rot * offset);
                //     Gizmos.DrawWireCube(pos2, new(0.3f, 0.3f, 0.3f));
                // }

                prevPos = pos;

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Vector3 CalculateCatmullRomPosition(SplinePoint p0, SplinePoint p1, SplinePoint p2, SplinePoint p3, float t) {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1.pos) +
                (-p0.pos + p2.pos) * t +
                (2f * p0.pos - 5f * p1.pos + 4f * p2.pos - p3.pos) * t2 +
                (-p0.pos + 3f * p1.pos - 3f * p2.pos + p3.pos) * t3
            );
        }

        public SplinePoint GetPoint(float t) {
            int count = points.Count;

            if (count == 0) return default;
            if (count  < 4) return new(points[0].pos, points[0].rot);

            t = Mathf.Clamp01(t);

            // We now treat the spline as having (count-1) segments.
            int   numSegments  = count - 1;
            float segmentT     = t * numSegments;
            int   segmentIndex = Mathf.FloorToInt(segmentT);

            if (segmentIndex >= numSegments) {
                segmentIndex = numSegments - 1;
                segmentT = 1f;
            } else {
                segmentT -= segmentIndex;
            }

            // For a segment from points[i] to points[i+1] we choose:
            // p0: previous point (or duplicate the first point)
            // p1: current point
            // p2: next point
            // p3: point after that (or duplicate the last point)
            SplinePoint p0 = segmentIndex == 0 ? points[0] : points[segmentIndex - 1];
            SplinePoint p1 = points[segmentIndex];
            SplinePoint p2 = points[segmentIndex + 1];
            SplinePoint p3 = (segmentIndex + 2 < count) ? points[segmentIndex + 2] : points[count - 1];

            Vector3 position = CalculateCatmullRomPosition(p0, p1, p2, p3, segmentT);
            Quaternion rotation = Quaternion.Slerp(p1.rot, p2.rot, segmentT);

            return new(position, rotation);
        }


        public SplinePoint GetPoint(Vector3 position) {
            float closestT = 0f;
            float closestDist = float.MaxValue;

            for (int i = 0; i <= 100; i++) {
                float t = i / 100f;
                var point = GetPoint(t);
                float dist = (point.pos - position).sqrMagnitude;

                if (dist < closestDist) {
                    closestDist = dist;
                    closestT = t;
                }
            }

            return GetPoint(closestT);
        }

        public Quaternion GetBankingAngle(Vector3 position) {
            var spPoint = GetPoint(position); // closest point on spline
            
            var rotation = spPoint.rot;

            return rotation;
        }
    }

}