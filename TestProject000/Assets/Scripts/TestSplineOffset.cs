using UnityEngine;

[ExecuteInEditMode]
class TestSplineOffset: MonoBehaviour {
    public Spline spline;
    public new Transform transform;

    public Vector3 targetPosition;
    public Vector3 offsetPosition;

    void Awake() {
        if (!transform) transform = base.transform;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(targetPosition, new(0.3f, 0.3f, 0.3f));

        Gizmos.color = Color.red;
        Gizmos.DrawCube(targetPosition + offsetPosition, new(0.3f, 0.3f, 0.3f));
    }

    void Update() {
        if (!spline) return;

        var spPoint = spline.GetPoint(targetPosition);
        var bankRot = spline.GetBankingAngle(spPoint.pos + offsetPosition);
        spPoint.pos += bankRot * offsetPosition;

        transform.SetPositionAndRotation(spPoint.pos, spPoint.rot);
    }
}