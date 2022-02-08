using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValheimPlayerModels
{
    public class ValheimAvatarDescriptor : MonoBehaviour
    {
        [Header("Infos")] public string avatarName = "player";
        [Header("Attachment points")] public Transform leftHand;
        public Transform rightHand;
        public Transform helmet;
        public Transform backShield;
        public Transform backMelee;
        public Transform backTwohandedMelee;
        public Transform backBow;
        public Transform backTool;
        public Transform backAtgeir;
    }
}