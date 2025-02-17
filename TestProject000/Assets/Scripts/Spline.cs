using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

using static CoreSystemFramework.Logging;

[CustomEditor(typeof(Spline))]
class SplineEditor : Editor {
    Spline instance;

    void Awake() => instance = (Spline)target;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (GUILayout.Button("Refresh spline points")) instance?.refreshSplinePoints();
    }
}

public struct SplinePoint {
    public SplinePoint(float x, float y, float z) {
        pos = new(x, y, z);
        rot = bankingRot = default;
    }
    public SplinePoint(Vector3 pos, Quaternion rot = default, Quaternion bankingRot = default) {
        this.pos        = pos;
        this.rot        = rot;
        this.bankingRot = bankingRot;
    }
    public Vector3    pos;
    public Quaternion rot;
    public Quaternion bankingRot;
}

[ExecuteInEditMode]
public class Spline : MonoBehaviour {
    List<SplinePoint> points = new(capacity: 100) {
        new(0, 0,-4.5f),
        new(0, 0, 2.5f),
        new(8.5f, 2f, 7f),
        new(3f, 0f, 13f),
        new(3f, -2.5f, 23.5f),
    };

    public Vector3 globalBankOffset = Vector3.zero;
    public float globalBankAngle = 0f; // in degrees


    void OnEnable() {
        refreshSplinePoints();
    }

    public void refreshSplinePoints() {
        log("refreshing spline points...");
        // if (count < 4) logWarning("  < 4 points! At least 4 points are required for a catmull-rom spline to work.");
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

            Gizmos.color = Color.green;
            for (int x = 0; x < 6; ++x) {
                var offset = new Vector3(-3 + x, 0, 0);
                //var bankRot = GetBankingAngle(spPoint.pos + offset);
                //var bankPos = spPoint.pos + (bankRot * offset);
                //Gizmos.DrawLine(offset, bankPos);
                var pos2 = spPoint.pos + (spPoint.rot * offset);
                Gizmos.DrawWireCube(pos2, new(0.3f, 0.3f, 0.3f));
            }

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