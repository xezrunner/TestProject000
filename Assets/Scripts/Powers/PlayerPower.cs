using UnityEngine;
using static DebugStats;

abstract class PlayerPower: MonoBehaviour {
    /// <summary> Whether to automatically handle mana consumption on casting. 
    /// If false, mana will not be checked before considering to call POWER_Cast(). 
    /// You may call ConsumeMana() yourself within the implementation of a power. </summary>
    public bool  autoConsumeMana = true;
    public float manaCost        = 20f;
    public float cooldownSec     = 0f;

    public bool isBeingCast = false;

    public (bool success, string reason) RequestCast() {
        if (autoConsumeMana) {
            var magicSystem = Player.Instance?.magicSystem;
            if (!magicSystem) return (false, "no magic system");

            bool success = magicSystem?.ConsumeMana(manaCost) ?? false;
            if (!success) return (false, $"failed to consume mana (need {manaCost}, have {magicSystem.getMana()})");
        }

        // TODO: check cooldown!

        var result = POWER_Cast();
        return result;
    }
    public bool RequestCancel() {
        bool result = POWER_Cancel();
        if (result) isBeingCast = false;
        return result;
    }

    public PlayerMagicSystem GetMagicSystem() => Player.Instance?.magicSystem;
    
    public float GetMana()               => Player.Instance?.magicSystem?.getMana()             ?? 0f;
    public bool  TestMana()              => Player.Instance?.magicSystem?.TestMana(manaCost)    ?? false;
    public bool  TestMana(float mana)    => Player.Instance?.magicSystem?.TestMana(mana)        ?? false;
    public bool  ConsumeMana()           => Player.Instance?.magicSystem?.ConsumeMana(manaCost) ?? false;
    public bool  ConsumeMana(float mana) => Player.Instance?.magicSystem?.ConsumeMana(mana)     ?? false;
    public void  PlayEmptyManaSFX()      => Player.Instance?.magicSystem?.PlayEmptyManaSFX();

    public abstract (bool success, string reason) POWER_Cast();
    public abstract bool POWER_Cancel();
}