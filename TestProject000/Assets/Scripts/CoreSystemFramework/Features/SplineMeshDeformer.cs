// #define OLD_IMPL

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CoreSystemFramework;
using Unity.Collections;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

using static CoreSystemFramework.Logging;

public class SplineMeshDeformer : MonoBehaviour {
    public Spline spline;

    public ComputeShader shader;

    #if !OLD_IMPL

    struct Data {
        public Data(Spline spline, Mesh mesh, Vector3 position = default) {
            this.spline = spline;
            gpuSplinePoints = spline.getGPUSplinePoints();

            this.mesh = mesh;
            vertexCount = mesh.vertexCount;
            originalVertices = new(vertexCount);
            mesh.GetVertices(originalVertices);

            this.position = position;

            vertexBuffer = null;
            //vertexBuffer = new(vertexCount, Marshal.SizeOf(typeof(Vector3)));
            //vertexBuffer.SetData(originalVertices);

            splinePointsBuffer = new(gpuSplinePoints.Length, Marshal.SizeOf(typeof(GPUSplinePoint)));
            splinePointsBuffer.SetData(gpuSplinePoints);

            splineArcLengthsBuffer = new(spline.arcLengths.Length, Marshal.SizeOf(typeof(float)));
            splineArcLengthsBuffer.SetData(spline.arcLengths);
        }

        public void InitializeNewVertexBuffer() {
            vertexBuffer = new(vertexCount, Marshal.SizeOf(typeof(Vector3)));
            vertexBuffer.SetData(originalVertices);
        }
        
        public Spline spline;
        public GPUSplinePoint[] gpuSplinePoints;
        
        public Mesh mesh;
        public int vertexCount;
        public List<Vector3> originalVertices;

        // Props:
        public Vector3 position;

        // Buffers;
        public ComputeBuffer vertexBuffer;
        public ComputeBuffer splinePointsBuffer;
        public ComputeBuffer splineArcLengthsBuffer;
    }

    Data sharedData;

    public MeshFilter meshFilter;

    void Start() => Initialize();

    static readonly int SHADER_PROP_SplinePointCount       = Shader.PropertyToID("_SplinePointCount");
    static readonly int SHADER_PROP_SplineBuffer           = Shader.PropertyToID("_SplineBuffer");
    static readonly int SHADER_PROP_SplineTotalLength      = Shader.PropertyToID("_SplineTotalLength");
    static readonly int SHADER_PROP_SplineLookupResolution = Shader.PropertyToID("_SplineLookupResolution");
    static readonly int SHADER_PROP_SplineArcLengths       = Shader.PropertyToID("_SplineArcLengths");
    static readonly int SHADER_PROP_VertexCount            = Shader.PropertyToID("_VertexCount");
    static readonly int SHADER_PROP_Vertices               = Shader.PropertyToID("_Vertices");
    static readonly int SHADER_PROP_zOffset                = Shader.PropertyToID("_zOffset");

    int kernelID = -1;

    public void Initialize() {
        bool ready = true;
        
        if (!spline)     ready = false;
        if (!shader)     ready = false;
        if (!meshFilter) ready = false;

        var mesh = meshFilter.mesh;
        if (!mesh)       ready = false;

        if (!ready) {
            logError("Missing refs/data.");
            return;
        }

        sharedData = new(spline, mesh);

        kernelID = shader.FindKernel("CSMain");

        // Upload to shader:
        shader.SetInt    (          SHADER_PROP_SplinePointCount,      sharedData.gpuSplinePoints.Length);
        shader.SetBuffer (kernelID, SHADER_PROP_SplineBuffer,          sharedData.splinePointsBuffer);
        shader.SetFloat  (          SHADER_PROP_SplineTotalLength,     sharedData.spline.totalLength);
        shader.SetInt    (          SHADER_PROP_SplineLookupResolution,sharedData.spline.lookupResolution);
        shader.SetBuffer (kernelID, SHADER_PROP_SplineArcLengths,      sharedData.splineArcLengthsBuffer);
        // shader.SetInt    (          SHADER_PROP_VertexCount,           sharedData.vertexCount);
        // shader.SetBuffer (kernelID, SHADER_PROP_Vertices,              sharedData.vertexBuffer);
    }

    void ReleaseIfNotNull(IDisposable disposable) {
        if (disposable != null) disposable.Dispose();
    }

    void OnDisable() {
        // TODO: release whatever needs releasing!
        // sharedData.vertexBuffer.Release();
        ReleaseIfNotNull(sharedData.splinePointsBuffer);
        ReleaseIfNotNull(sharedData.splineArcLengthsBuffer);
        ReleaseIfNotNull(sharedData.vertexBuffer);
    }

