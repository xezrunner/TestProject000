using System;
using UnityEngine.Rendering;

[Serializable]
public class RadialZoomVolumeComponent : VolumeComponent {
    public FloatParameter radius = new(10);
}