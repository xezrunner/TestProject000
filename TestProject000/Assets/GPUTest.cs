using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CoreSystemFramework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
        splineArcLengthsBuffer.Release();
        vertexBuffer.Release();
    }

    ComputeBuffer vertexBuffer;
    ComputeBuffer splinePointsBuffer;
    ComputeBuffer splineArcLengthsBuffer;
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
        shader.SetInt("_SplineLookupResolution", spline.lookupResolution);

        // Arc lengths (for point by distance):
        stride = Marshal.SizeOf(typeof(float));
        splineArcLengthsBuffer = new ComputeBuffer(spline.arcLengths.Length, stride);
        splineArcLengthsBuffer.SetData(spline.arcLengths);
        shader.SetBuffer(kernel, "_SplineArcLengths", splineArcLengthsBuffer);
    }


    static List<double> timings = new(capacity: 500);

    // TODO: profile!
    List<(AsyncGPUReadbackRequest request, NativeArray<Vector3> dataArray, int id, Stopwatch watch)> requests = new();
    static int requestId = -1;
    public void runComputeShader() {
        ++requestId;
        Stopwatch watch = new();
        watch.Start();

        // Reset vertex buffer to original mesh vertices (for deformation):
        vertexBuffer.SetData(originalVertices);
        // TEMP: z offset:
        shader.SetFloat("_zOffset", zOffset);
        
        // Dispatch:
        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
        shader.Dispatch(kernel, threadGroups, 1, 1);

        // Grab results and set mesh @Performance
        var dataArray = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        var request = AsyncGPUReadback.RequestIntoNativeArray(ref dataArray, vertexBuffer);
        requests.Add((request, dataArray, requestId, watch));

        // log($"requested {requestId}");

        // vertexBuffer.GetData(resultVertices);
        // mesh.SetVertices(resultVertices);
        // mesh.RecalculateBounds();
        // mesh.RecalculateNormals();
        // mesh.RecalculateTangents();
    }

    [ConsoleCommand] static void list_timings(int n = 30) {
        double average = 0;
        foreach (var timing in timings) average += timing;
        average /= timings.Count;
        log($"average: {average}ms");
        log($"listing {n} timings:");
        for (int i = 0; i < n; ++i) log($"  - [{i}] {timings[i]}ms");
    }

    public bool allowDynamicZOffset = false;
    public bool allowAutoZOffsetUpdate = true;
    float prev_zOffset = 0;
    void Update() {
        if (!allowDynamicZOffset && allowAutoZOffsetUpdate && requests.Count < 2) {
            zOffset += 0.1f;
            runComputeShader();
        }

        if (!allowDynamicZOffset) return;

        if (zOffset != prev_zOffset) runComputeShader();
        prev_zOffset = zOffset;
    }

    void LateUpdate() {
        foreach (var it in requests) {
            if (it.request.done) {
                if (it.request.hasError) {
                    logError("Error!");
                }
                mesh.SetVertices(it.dataArray);
                // log($"processed {it.id}");
                it.watch.Stop();
                timings.Add(it.watch.Elapsed.TotalMilliseconds);
                it.dataArray.Dispose();
            }
        }
        requests.RemoveAll(x => x.request.done);

    }
}
