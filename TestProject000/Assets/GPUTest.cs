using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreSystemFramework;
using UnityEditor;
using UnityEngine;

using static CoreSystemFramework.Logging;

[CustomEditor(typeof(GPUTest))]
class GPUTestEditor: Editor {
    GPUTest instance;

    void OnEnable() => instance = (GPUTest)target;
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (!instance) return;

        if (!Application.isPlaying) return;
        if (!instance.ready) return;


        if (GUILayout.Button("Run compute shader")) {
            instance.runComputeShader();
        }

        GUILayout.Label("[temp] Z offset:");
        instance.zOffset = EditorGUILayout.Slider(instance.zOffset, 0, 200);
    }
}

public class GPUTest : MonoBehaviour {
    public Spline spline;
    public MeshFilter meshFilter;

    public ComputeShader shader;

    [NonSerialized] public float zOffset;
    [NonSerialized] public bool ready = true;

    int kernel;
    
    Mesh mesh;
    int vertexCount;
    List<Vector3> originalVertices;
    Vector3[] resultVertices;

    void Start() {
        if (!spline)     ready = false;
        if (!meshFilter) ready = false;
        if (!shader)     ready = false;

        mesh = meshFilter.mesh;
        if (!mesh)       ready = false;

        if (!ready) {
            logError("Missing refs.");
            return;
        }

        // Store original mesh vertices:
        vertexCount = mesh.vertexCount;
        originalVertices = new(vertexCount);
        mesh.GetVertices(originalVertices);

        // Prepare result mesh vertex array, which will be set to the mesh after CS dispatch:
        resultVertices = new Vector3[vertexCount];

        kernel = shader.FindKernel("CSMain");
        uploadDataToShader();
    }

    void OnDisable() {
        // Cleanup, otherwise these leak:
        splinePointsBuffer.Release();
        vertexBuffer.Release();
    }

    ComputeBuffer vertexBuffer;
    ComputeBuffer splinePointsBuffer;
    void uploadDataToShader() {
        // Spline point data:
        int count = spline.points.Count;
        var points = spline.getGPUSplinePoints();
        
        int stride = Marshal.SizeOf(typeof(GPUSplinePoint));
        splinePointsBuffer = new ComputeBuffer(count, stride);
        splinePointsBuffer.SetData(points);

        shader.SetInt("_SplinePointCount", count);
        shader.SetFloat("_SplineTotalLength", spline.totalLength);
        shader.SetBuffer(kernel, "_SplineBuffer", splinePointsBuffer);

        // Vertices:
        stride = Marshal.SizeOf(typeof(Vector3));
        vertexBuffer = new ComputeBuffer(vertexCount, stride);
        vertexBuffer.SetData(originalVertices);

        shader.SetInt("_VertexCount", vertexCount);
        shader.SetBuffer(kernel, "_Vertices", vertexBuffer);

        // Misc spline data:
        shader.SetFloats("_SplineArcLengths", spline.arcLengths);
    }

    // TODO: profile!
    public void runComputeShader() {
        // Reset vertex buffer to original mesh vertices (for deformation):
        vertexBuffer.SetData(originalVertices);
        // TEMP: z offset:
        shader.SetFloat("_zOffset", zOffset);
        
        // Dispatch:
        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
        shader.Dispatch(kernel, threadGroups, 1, 1);

        vertexBuffer.GetData(resultVertices);

        mesh.SetVertices(resultVertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    public bool allowDynamicZOffset = true;
    float prev_zOffset = 0;
    void Update() {
        if (!allowDynamicZOffset) return;

        if (zOffset != prev_zOffset) runComputeShader();
        prev_zOffset = zOffset;
    }
}