    List<(Data data, MeshFilter meshFilter, int tag)> requests = new(capacity: 50);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO: unsure if effective
    public void RequestMeshDeform_Shared(Vector3 position, MeshFilter meshFilter, int tag = -1) {
        var data = sharedData;
        data.position = position;
        data.InitializeNewVertexBuffer();

        requests.Add((data, meshFilter, tag));
    }

    IEnumerator COROUTINE_DispatchAllRequests() {
        int count = requests.Count;
        log($"Dispatching {count} requests...");

        for (int i = 0; i < count; ++i) {
            if (i % DISPATCH_RequestsPerFrame == 0) yield return null;
            queueDispatch(i); // @alloc
        }

        StartCoroutine(COROUTINE_ProcessDispatchRequests());
        requests.Clear();
    }

    public void DispatchAllRequests() => StartCoroutine(COROUTINE_DispatchAllRequests());

    public event Action<int> onDispatchRequestFinished;

    class DispatchedRequestInfo {
        public Data data;
        public AsyncGPUReadbackRequest request;
        public NativeArray<Vector3> dataArray; // TODO: naming

        public MeshFilter meshFilter;
        public int tag = -1;
    }

    List<DispatchedRequestInfo> dispatchedRequests = new(capacity: 50);

    static readonly ProfilerMarker PROFILING_queueDispatchMarker = new ProfilerMarker("SMD::queueDispatch");
    void queueDispatch(int index) {
        PROFILING_queueDispatchMarker.Begin();
        
        shader.SetFloat (SHADER_PROP_zOffset           , requests[index].data.position.z);
        shader.SetInt   (SHADER_PROP_VertexCount       , requests[index].data.vertexCount);
        shader.SetBuffer(kernelID, SHADER_PROP_Vertices, requests[index].data.vertexBuffer);

        // Dispatch:
        int threadGroups = Mathf.CeilToInt(requests[index].data.vertexCount / 64f);
        shader.Dispatch(kernelID, threadGroups, 1, 1);

        var dataArray = new NativeArray<Vector3>(requests[index].data.vertexCount, Allocator.Persistent);
        var request = AsyncGPUReadback.RequestIntoNativeArray(ref dataArray, requests[index].data.vertexBuffer);

        var dispatchedRequestInfo = new DispatchedRequestInfo() {
            data = requests[index].data,
            request = request,
            dataArray = dataArray,
            meshFilter = requests[index].meshFilter,
            tag = requests[index].tag
        };
        dispatchedRequests.Add(dispatchedRequestInfo);

        PROFILING_queueDispatchMarker.End();
    }

    public static int DISPATCH_SetMeshesPerFrame = 10;
    public static int DISPATCH_RequestsPerFrame = 100;

    IEnumerator COROUTINE_ProcessDispatchRequests() {
        int setMeshes = 0;
        int processedPerFrame = 0;

        int count = dispatchedRequests.Count;
        if (count == 0) yield break;

        loop:
        for (int i = 0; i < count; ++i) {
            if (!dispatchedRequests[i].request.done) continue;

            if (setMeshes >= DISPATCH_SetMeshesPerFrame) {
                // log("exceeded DISPATCH_SetMeshesPerFrame, waiting a frame...");
                setMeshes = 0;
                yield return null;
            }

#if false
            if (processedPerFrame++ >= DISPATCH_RequestsPerFrame) {
                // log("exceeded DISPATCH_RequestsPerFrame, waiting a frame...");
                yield return null;
                processedPerFrame = 0;
            }
#endif

            // var mesh = dispatchedRequests[i].data.mesh;
            var mesh = dispatchedRequests[i].meshFilter.mesh; // TODO: slow!

            // Set vertices and refresh mesh:
            mesh.SetVertices(dispatchedRequests[i].dataArray);
            // NOTE: recalculating the bounds *especially* is super important for performance,
            // as having incorrect bounds info would make it believe all of them are at the origin
            // and would not get culled when far away/occluded.
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            onDispatchRequestFinished.Invoke(dispatchedRequests[i].tag);

            // TODO: Cleanup!
            dispatchedRequests[i].dataArray.Dispose();
            dispatchedRequests[i].data.vertexBuffer.Dispose();

            ++setMeshes;
        }
        dispatchedRequests.RemoveAll(x => x.request.done); // @Performance

        if (dispatchedRequests.Count == 0) log($"Dispatching of {count} requests finished.");
        else {
            yield return null;
            goto loop;
        }
    }

    #else
    public MeshFilter meshFilter;


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
    #endif
}