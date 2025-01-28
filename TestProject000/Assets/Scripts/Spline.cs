using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct SplinePoint {
    public Vector3 position;
    public float bankAngle;
}

public class Spline : MonoBehaviour {
    public List<SplinePoint> points = new List<SplinePoint>();
    public bool loop = false;
    public int projectionRefinementSteps = 5;
    public int projectionInitialStepsPerSegment = 10;

    public Vector3 GetPosition(float t) {
        if (points.Count < 2) return transform.position;

        int numSegments = loop ? points.Count : points.Count - 1;
        t = Mathf.Clamp01(t);
        float segmentT = t * numSegments;
        int segmentIndex = Mathf.FloorToInt(segmentT);
        if (segmentIndex >= numSegments) {
            segmentIndex = numSegments - 1;
            segmentT = numSegments;
        } else {
            segmentT -= segmentIndex;
        }

        int p0Index, p1Index, p2Index, p3Index;
        GetIndices(segmentIndex, out p0Index, out p1Index, out p2Index, out p3Index);

        SplinePoint p0 = points[p0Index];
        SplinePoint p1 = points[p1Index];
        SplinePoint p2 = points[p2Index];
        SplinePoint p3 = points[p3Index];

        Vector3 pos = CalculateCatmullRomPosition(p0.position, p1.position, p2.position, p3.position, segmentT);
        return transform.TransformPoint(pos);
    }

    public Vector3 GetTangent(float t) {
        if (points.Count < 2) return transform.forward;

        int numSegments = loop ? points.Count : points.Count - 1;
        t = Mathf.Clamp01(t);
        float segmentT = t * numSegments;
        int segmentIndex = Mathf.FloorToInt(segmentT);
        if (segmentIndex >= numSegments) {
            segmentIndex = numSegments - 1;
            segmentT = numSegments;
        } else {
            segmentT -= segmentIndex;
        }

        int p0Index, p1Index, p2Index, p3Index;
        GetIndices(segmentIndex, out p0Index, out p1Index, out p2Index, out p3Index);

        SplinePoint p0 = points[p0Index];
        SplinePoint p1 = points[p1Index];
        SplinePoint p2 = points[p2Index];
        SplinePoint p3 = points[p3Index];

        Vector3 tangent = CalculateCatmullRomTangent(p0.position, p1.position, p2.position, p3.position, segmentT);
        return transform.TransformVector(tangent).normalized;
    }

    public float GetBankAngle(float t) {
        if (points.Count < 2) return 0f;

        int numSegments = loop ? points.Count : points.Count - 1;
        t = Mathf.Clamp01(t);
        float segmentT = t * numSegments;
        int segmentIndex = Mathf.FloorToInt(segmentT);
        if (segmentIndex >= numSegments) {
            segmentIndex = numSegments - 1;
            segmentT = 1f;
        } else {
            segmentT -= segmentIndex;
        }

        int p1Index = loop ? segmentIndex % points.Count : segmentIndex;
        int p2Index = loop ? (segmentIndex + 1) % points.Count : Mathf.Min(segmentIndex + 1, points.Count - 1);

        return Mathf.Lerp(points[p1Index].bankAngle, points[p2Index].bankAngle, segmentT);
    }

    public Quaternion GetRotation(float t) {
        Vector3 tangent = GetTangent(t);
        if (tangent.sqrMagnitude == 0) return Quaternion.identity;

        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(tangent.normalized, up)) > 0.9999f) {
            up = Vector3.forward;
        }

        Quaternion lookRotation = Quaternion.LookRotation(tangent, up);
        float bankAngle = GetBankAngle(t);
        return lookRotation * Quaternion.Euler(0, 0, bankAngle);
    }

    public Vector3 ProjectPosition(Vector3 worldPosition, out float t) {
        t = 0f;
        if (points.Count < 2) return transform.position;

        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        float minDistSq = float.MaxValue;
        float closestT = 0f;
        int totalSegments = loop ? points.Count : points.Count - 1;

        for (int i = 0; i < totalSegments; i++) {
            for (int j = 0; j < projectionInitialStepsPerSegment; j++) {
                float segT = j / (float)projectionInitialStepsPerSegment;
                float currentT = (i + segT) / totalSegments;
                Vector3 pos = GetPosition(currentT);
                float distSq = (pos - worldPosition).sqrMagnitude;
                if (distSq < minDistSq) {
                    minDistSq = distSq;
                    closestT = currentT;
                }
            }
        }

        float refineStep = 1f / (totalSegments * projectionInitialStepsPerSegment * 2);
        float lower = Mathf.Max(0, closestT - refineStep);
        float upper = Mathf.Min(1, closestT + refineStep);

        for (int i = 0; i < projectionRefinementSteps; i++) {
            float midT = (lower + upper) * 0.5f;
            Vector3 midPos = GetPosition(midT);
            float midDistSq = (midPos - worldPosition).sqrMagnitude;

            float leftT = Mathf.Max(midT - refineStep, 0);
            Vector3 leftPos = GetPosition(leftT);
            float leftDistSq = (leftPos - worldPosition).sqrMagnitude;

            float rightT = Mathf.Min(midT + refineStep, 1);
            Vector3 rightPos = GetPosition(rightT);
            float rightDistSq = (rightPos - worldPosition).sqrMagnitude;

            if (leftDistSq < midDistSq) {
                upper = midT;
                midDistSq = leftDistSq;
                midT = leftT;
            } else if (rightDistSq < midDistSq) {
                lower = midT;
                midDistSq = rightDistSq;
                midT = rightT;
            }

            closestT = midT;
            refineStep *= 0.5f;
        }

        t = closestT;
        return GetPosition(closestT);
    }

    private void GetIndices(int segmentIndex, out int p0, out int p1, out int p2, out int p3) {
        if (loop) {
            p0 = (segmentIndex - 1 + points.Count) % points.Count;
            p1 = segmentIndex % points.Count;
            p2 = (segmentIndex + 1) % points.Count;
            p3 = (segmentIndex + 2) % points.Count;
        } else {
            p0 = Mathf.Max(segmentIndex - 1, 0);
            p1 = Mathf.Min(segmentIndex, points.Count - 1);
            p2 = Mathf.Min(segmentIndex + 1, points.Count - 1);
            p3 = Mathf.Min(segmentIndex + 2, points.Count - 1);
        }
    }

    private Vector3 CalculateCatmullRomPosition(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2 * p1) + (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
    }

    private Vector3 CalculateCatmullRomTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        float t2 = t * t;
        return 0.5f * ((-p0 + p2) +
            2 * (2 * p0 - 5 * p1 + 4 * p2 - p3) * t +
            3 * (-p0 + 3 * p1 - 3 * p2 + p3) * t2);
    }

    private void OnDrawGizmos() {
        if (points == null || points.Count < 2) return;

        Gizmos.color = Color.white;
        int numSteps = 20 * (loop ? points.Count : points.Count - 1);
        Vector3 prevPos = GetPosition(0);

        for (int i = 1; i <= numSteps; i++) {
            float t = i / (float)numSteps;
            Vector3 pos = GetPosition(t);
            Gizmos.DrawLine(prevPos, pos);
            prevPos = pos;
        }

        Gizmos.color = Color.yellow;
        foreach (SplinePoint point in points) {
            Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.1f);
        }
    }
}