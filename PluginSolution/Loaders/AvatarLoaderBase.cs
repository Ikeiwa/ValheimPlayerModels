using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimPlayerModels.Loaders
{
    public abstract class AvatarLoaderBase
    {
        public enum ParameterType
        {
            Bool,
            Int,
            Float
        }

        public class AvatarParameter
        {
            public ParameterType type;
            public bool boolValue;
            public int intValue;
            public float floatValue;
        }

        public class MenuControl
        {
            public string name;
            public ControlType type;
            public string parameter;
            public float value;
        }

        public bool LoadedSuccessfully { get; protected set; }
        public GameObject AvatarObject { get; protected set; }
        public Transform Transform { get; protected set; }
        public Transform LeftFoot { get; protected set; }
        public Transform RightFoot { get; protected set; }
        public Transform Hips { get; protected set; }
        public Animator Animator { get; protected set; }
        public ValheimAvatarDescriptor AvatarDescriptor { get; protected set; }
        public Dictionary<int, AvatarParameter> Parameters { get; protected set; }
        public List<MenuControl> MenuControls { get; protected set; }

        public abstract IEnumerator LoadFile(string file);
        public abstract bool LoadAvatar(PlayerModel playerModel);
        public abstract void Unload();
        public abstract void Destroy();
    }
}
