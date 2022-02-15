using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimPlayerModels.Loaders
{
    public class ValavtrLoader : AvatarLoaderBase
    {
        private AssetBundle avatarBundle;

        public override IEnumerator LoadFile(string file)
        {
            AssetBundleCreateRequest bundleRequest =
                AssetBundle.LoadFromFileAsync(file);
            yield return bundleRequest;

            avatarBundle = bundleRequest.assetBundle;
            if (!avatarBundle)
            {
                Debug.LogError("Avatar Bundle " + file + " couldn't load!");
                LoadedSuccessfully = false;
                yield break;
            }

            LoadedSuccessfully = true;
        }

        public override bool LoadAvatar(PlayerModel playerModel)
        {
            GameObject avatarAsset = avatarBundle.LoadAsset<GameObject>("_avatar");
            if (!avatarAsset)
            {
                Debug.LogError("Couldn't find avatar prefab");
                return false;
            }

            AvatarObject = Object.Instantiate(avatarAsset);
            AvatarDescriptor = AvatarObject.GetComponent<ValheimAvatarDescriptor>();
            Animator = AvatarObject.GetComponent<Animator>();

            Transform = AvatarObject.transform;
            Transform.SetParent(playerModel.transform, false);

            LeftFoot = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            RightFoot = Animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Hips = Animator.GetBoneTransform(HumanBodyBones.Hips);

            #region Convert Material Shaders

            Renderer[] renderers = AvatarObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat && mat.shader.name == "Valheim/Standard")
                    {
                        mat.shader = Shader.Find("Custom/Player");

                        var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") as Texture2D : null;
                        var bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                        mat.SetTexture("_MainTex", mainTex);
                        mat.SetTexture("_SkinBumpMap", bumpMap);
                        mat.SetTexture("_ChestTex", mainTex);
                        mat.SetTexture("_ChestBumpMap", bumpMap);
                        mat.SetTexture("_LegsTex", mainTex);
                        mat.SetTexture("_LegsBumpMap", bumpMap);
                    }
                }
            }

            #endregion

            #region Import Parameters

            AvatarDescriptor.Validate();

            Parameters = new Dictionary<int, AvatarLoaderBase.AvatarParameter>();

            if (AvatarDescriptor.boolParameters != null)
            {
                for (int i = 0; i < AvatarDescriptor.boolParameters.Count; i++)
                {
                    int hash = Animator.StringToHash(AvatarDescriptor.boolParameters[i]);
                    if (!Parameters.ContainsKey(hash))
                    {
                        Parameters.Add(hash, new AvatarLoaderBase.AvatarParameter { type = AvatarLoaderBase.ParameterType.Bool, boolValue = AvatarDescriptor.boolParametersDefault[i] });
                        Animator.SetBool(hash, AvatarDescriptor.boolParametersDefault[i]);
                    }
                }
            }

            if (AvatarDescriptor.intParameters != null)
            {
                for (int i = 0; i < AvatarDescriptor.intParameters.Count; i++)
                {
                    int hash = Animator.StringToHash(AvatarDescriptor.intParameters[i]);
                    if (!Parameters.ContainsKey(hash))
                    {
                        Parameters.Add(hash, new AvatarLoaderBase.AvatarParameter { type = AvatarLoaderBase.ParameterType.Int, intValue = AvatarDescriptor.intParametersDefault[i] });
                        Animator.SetInteger(hash, AvatarDescriptor.intParametersDefault[i]);
                    }
                }
            }

            if (AvatarDescriptor.floatParameters != null)
            {
                for (int i = 0; i < AvatarDescriptor.floatParameters.Count; i++)
                {
                    int hash = Animator.StringToHash(AvatarDescriptor.floatParameters[i]);
                    if (!Parameters.ContainsKey(hash))
                    {
                        Parameters.Add(hash, new AvatarLoaderBase.AvatarParameter { type = AvatarLoaderBase.ParameterType.Float, floatValue = AvatarDescriptor.floatParametersDefault[i] });
                        Animator.SetFloat(hash, AvatarDescriptor.floatParametersDefault[i]);
                    }
                }
            }

            #endregion

            #region Load Menu

            MenuControls = new List<AvatarLoaderBase.MenuControl>();

            if (AvatarDescriptor.controlName != null)
            {
                for (int i = 0; i < AvatarDescriptor.controlName.Length; i++)
                {
                    MenuControls.Add(new AvatarLoaderBase.MenuControl
                    {
                        name = AvatarDescriptor.controlName[i],
                        type = AvatarDescriptor.controlTypes[i],
                        parameter = AvatarDescriptor.controlParameterNames[i],
                        value = AvatarDescriptor.controlValues[i]
                    });
                }
            }

            #endregion

            return true;
        }

        public override void Unload()
        {
            if (avatarBundle) avatarBundle.Unload(true);
        }

        public override void Destroy()
        {
            Object.Destroy(AvatarObject);
        }
    }
}