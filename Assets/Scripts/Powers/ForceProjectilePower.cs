using UnityEngine;

public class ForceProjectilePower : PlayerPower {
    
    
    public override bool POWER_Cancel() {
        return true;
    }

    public override (bool success, string reason) POWER_Cast() {
        return (true, null);
    }
}
