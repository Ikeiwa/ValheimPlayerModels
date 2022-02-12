#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;

namespace ValheimPlayerModels
{
    [DefaultExecutionOrder(int.MaxValue-1)]
    public class PlayerModel : MonoBehaviour
    {
        public class AttachTransform
        {
            public Transform ogAttach;
            public Transform pmAttach;
            public Vector3 ogPosition;
            public Quaternion ogRotation;
            public Vector3 ogScale;
        }

        public Player player { get; private set; }
        private VisEquipment visEquipment;
        private GameObject ogVisual;
        private ZNetView zNetView;

        private AssetBundle avatarBundle;
        private GameObject avatarObject;
        private bool playerModelVisible = true;
        public bool playerModelLoaded { get; private set; }

        private Animator ogAnimator;
        private List<AttachTransform> ogAttachments;
        private HumanPoseHandler ogPose;
        private HumanPoseHandler pmPose;
        private HumanPose pose = new HumanPose();

        private Transform pmTranform;
        private Transform pmLeftFoot;
        private Transform pmRightFoot;
        private Transform pmHips;
        private float footOffset;

        private ValheimAvatarDescriptor avatarDescriptor;
        private Animator pmAnimator;

        private int lastEquipedCount = 0;
        private bool dead = false;
        private bool requestHide;

        private Dictionary<int, bool> boolParameters;
        private Dictionary<int, int> intParameters;
        private Dictionary<int, float> floatParameters;

        public bool enableTracking = true;

        private void Awake()
        {
            player = GetComponent<Player>();
            visEquipment = GetComponent<VisEquipment>();
            zNetView = GetComponent<ZNetView>();
            ogVisual = player.m_animator.gameObject;

            ogAnimator = player.m_animator;
            ogAnimator.keepAnimatorControllerStateOnDisable = true;
            ogAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ogAttachments = new List<AttachTransform>();

            StartCoroutine(LoadAvatar());
        }

        public void Unload()
        {
            if(avatarBundle) avatarBundle.Unload(true);
        }

        private void OnDestroy()
        {
            if(!PluginConfig.enableCustomRagdoll.Value && avatarObject) Destroy(avatarObject);
            if (ogPose != null) ogPose.Dispose();
            if (pmPose != null) pmPose.Dispose();
            StopAllCoroutines();
            //if(avatarBundle) avatarBundle.Unload(true);
        }

