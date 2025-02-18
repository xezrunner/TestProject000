using System.Runtime.InteropServices;
using CoreSystemFramework;
using UnityEditor;
using UnityEngine;

using static CoreSystemFramework.Logging;

[CustomEditor(typeof(SplineGPUTest))]
class SplineGPUTestEditor: Editor {
    SplineGPUTest instance;

    void OnEnable() => instance = (SplineGPUTest)target;
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (!instance) return;

        if (GUILayout.Button("Upload spline data")) {
            instance.uploadSplineData();
        }
    }
}

[ExecuteInEditMode]
public class SplineGPUTest : MonoBehaviour {
    public Spline       spline;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    void OnEnable() => uploadSplineData();

    public void uploadSplineData() {
        if (!spline) {
            logError("no spline!"); return;
        }
        if (!meshRenderer) {
            logError("no meshRenderer!"); return;
        }
        if (!meshFilter) {
            logError("no meshFilter!"); return;
        }

        var material = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;

        // Prepare GPUSplinePoints:
        var count = spline.points.Count;
        var points = new GPUSplinePoint[count];
        for (int i = 0; i < count; ++i) {
            var point = spline.points[i];
            points[i] = new(point.pos, point.rot, point.bankingRot);
        }

        ComputeBuffer splineBuffer;
        splineBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(GPUSplinePoint)));
        splineBuffer.SetData(points);

        material.SetInteger("_SplinePointCount", count);
        material.SetBuffer("_SplineBuffer", splineBuffer);

        material.SetFloat("_SplineTotalLength", spline.totalLength);
        material.SetFloatArray("_SplineArcLengths", spline.arcLengths);

        var mesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
        // mesh.bounds = new(default, new(300f, 300f, 300f));
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        
        log("uploaded!");
    }
}
