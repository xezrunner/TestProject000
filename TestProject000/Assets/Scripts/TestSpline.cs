using UnityEngine;

[ExecuteInEditMode]
class TestSpline: MonoBehaviour {
    public Spline spline;

    Vector3 cubeSize = new(0.25f, 0.25f, 0.25f);

    [Range(0, 1)] public float t = 0f;

    void OnDrawGizmos() {
        return;
        if (!spline) return;

        Gizmos.color = new(1f, 0.25f, 0f);
        
        var spPoint = spline.GetPoint(t);
        Gizmos.DrawWireCube(spPoint.pos, cubeSize);
    }

    public Transform testTransform;

    void Update() {
        if (!testTransform) return;

        var spPoint = spline.GetPoint(t);
        Vector3 pos = spPoint.pos;
        Quaternion rot = spPoint.rot;
        testTransform.SetPositionAndRotation(pos, rot);
    }
}