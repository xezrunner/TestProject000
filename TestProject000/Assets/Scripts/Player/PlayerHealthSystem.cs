using UnityEngine;

using static DebugStats;

public class PlayerHealthSystem : MonoBehaviour {
    public int maxHealth = 100;
    public int currentHealth;

    void Start() {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage) {
        currentHealth -= damage;
        if (currentHealth <= 0) {
            // TODO: Death
            Debug.LogWarning("Player death");
        }
    }

    void UPDATE_PrintStats() {
        STATS_SectionStart("Health system");

        STATS_SectionPrintLine($"health: {currentHealth}");

        STATS_SectionEnd();
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}
