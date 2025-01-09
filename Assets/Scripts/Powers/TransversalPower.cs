using static DebugStats;

class TransversalPower: PlayerPower {
    public TransversalPower() {
        // TODO: attribute for props?
        this.autoConsumeMana = false;
        this.manaCost = 20f;   // TODO: should we define mana portion constants?
        this.cooldownSec = 1f;
    }

    // TODO: port TransversalPower!

    public override bool POWER_Cast() {
        STATS_PrintQuickLine("Cast!".bold());
        return true;
    }

    public override bool POWER_Cancel() {
        STATS_PrintQuickLine("Cancel!");
        return true;
    }

}