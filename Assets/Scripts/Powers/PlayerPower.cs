using static DebugStats;

abstract class PlayerPower { // TODO: MonoBehaviour
    /// <summary> Whether to automatically handle mana consumption on casting. 
    /// If false, mana will not be checked before considering to call POWER_Cast(). 
    /// You may consume mana yourself within the power. </summary>
    public bool  autoConsumeMana = true;
    public float manaCost        = 20f;
    public float cooldownSec     = 0f;

    // TODO: should this be controllled by the individual powers?
    private bool isBeingCast       = false;
    public  bool getIsBeingCast() => isBeingCast;

    public bool RequestCast() {
        if (autoConsumeMana) {
            var magicSystem = Player.Instance?.magicSystem;
            if (!magicSystem) {
                STATS_PrintQuickLine("no magic system");
                return false;
            }

            bool success = magicSystem?.ConsumeMana(manaCost) ?? false;
            if (!success) {
                STATS_PrintQuickLine($"failed to consume mana (need {manaCost}, have {magicSystem.getMana()})");
                return false;
            }
        }

        // TODO: check cooldown!

        return POWER_Cast();
    }
    public bool RequestCancel() {
        return POWER_Cancel();
    }

    public abstract bool POWER_Cast();
    public abstract bool POWER_Cancel();
}