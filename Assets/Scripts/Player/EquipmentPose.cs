using Rimbara.Interaction;
using UnityEngine;

namespace Rimbara.Player
{
    [RequireComponent(typeof(Animator))]
    public sealed class EquipmentPose : MonoBehaviour
    {
        [SerializeField] private Transform handGrip;
        [SerializeField] private WaterSprayer water;
        private Animator animator;

        public void Configure(Transform grip, WaterSprayer sprayer) { handGrip = grip; water = sprayer; }
        private void Awake() { animator = GetComponent<Animator>(); }

        private void OnAnimatorIK(int layer)
        {
            float weight = water != null && water.Spraying ? 1f : 0f;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);
            if (weight <= 0f || handGrip == null) return;
            animator.SetIKPosition(AvatarIKGoal.RightHand, handGrip.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, handGrip.rotation);
        }
    }
}
