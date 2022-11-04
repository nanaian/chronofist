using UnityEngine;
using General;

namespace Health {
    [RequireComponent(typeof(Rigidbody2D))]
    public class Health : MonoBehaviour {
        public float health = 100f;
        public float maxHealth = 100f;
        public float minHealth = 0f;

        public bool isDead => health <= minHealth;

        public event Util.DFloat OnTakeDamage;
        public event Util.DFloat OnHeal;
        public event Util.DVoid OnFullHealth;
        public event Util.DVoid OnDeath;

        public void Start() {
            if (GetComponent<DeathHandler>() == null) {
                Debug.LogWarning($"'{gameObject.name}' has Health but no DeathHandler");
            }
        }

        public void ApplyDamage(float damage) {
            if (isDead) {
                return;
            }

            health -= damage;
            OnTakeDamage?.Invoke(damage);

            if (health <= minHealth) {
                health = minHealth;
                OnDeath?.Invoke();
            }
        }

        public void Kill() {
            ApplyDamage(health);
        }

        public void Heal(float heal, bool allowRevive) {
            if (isDead && !allowRevive) {
                return;
            }

            health += heal;
            OnHeal?.Invoke(heal);

            if (health >= maxHealth) {
                health = maxHealth;
                OnFullHealth?.Invoke();
            }
        }

        public void OnGUI() {
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);

            GUI.Label(
                new Rect(screenPos.x, Screen.height - screenPos.y, 0, 0),
                $"{health}/{maxHealth}",
                new GUIStyle() {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() {
                        textColor = isDead ? Color.red : Color.green
                    }
                }
            );
        }
    }
}