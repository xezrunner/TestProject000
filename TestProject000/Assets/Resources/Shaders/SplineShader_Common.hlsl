/* from .../<universal rp>/Shaders/LitGBufferPass.hlsl.meta:

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

*/

// NOTE: match with Spline.cs::GPUSplinePoint
struct SplinePoint {
    float3 pos;
    float4 rot;
    float4 bankingRot;
};

int _SplinePointCount;
StructuredBuffer<SplinePoint> _SplineBuffer;

float _SplineTotalLength;

// For GetPointByDistance()
uint _SplineLookupResolution;
Buffer<float> _SplineArcLengths;

float3 CalculateCatmullRomPosition(SplinePoint p0, SplinePoint p1, SplinePoint p2, SplinePoint p3, float t) {
    float t2 = t * t;
    float t3 = t2 * t;

    return 0.5 * (
        (2 * p1.pos) +
        (-p0.pos + p2.pos) * t +
        (2 * p0.pos - 5 * p1.pos + 4 * p2.pos - p3.pos) * t2 +
        (-p0.pos + 3 * p1.pos - 3 * p2.pos + p3.pos) * t3
    );
}

SplinePoint GetPoint(float t) {
    SplinePoint sp;
    sp.pos = float3(0,0,0);
    sp.rot = float4(0,0,0,1);
    sp.bankingRot = float4(0,0,0,1);

    // TODO: Branching!
    // TODO: Warn during C# setup!
    // if (_SplinePointCount == 0) return sp;
    // if (_SplinePointCount  < 4) return sp;

    // TODO: t = Mathf.Clamp01(t);

    // We now treat the spline as having (count-1) segments.
    int   numSegments  = _SplinePointCount - 1;
    float segmentT     = t * numSegments;
    int   segmentIndex = floor(segmentT); // TODO:

    if (segmentIndex >= numSegments) {
        segmentIndex = numSegments - 1;
        segmentT = 1;
    } else {
        segmentT -= segmentIndex;
    }

    // For a segment from points[i] to points[i+1] we choose:
    // p0: previous point (or duplicate the first point)
    // p1: current point
    // p2: next point
    // p3: point after that (or duplicate the last point)
    SplinePoint p0;
    SplinePoint p1;
    SplinePoint p2;
    SplinePoint p3;

    if (segmentIndex == 0) p0 = _SplineBuffer[0];
    else p0 = _SplineBuffer[segmentIndex - 1];

    p1 = _SplineBuffer[segmentIndex];
    p2 = _SplineBuffer[segmentIndex + 1];

    if (segmentIndex + 2 < _SplinePointCount) p3 = _SplineBuffer[segmentIndex + 2];
    else p3 = _SplineBuffer[_SplinePointCount - 1];

    float3 position = CalculateCatmullRomPosition(p0, p1, p2, p3, segmentT);
    // TODO: float4 rotation = Quaternion.Slerp(p1.rot, p2.rot, segmentT);

    sp.pos = position;

    return sp;
}

// TODO: do we actually need this?
#if 0
SplinePoint GetPoint(float3 position) {
    float closestT = 0;
    float closestDist = 99999999999.0; // TODO: this should be FLT_MAX, but that isn't available in compute shaders (?)

    for (int i = 0; i <= 100; i++) {
        float t = i / 100;
        SplinePoint sp = GetPoint(t);
        float dist = distance(sp.pos, position);

        if (dist < closestDist) {
            closestDist = dist;
            closestT = t;
        }
    }

    return GetPoint(closestT);
}
#endif

SplinePoint GetPointByDistance(float dist) {
    // Clamp the requested distance to the total length of the spline.
    // TODO: optimization!
    // if      (dist <= 0.0)                return GetPoint(0);
    // else if (dist >= _SplineTotalLength) return GetPoint(1);

    // Binary search to find the smallest index such that arcLengths[index] >= dist.
    uint left = 0;
    uint right = _SplineLookupResolution;
    while (left < right) {
        uint mid = (left + right) / 2;
        if (_SplineArcLengths[mid] < dist) left = mid + 1;
        else right = mid;
    }

    uint indexFound = left;
    float segmentStartDist = _SplineArcLengths[indexFound - 1];
    float segmentEndDist = _SplineArcLengths[indexFound];

    // Determine the local fraction within this segment.
    float localFraction = (dist - segmentStartDist) / (segmentEndDist - segmentStartDist);

    // Map the interval back to the global t parameter.
    float t0 = (indexFound - 1) / (float)_SplineLookupResolution;
    float t1 = indexFound / (float)_SplineLookupResolution;
    float targetT = lerp(t0, t1, localFraction);

    return GetPoint(targetT);
}