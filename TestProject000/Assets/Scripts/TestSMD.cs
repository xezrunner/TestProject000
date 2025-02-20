using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using CoreSystemFramework;
using UnityEngine;
using UnityEngine.Scripting;
using static CoreSystemFramework.Logging;

public class TestSMD: MonoBehaviour {
    public static TestSMD Instance;
    
    new Transform transform;
    
    public SplineMeshDeformer SMD;

    public GameObject preset;
    MeshFilter preset_meshFilter;

    public int x = 6;
    public int z = 4;

    // TODO: these should be dynamic based on the mesh!
    public float width  = 3.68f;
    public float height = 0.619f;
    public float length = 32.9f;

    struct DeformObjInfo {
        public GameObject obj;
        public Transform  trans;
        public MeshFilter meshFilter;
        public int x;
        public int z;
    }

    DeformObjInfo[] infos;

    void Awake() {
        Instance = this;
        transform = base.transform;
        
        preset_meshFilter = preset.GetComponent<MeshFilter>();
        if (!preset_meshFilter) preset = null;

        if (!preset) return;

        preCache();

        SMD.onDispatchRequestFinished += callback;
    }

    int callbackCount = 0;
    void callback(int tag) {
        ++callbackCount;

        infos[tag].obj.SetActive(true);
        infos[tag].trans.SetPositionAndRotation(
            new(infos[tag].x * width, 0, 0), infos[tag].trans.rotation
        );
        // log($"done with [{tag}]");

        if (callbackCount == x * z) {
            watch.Stop();
            log($"took {watch.Elapsed.TotalMilliseconds}ms ({watch.Elapsed.TotalSeconds}s) to spawn {x*z} [deformed mesh] objects");
            watch.Reset();
        }
    }

    public void preCache() {
        if (infos != null) {
            for (int i = infos.Length-1; i >= 0; --i) Destroy(infos[i].obj);
            infos = null;
        }
        callbackCount = 0;

        List<DeformObjInfo> list = new(x * z);
        for (int i = 0; i < z; ++i) {     // Z
            for (int j = 0; j < x; ++j) { // X
                var obj = Instantiate(preset, transform);
                obj.name = $"deform obj x:{i} z:{j}";
                obj.SetActive(false);

                var trans = obj.GetComponent<Transform>();

                var meshFilter = obj.GetComponent<MeshFilter>();

                var info = new DeformObjInfo() {
                    obj = obj, trans = trans,
                    meshFilter = meshFilter,
                    x = (x/2) - (j+1), z = i
                };
                list.Add(info);
            }
        }
        infos = list.ToArray();
    }

    [ConsoleCommand] static void spawn_array(int x = 6, int z = 40) {
        if (!Instance) return;
        Instance.x = x;
        Instance.z = z;
        Instance.preCache();
        Instance.spawnArrayOfObjs();
    }

    static int MaxPerFrameRequests = 50;
    IEnumerator COROUTINE_spawnArrayOfObjs() {
        var result = Resources.UnloadUnusedAssets();
        while (!result.isDone) yield return null;
        System.GC.Collect();
        
        int count = infos.Length;
        for (int i = 0; i < count; ++i) {
            if (i % MaxPerFrameRequests == 0) yield return null;
            SMD.RequestMeshDeform_Shared(new(0,0,infos[i].z * length), infos[i].meshFilter, i); // @alloc
        }

        SMD.DispatchAllRequests();
    }

    Stopwatch watch = new();
    public void spawnArrayOfObjs() {
        if (!preset) {
            logError("no preset!"); return;
        }

        watch.Start();

        log($"spawning {x*z} objects...");

        StartCoroutine(COROUTINE_spawnArrayOfObjs());
    }

    void Update() {
        STATS_PrintLine($"object count: {$"{callbackCount}/{x * z}".color(callbackCount == x * z ? Color.green : Color.white)}");
    }

    
}