using UnityEngine;

using static CoreSystem.Logging;

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
        STATS_PrintLine($"health: {currentHealth}");
    }

    void LateUpdate() {
        UPDATE_PrintStats();
    }
}
