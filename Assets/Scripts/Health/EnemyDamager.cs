using UnityEngine;
using Enemy;
using Effects;

namespace Health {
    public class EnemyDamager : MonoBehaviour {
        public float damage = 1f;
        public Vector2 knockback = Vector2.zero;
        public float radius = 1f;
        public int attemptsUntilDestroy = 0; // 0 = infinite

        public void FixedUpdate() {
            int numHits = 0;

            foreach (BaseEnemy enemy in BaseEnemy.enemies) {
                // TODO: do a real collision check; this currently just checks the middle of the enemy
                if (Vector2.Distance(enemy.transform.position, transform.position) <= radius) {
                    Hit(enemy);
                    numHits++;
                }
            }

            // Freeze frame effect on hit
            if (numHits > 0) {
                Effects.TimeEffect.Spawn(0.1f, 0.12f);
            }

            if (attemptsUntilDestroy > 0) {
                attemptsUntilDestroy--;
                if (attemptsUntilDestroy == 0)
                    Destroy(gameObject);
            }
        }

        public void Hit(BaseEnemy enemy) {
            if (enemy.TryResetCooldown()) {
                enemy.ApplyKnockback(knockback * transform.lossyScale);
                enemy.GetHealth()?.ApplyDamage(damage);
            }
        }

        public void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawRay(transform.position, knockback * transform.lossyScale);
        }
    }
}