using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValheimPlayerModels
{
    public enum ControlType
    {
        Button,
        Toggle,
        Slider
    }

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
        [Space] 
        public bool showHelmet;
        public bool showCape;
        [Header("Parameters")] 
        [Space] 
        public List<string> boolParameters;
        public List<bool> boolParametersDefault;
        public List<string> intParameters;
        public List<int> intParametersDefault;
        public List<string> floatParameters;
        public List<float> floatParametersDefault;
        [Header("Menu")] 
        public ControlType[] controlTypes;
        public string[] controlParameterNames;
        public float[] controlValues;

        private void Awake()
        {
            Validate();
        }

        public void Validate()
        {
            for (int i = 0; i < boolParameters.Count; i++)
            {
                if (!boolParameters[i].StartsWith("param_"))
                    boolParameters[i] = "param_" + boolParameters[i];
            }

            for (int i = 0; i < intParameters.Count; i++)
            {
                if (!intParameters[i].StartsWith("param_"))
                    intParameters[i] = "param_" + intParameters[i];
            }

            for (int i = 0; i < floatParameters.Count; i++)
            {
                if (!floatParameters[i].StartsWith("param_"))
                    floatParameters[i] = "param_" + floatParameters[i];
            }

            if (boolParametersDefault.Count != boolParameters.Count)
                boolParametersDefault.Resize(boolParameters.Count);

            if (intParametersDefault.Count != intParameters.Count)
                intParametersDefault.Resize(intParameters.Count);

            if (floatParametersDefault.Count != floatParameters.Count)
                floatParametersDefault.Resize(floatParameters.Count);

            if (controlParameterNames.Length != controlTypes.Length)
                Array.Resize(ref controlParameterNames, controlTypes.Length);

            if (controlValues.Length != controlTypes.Length)
                Array.Resize(ref controlValues, controlTypes.Length);
        }

        void OnValidate()
        {
            Validate();
        }
    }
}