using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimPlayerModels
{
    public class CustomRagdoll : MonoBehaviour
    {
        private Ragdoll ragdoll;
        private Animator ragdollAnimator;
        private Animator avatarAnimator;

        public void Setup(Animator ogAnimator, Animator avatarAnimator)
        {
            if(!avatarAnimator) Destroy(this);

            foreach (var renderer in transform.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
            avatarAnimator.transform.SetParent(transform);
            avatarAnimator.transform.localPosition = Vector3.zero;
            avatarAnimator.transform.localRotation = Quaternion.identity;

            ragdoll = GetComponent<Ragdoll>();
            this.avatarAnimator = avatarAnimator;
            ragdollAnimator = ragdoll.gameObject.transform.GetChild(0).gameObject.AddComponent<Animator>();
            ragdollAnimator.avatar = ogAnimator.avatar;

            
        }

        private void LateUpdate()
        {
            avatarAnimator.GetBoneTransform(HumanBodyBones.Hips).position =
                ragdollAnimator.GetBoneTransform(HumanBodyBones.Hips).position;

            for (var i = 0; i < 55; i++)
            {
                var ogTransform = ragdollAnimator.GetBoneTransform((HumanBodyBones)i);
                var pmTransform = avatarAnimator.GetBoneTransform((HumanBodyBones)i);

                if (ogTransform && pmTransform)
                {
                    pmTransform.rotation = ogTransform.rotation;
                }
            }
        }
    }
}