        private void FixedUpdate()
        {
            if (playerModelLoaded && playerModelVisible && !dead && zNetView.IsValid())
            {
                ZDO zdo = zNetView.GetZDO();
                if (!zNetView.IsOwner())
                {
                    foreach (KeyValuePair<int, bool> boolParameter in boolParameters)
                    {
                        bool boolValue = zdo.GetBool(438569 + boolParameter.Key);
                        pmAnimator.SetBool(boolParameter.Key,boolValue);
                    }
                    foreach (KeyValuePair<int, int> intParameter in intParameters)
                    {
                        int intValue = zdo.GetInt(438569 + intParameter.Key);
                        pmAnimator.SetInteger(intParameter.Key, intValue);
                    }
                    foreach (KeyValuePair<int, float> floatParameter in floatParameters)
                    {
                        float floatValue = zdo.GetFloat(438569 + floatParameter.Key);
                        pmAnimator.SetFloat(floatParameter.Key, floatValue);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (playerModelLoaded && playerModelVisible && !dead)
            {
                if (enableTracking)
                {
                    pmTranform.localPosition = Vector3.zero;
                    ogPose.GetHumanPose(ref pose);
                    pmPose.SetHumanPose(ref pose);

                    Transform ogHips = ogAnimator.GetBoneTransform(HumanBodyBones.Hips);

                    pmHips.position = new Vector3(pmHips.position.x, ogHips.position.y, pmHips.position.z);

                    float groundOffset = Mathf.Min(pmLeftFoot.position.y - pmTranform.position.y, pmRightFoot.position.y - pmTranform.position.y, 0);

                    pmHips.Translate(0, -groundOffset + footOffset, 0, Space.World);
                }

                foreach (AttachTransform attachTransform in ogAttachments)
                {
                    attachTransform.ogAttach.position = attachTransform.pmAttach.position;
                    attachTransform.ogAttach.rotation = attachTransform.pmAttach.rotation;
                    attachTransform.ogAttach.localScale =
                        Vector3.Scale(attachTransform.ogScale, attachTransform.pmAttach.localScale);
                }

                if (requestHide)
                {
                    ToggleEquipments();
                }

                if (player.IsDead() && !dead)
                {
                    dead = true;
                    if(!PluginConfig.enableCustomRagdoll.Value)
                        Hide();
                }
            }
        }

        IEnumerator LoadAvatar()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Plugin.RefreshBundlePaths();

            #region Get Player Name

            string playerName = "";
            if (Game.instance != null)
            {
                playerName = player.GetPlayerName();
                if (playerName == "" || playerName == "...") playerName = Game.instance.GetPlayerProfile().GetName();
            }
            else
            {
                var index = FejdStartup.instance.GetField<FejdStartup, int>("m_profileIndex");
                var profiles = FejdStartup.instance.GetField<FejdStartup, List<PlayerProfile>>("m_profiles");
                if (index >= 0 && index < profiles.Count) playerName = profiles[index].GetName();
            }
            playerName = playerName.ToLower();

            #endregion

            #region Get Player Net ID

            string playerId = "";
            if (ZNet.instance != null)
            {
                if (!ZNet.instance.IsServer() && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
                {
                    int tries = 0;
                    do
                    {
                        List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
                        int playerIndex =
                            playerList.FindIndex(p => p.m_characterID.userID == zNetView.m_zdo.m_uid.userID);
                        if (playerIndex != -1)
                        {
                            playerId = playerList[playerIndex].m_host;
                        }
                        else
                        {
                            tries++;
                            yield return null;
                        }
                    } while (string.IsNullOrEmpty(playerId) && tries < 100);

                    if (!string.IsNullOrEmpty(playerId))
                    {
                        if (Plugin.playerModelBundlePaths.ContainsKey(playerId))
                        {
                            playerName = playerId;
                        }
                    }
                }
            }

            #endregion

            #region Load Asset Bundle

            Debug.Log("Loading " + playerName + " avatar");

            if (!Plugin.playerModelBundlePaths.ContainsKey(playerName))
            {
                Debug.LogError("Bundle list doesn't contain bundle for : " + playerName);
                Destroy(this);
                yield break;
            }

            if (Plugin.playerModelBundleCache.ContainsKey(playerName))
            {
                while (Plugin.playerModelBundleCache[playerName] == null)
                { yield return null; }

                avatarBundle = Plugin.playerModelBundleCache[playerName];
            }
            else
            {
                if (!Plugin.playerModelBundleCache.ContainsKey(playerName))
                    Plugin.playerModelBundleCache.Add(playerName, null);

                AssetBundleCreateRequest bundleRequest =
                    AssetBundle.LoadFromFileAsync(Plugin.playerModelBundlePaths[playerName]);
                yield return bundleRequest;

                avatarBundle = bundleRequest.assetBundle;
                if (!avatarBundle)
                {
                    Debug.LogError("Avatar Bundle for " + playerName + " couldn't load!");
                    Destroy(this);
                    yield break;
                }

                Plugin.playerModelBundleCache[playerName] = avatarBundle;
            }

            #endregion

            #region Load Avatar Object

            GameObject avatarAsset = avatarBundle.LoadAsset<GameObject>("_avatar");
            if (!avatarAsset)
            {
                Debug.LogError("Couldn't find avatar prefab for " + playerName);
                Destroy(this);
                yield break;
            }

            avatarObject = Instantiate(avatarAsset);
            avatarDescriptor = avatarObject.GetComponent<ValheimAvatarDescriptor>();
            pmAnimator = avatarObject.GetComponent<Animator>();

            #region Import Parameters

            avatarDescriptor.Validate();

            boolParameters = new Dictionary<int, bool>();
            intParameters = new Dictionary<int, int>();
            floatParameters = new Dictionary<int, float>();

            if (avatarDescriptor.boolParameters != null)
            {
                for (int i = 0; i < avatarDescriptor.boolParameters.Count; i++)
                {
                    int hash = Animator.StringToHash(avatarDescriptor.boolParameters[i]);
                    if (!boolParameters.ContainsKey(hash))
                    {
                        boolParameters.Add(hash, avatarDescriptor.boolParametersDefault[i]);
                        pmAnimator.SetBool(hash, avatarDescriptor.boolParametersDefault[i]);
                    }
                }
            }

            if (avatarDescriptor.intParameters != null)
            {
                for (int i = 0; i < avatarDescriptor.intParameters.Count; i++)
                {
                    int hash = Animator.StringToHash(avatarDescriptor.intParameters[i]);
                    if (!intParameters.ContainsKey(hash))
                    {
                        intParameters.Add(hash, avatarDescriptor.intParametersDefault[i]);
                        pmAnimator.SetInteger(hash, avatarDescriptor.intParametersDefault[i]);
                    }
                }
            }

            if (avatarDescriptor.floatParameters != null)
            {
                for (int i = 0; i < avatarDescriptor.floatParameters.Count; i++)
                {
                    int hash = Animator.StringToHash(avatarDescriptor.floatParameters[i]);
                    if (!floatParameters.ContainsKey(hash))
                    {
                        floatParameters.Add(hash, avatarDescriptor.floatParametersDefault[i]);
                        pmAnimator.SetFloat(hash, avatarDescriptor.floatParametersDefault[i]);
                    }
                }
            }

            #endregion

            #region Convert Material Shaders

            Renderer[] renderers = avatarObject.GetComponentsInChildren<Renderer>();
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

            pmTranform = avatarObject.transform;
            pmTranform.SetParent(transform, false);

            #endregion

            ApplyAvatar();
            playerModelLoaded = true;
        }

        private void SetAttachParent(Transform attach, Transform newAttach)
        {
            ogAttachments.Add(new AttachTransform
            {
                ogAttach = attach,
                pmAttach = newAttach,
                ogPosition = attach.localPosition,
                ogRotation = attach.localRotation,
                ogScale = attach.localScale
            });
        }

        private void ApplyAvatar()
        {
            pmAnimator.applyRootMotion = true;
            pmAnimator.updateMode = ogAnimator.updateMode;
            pmAnimator.feetPivotActive = ogAnimator.feetPivotActive;
            pmAnimator.layersAffectMassCenter = ogAnimator.layersAffectMassCenter;
            pmAnimator.stabilizeFeet = ogAnimator.stabilizeFeet;

            if (ogPose == null) ogPose = new HumanPoseHandler(ogAnimator.avatar, ogAnimator.transform);
            if (pmPose == null) pmPose = new HumanPoseHandler(pmAnimator.avatar, pmAnimator.transform);

            pmLeftFoot = pmAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            pmRightFoot = pmAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            pmHips = pmAnimator.GetBoneTransform(HumanBodyBones.Hips);

            footOffset = ((pmLeftFoot.position.y - pmTranform.position.y) +
                          (pmRightFoot.position.y - pmTranform.position.y)) / 2.0f;

            SetAttachParent(visEquipment.m_backAtgeir, avatarDescriptor.backAtgeir);
            SetAttachParent(visEquipment.m_backBow, avatarDescriptor.backBow);
            SetAttachParent(visEquipment.m_backMelee, avatarDescriptor.backMelee);
            SetAttachParent(visEquipment.m_backShield, avatarDescriptor.backShield);
            SetAttachParent(visEquipment.m_backTool, avatarDescriptor.backTool);
            SetAttachParent(visEquipment.m_backTwohandedMelee, avatarDescriptor.backTwohandedMelee);
            SetAttachParent(visEquipment.m_helmet, avatarDescriptor.helmet);
            SetAttachParent(visEquipment.m_leftHand, avatarDescriptor.leftHand);
            SetAttachParent(visEquipment.m_rightHand, avatarDescriptor.rightHand);

            ToggleAvatar();
        }

        public void ToggleAvatar(bool visible = true)
        {
            pmAnimator?.gameObject.SetActive(visible);

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in ogVisual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.forceRenderingOff = visible;
                skinnedMeshRenderer.updateWhenOffscreen = true;
            }

            ToggleEquipments(!visible);

            playerModelVisible = visible;
        }

        public void Hide()
        {
            pmAnimator.gameObject.SetActive(false);
        }

        public void Show()
        {
            if(pmAnimator)
                pmAnimator.gameObject.SetActive(true);
        }

        public void ToggleEquipments(bool visible = false)
        {
            if (!playerModelLoaded)
            {
                requestHide = true;
                return;
            }

            if (visEquipment)
            {
                visEquipment.m_beardItemInstance?.SetActive(visible);
                visEquipment.m_hairItemInstance?.SetActive(visible);
                visEquipment.m_helmetItemInstance?.SetActive(visible || avatarDescriptor.showHelmet);

                if(visEquipment.m_shoulderItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_shoulderItemInstances)
                    {
                        if (visEquipment.m_shoulderItem.ToLower().Contains("cape"))
                        {
                            itemInstance?.SetActive(visible || avatarDescriptor.showCape);
                            foreach (SkinnedMeshRenderer skinnedMeshRenderer in itemInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {
                                skinnedMeshRenderer.forceRenderingOff = visible;
                                skinnedMeshRenderer.updateWhenOffscreen = true;
                            }
                        }
                        else itemInstance?.SetActive(visible);
                    }
                if (visEquipment.m_legItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_legItemInstances) { itemInstance?.SetActive(visible); }
                if (visEquipment.m_chestItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_chestItemInstances) { itemInstance?.SetActive(visible); }
                if (visEquipment.m_utilityItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_utilityItemInstances) { itemInstance?.SetActive(visible); }

                if (visible)
                {
                    foreach (AttachTransform attachTransform in ogAttachments)
                    {
                        attachTransform.ogAttach.localPosition = attachTransform.ogPosition;
                        attachTransform.ogAttach.localRotation = attachTransform.ogRotation;
                    }
                }

                requestHide = false;
            }
        }

        public void SetupRagdoll(Ragdoll ragdoll)
        {
            ragdoll.gameObject.AddComponent<CustomRagdoll>().Setup(ogAnimator,pmAnimator);
        }

        #region Animator Params Methods

        public bool SetBool(string name, bool value)
        {
            int hash = Animator.StringToHash(name);
            if (!boolParameters.ContainsKey(hash)) return false;

            boolParameters[hash] = value;
            if (pmAnimator.GetBool(hash) == boolParameters[hash]) return true;

            pmAnimator.SetBool(hash, value);
            if (zNetView.GetZDO() != null && zNetView.IsOwner())
                zNetView.GetZDO().Set(438569 + hash, value);
            return true;
        }

        public bool SetInt(string name, int value)
        {
            int hash = Animator.StringToHash(name);
            if (!intParameters.ContainsKey(hash)) return false;

            intParameters[hash] = value;
            if (pmAnimator.GetInteger(hash) == intParameters[hash]) return true;

            pmAnimator.SetInteger(hash, value);
            if (zNetView.GetZDO() != null && zNetView.IsOwner())
                zNetView.GetZDO().Set(438569 + hash, value);
            return true;
        }

        public bool SetFloat(string name, float value)
        {
            int hash = Animator.StringToHash(name);
            if (!floatParameters.ContainsKey(hash)) return false;

            floatParameters[hash] = value;
            if (Mathf.Abs(floatParameters[hash] - value) < 0.01f) return true;

            pmAnimator.SetFloat(hash, value);
            if (zNetView.GetZDO() != null && zNetView.IsOwner())
                zNetView.GetZDO().Set(438569 + hash, value);
            return true;
        }
        #endregion
    }
}
#endif