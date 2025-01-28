using UnityEngine;

[ExecuteInEditMode]
class TestSpline: MonoBehaviour {
    public Spline spline;

    Vector3 cubeSize = new(0.25f, 0.25f, 0.25f);

    public float t = 0f;

    void OnDrawGizmos() {
        return;
        if (!spline) return;

        Gizmos.color = new(1f, 0.25f, 0f);
        
        Gizmos.DrawWireCube(spline.GetPosition(t), cubeSize);
    }

    public Transform testTransform;

    void Update() {
        if (!testTransform) return;

        Vector3 pos = spline.GetPosition(t);
        Quaternion rot = spline.GetRotation(t);
        testTransform.SetPositionAndRotation(pos, rot);
    }
}