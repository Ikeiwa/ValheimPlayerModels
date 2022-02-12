#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;

namespace ValheimPlayerModels
{
    public class CustomRagdoll : MonoBehaviour
    {
        private Ragdoll ragdoll;
        private Animator ragdollAnimator;
        private Animator avatarAnimator;
        private bool hidden = false;

        public void Setup(Animator ogAnimator, Animator avatarAnimator)
        {
            if(!avatarAnimator) Destroy(this);

            avatarAnimator.transform.SetParent(transform);
            avatarAnimator.transform.localPosition = Vector3.zero;
            avatarAnimator.transform.localRotation = Quaternion.identity;

            ragdoll = GetComponent<Ragdoll>();
            this.avatarAnimator = avatarAnimator;
            ragdollAnimator = ragdoll.gameObject.transform.GetChild(0).gameObject.AddComponent<Animator>();
            ragdollAnimator.avatar = ogAnimator.avatar;

            for (var i = 0; i < 55; i++)
            {
                var ogTransform = ragdollAnimator.GetBoneTransform((HumanBodyBones)i);
                var pmTransform = avatarAnimator.GetBoneTransform((HumanBodyBones)i);

                if (ogTransform && pmTransform)
                {
                    RotationConstraint constraint = pmTransform.gameObject.AddComponent<RotationConstraint>();
                    constraint.AddSource(new ConstraintSource {sourceTransform = ogTransform, weight = 1});

                    Quaternion start = Quaternion.identity * Quaternion.Inverse(ogTransform.rotation);
                    Quaternion end = Quaternion.identity * Quaternion.Inverse(pmTransform.rotation);

                    constraint.rotationOffset = (start * Quaternion.Inverse(end)).eulerAngles;
                    constraint.locked = true;
                    constraint.constraintActive = true;

                    if (i == 0)
                    {
                        PositionConstraint posConstraint = pmTransform.gameObject.AddComponent<PositionConstraint>();
                        posConstraint.AddSource(new ConstraintSource { sourceTransform = ogTransform, weight = 1 });
                        posConstraint.translationOffset = pmTransform.position - ogTransform.position;
                        posConstraint.locked = true;
                        posConstraint.constraintActive = true;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (!hidden)
            {
                foreach (var renderer in ragdollAnimator.transform.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }

                hidden = true;
            }
            /*avatarAnimator.GetBoneTransform(HumanBodyBones.Hips).position =
                ragdollAnimator.GetBoneTransform(HumanBodyBones.Hips).position;

            for (var i = 0; i < 55; i++)
            {
                var ogTransform = ragdollAnimator.GetBoneTransform((HumanBodyBones)i);
                var pmTransform = avatarAnimator.GetBoneTransform((HumanBodyBones)i);

                if (ogTransform && pmTransform)
                {
                    pmTransform.rotation = ogTransform.rotation;
                }
            }*/
        }
    }
}
#endif