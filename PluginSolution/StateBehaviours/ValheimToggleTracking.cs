using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimPlayerModels
{
    public class ValheimToggleTracking : StateMachineBehaviour
    {
        public bool enableTracking = true;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
#if PLUGIN
            PlayerModel playerModel = animator.transform.parent.GetComponent<PlayerModel>();
            if (playerModel) playerModel.enableTracking = enableTracking;
#endif
        }
    }
}
