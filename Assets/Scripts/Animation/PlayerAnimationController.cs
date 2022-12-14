using Physics;
using UnityEngine;
using Anim = Animation.AnimationManagerMc.McAnimation;
using Combat;

namespace Animation {
    [RequireComponent(typeof(AnimationManagerMc)), RequireComponent(typeof(Player))]
    public class PlayerAnimationController : MonoBehaviour {
        private Anim anim;
        private AnimationManagerMc animManager;
        [SerializeField] private HitboxManager hitbox;
        private Player player;

        private void Awake() {
            animManager = GetComponent<AnimationManagerMc>();
            player = GetComponent<Player>();
        }

        private void LateUpdate() {
            var timeMultiplier = LocalTime.MultiplierAt(transform.position);

            // Set animation speed to local time
            animManager.SetSpeed(timeMultiplier);

            // Don't update animation if time is low
            if (timeMultiplier < 0.01f) {
                return;
            }

            var previousAnim = anim;

            if (player.GetAttackType() == Player.AttackType.DashForward) {
                anim = Anim.DashForward;
            } else if (player.GetAttackType() == Player.AttackType.DashBackward) {
                anim = Anim.DashBackward;
            } else if (player.GetAttackType() == Player.AttackType.Jab1) {
                anim = player.IsAirbourne() ? Anim.AirPunch1 : Anim.Punch1;
            } else if (player.GetAttackType() == Player.AttackType.Jab2) {
                anim = player.IsAirbourne() ? Anim.AirPunch2 : Anim.Punch2;
            } else if (player.GetAttackType() == Player.AttackType.Jab3) {
                anim = player.IsAirbourne() ? Anim.AirPunch3 : Anim.Punch3;
            } else if (player.GetAttackType() == Player.AttackType.Uppercut) {
                anim = Anim.Uppercut;
            } else if (player.GetAttackType() == Player.AttackType.Slam) {
                anim = Anim.Slam;
            } else if (player.IsWallPushing()) {
                anim = Anim.Push;
            } else if (player.IsWallSliding()) {
                anim = Anim.WallCling;
            } else if (player.IsJumping()) {
                anim = Anim.Jump;
            } else if (player.IsFalling()) {
                anim = Anim.Fall;
            } else if (player.IsSlidng()) {
                anim = Anim.Slide;
            } else if (player.IsMovingHorizontally()) {
                anim = Anim.Run;
            } else {
                anim = Anim.Idle;
            }

            if (anim != previousAnim) {
                hitbox.Restore();
                animManager.PlayAnimation(anim);
            }

            // Flip sprite if facing left
            var scale = transform.localScale;
            scale.x = player.IsFacingLeft() ? -Mathf.Abs(transform.localScale.x) : Mathf.Abs(transform.localScale.x);
            transform.localScale = scale;

            //spriteRenderer.transform.localPosition = new Vector3(player.IsFacingLeft() ? Util.PIXEL * -0.5f : 0f, 0f, 0f);
        }
    }
}
