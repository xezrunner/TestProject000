using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class BlurVolumeComponent : VolumeComponent {
    public BoolParameter isActive = new(true);

    public FloatParameter horizontalBlur = new(5);
    public FloatParameter verticalBlur = new(5);
}
