using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TransversalPowerEffectsController : MonoBehaviour
{
    private float cameraStartFOV;

    public float speed = 1f;

    public float cameraFOVAddition  = 0f;
    public float additiveColorAlpha = 0f;
    public float radialZoomRadius   = 0f;

    public float cameraFOVAdditionStart  = 50f;
    public float additiveColorAlphaStart = 1f;
    public float radialZoomRadiusStart   = 15f;

    public Camera playerCamera;

    public ScriptableRendererFeature FEATURE_AdditiveColor;
    public Material                  FEATURE_AdditiveColorMaterial;

    public ScriptableRendererFeature FEATURE_RadialZoom;
    public Material                  FEATURE_RadialZoomMaterial;

    int SHADER_additiveAlpha    = Shader.PropertyToID("_Alpha");
    int SHADER_radialZoomRadius = Shader.PropertyToID("_Radius");

    public bool IsTest   = true;
    public bool IsActive = true;

    void Start() {
        // TODO: This is temporary for now. In an actual project, we would probably have some form of
        // structure that would contain our FOV.
        // We should also probably store the FOV at the time that the effect is performed, so that
        // we can accurately restore the actual FOV that the camera had before the casting.
        cameraStartFOV = playerCamera.fieldOfView;
    }

    public void StartEffect() {
        t = 0f;
        additiveColorAlpha = additiveColorAlphaStart;
        radialZoomRadius   = radialZoomRadiusStart;
    }

    public float t = 1f;
    void Update() {
        FEATURE_AdditiveColor.SetActive(IsActive);
        FEATURE_RadialZoom.SetActive(IsActive);

        if (!IsActive) return;

        FEATURE_AdditiveColorMaterial.SetFloat(SHADER_additiveAlpha,    additiveColorAlpha);
        FEATURE_RadialZoomMaterial   .SetFloat(SHADER_radialZoomRadius, radialZoomRadius);
        playerCamera.fieldOfView = cameraStartFOV + cameraFOVAddition;

        if (IsTest) return;
        if (t > 1f) return;

        additiveColorAlpha = additiveColorAlphaStart * (1 - t);
        radialZoomRadius   = radialZoomRadiusStart   * (1 - t);
        cameraFOVAddition  = cameraFOVAdditionStart  * (1 - t);

        t += Time.deltaTime * speed;
        if (t > 1f) t = 1f;
    }

    void OnApplicationQuit() {
        Debug.Log(nameof(TransversalPowerEffectsController) + " stopping");
        // TODO: changing these features during runtime stick in the editor once stopped.
        // Is there a way to make these behave like instances?
        // Would we have to manage it ourselves for the editor?
        FEATURE_AdditiveColorMaterial.SetFloat(SHADER_additiveAlpha,    0);
        FEATURE_RadialZoomMaterial   .SetFloat(SHADER_radialZoomRadius, 0);

        FEATURE_AdditiveColor.SetActive(false);
        FEATURE_RadialZoom.SetActive(false);
    }
}
