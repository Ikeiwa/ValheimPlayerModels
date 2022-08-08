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
using ValheimPlayerModels.Loaders;

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

        public static PlayerModel localModel;
        public string selectedAvatar = "";
        public Player player { get; private set; }
        public ZNetView zNetView { get; private set; }
        private VisEquipment visEquipment;
        private GameObject ogVisual;
        private int selectedHash = Animator.StringToHash("SelectedCustomAvatar");

        public AvatarInstance avatar { get; private set; }
        private bool playerModelVisible = true;
        public bool playerModelLoaded { get; private set; }

        private Animator ogAnimator;
        private List<AttachTransform> ogAttachments;
        private HumanPoseHandler ogPose;
        private HumanPoseHandler pmPose;
        private HumanPose pose = new HumanPose();
        
        private float footOffset;
        private bool dead = false;
        private bool requestHide;

        public bool enableTracking = true;

        #region Unity Events

        private void Awake()
        {
            player = GetComponent<Player>();
            visEquipment = GetComponent<VisEquipment>();
            zNetView = GetComponent<ZNetView>();
            ogVisual = player.m_animator.gameObject;

            ogAnimator = player.m_animator;
            ogAnimator.keepAnimatorControllerStateOnDisable = true;
            ogAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            StartCoroutine(LoadAvatar());
        }

        private void OnDestroy()
        {
            if (localModel == this)
            {
                Plugin.showActionMenu = false;
                Plugin.showAvatarMenu = false;
                Plugin.ResetCursor();
            }
            RemoveAvatar(false);
            StopAllCoroutines();
        }

        private void Update()
        {
            
        }

        private void FixedUpdate()
        {
            if (Game.instance != null && playerModelLoaded && playerModelVisible && !dead && zNetView.IsValid())
            {
                if (!zNetView.IsOwner())
                {
                    ZDO zdo = zNetView.GetZDO();
                    foreach (KeyValuePair<int, AvatarInstance.AvatarParameter> avatarParameter in avatar.Parameters)
                    {
                        switch (avatarParameter.Value.type)
                        {
                            case AvatarInstance.ParameterType.Bool:
                                bool boolValue = zdo.GetBool(438569 + avatarParameter.Key);
                                avatar.Animator.SetBool(avatarParameter.Key, boolValue);
                                break;
                            case AvatarInstance.ParameterType.Int:
                                int intValue = zdo.GetInt(438569 + avatarParameter.Key);
                                avatar.Animator.SetInteger(avatarParameter.Key, intValue);
                                break;
                            case AvatarInstance.ParameterType.Float:
                                float floatValue = zdo.GetFloat(438569 + avatarParameter.Key);
                                avatar.Animator.SetFloat(avatarParameter.Key, floatValue);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    string newAvatar = zdo.GetString(selectedHash, null);
                    if (newAvatar != null && newAvatar != selectedAvatar)
                    {
                        selectedAvatar = newAvatar;
                        ReloadAvatar();
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
                    avatar.Transform.localPosition = Vector3.zero;
                    ogPose.GetHumanPose(ref pose);
                    pmPose.SetHumanPose(ref pose);

                    Transform ogHips = ogAnimator.GetBoneTransform(HumanBodyBones.Hips);

                    avatar.Hips.position = new Vector3(avatar.Hips.position.x, ogHips.position.y, avatar.Hips.position.z);

                    float groundOffset = Mathf.Min(avatar.LeftFoot.position.y - avatar.Transform.position.y, avatar.RightFoot.position.y - avatar.Transform.position.y, 0);

                    avatar.Hips.Translate(0, -groundOffset + footOffset, 0, Space.World);
                }

                foreach (AttachTransform attachTransform in ogAttachments)
                {
                    if (attachTransform.pmAttach != null)
                    {
                        attachTransform.ogAttach.position = attachTransform.pmAttach.position;
                        attachTransform.ogAttach.rotation = attachTransform.pmAttach.rotation;
                        attachTransform.ogAttach.localScale =
                            Vector3.Scale(attachTransform.ogScale, attachTransform.pmAttach.localScale);
                    } 
                }

                if (requestHide)
                {
                    ToggleEquipments();
                }

                if (player.IsDead() && !dead)
                {
                    dead = true;
                    if (!PluginConfig.enableCustomRagdoll.Value)
                        Hide();
                }
            }
        }

        #endregion

        #region Utility Methods

        #endregion

        #region avatar methods

        IEnumerator LoadAvatar()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Plugin.RefreshBundlePaths();

            #region Get Selected Avatar

            if (player == Player.m_localPlayer || Game.instance == null)
            {
                selectedAvatar = PluginConfig.selectedAvatar.Value;
                localModel = this;
            }

            if (zNetView != null && zNetView.IsValid() && !zNetView.IsOwner() && Game.instance != null)
            {
                int tries = 0;
                string syncedAvatar = null;
                while (syncedAvatar == null && tries < 100)
                {
                    syncedAvatar = zNetView.GetZDO().GetString(selectedHash, null);
                    tries++;
                    yield return new WaitForSecondsRealtime(0.05f);
                }

                if (syncedAvatar != null)
                    selectedAvatar = syncedAvatar;
            }

            if (string.IsNullOrEmpty(selectedAvatar))
            {
                #region Get Player Name

                if (Game.instance != null)
                {
                    selectedAvatar = player.GetPlayerName();
                    if (selectedAvatar == "" || selectedAvatar == "...") selectedAvatar = Game.instance.GetPlayerProfile().GetName();
                }
                else
                {
                    var index = FejdStartup.instance.GetField<FejdStartup, int>("m_profileIndex");
                    var profiles = FejdStartup.instance.GetField<FejdStartup, List<PlayerProfile>>("m_profiles");
                    if (index >= 0 && index < profiles.Count) selectedAvatar = profiles[index].GetName();
                }
                selectedAvatar = selectedAvatar.ToLower();

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
                                tries++;
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
                                selectedAvatar = playerId;
                            }
                        }
                    }
                }

                #endregion
            }

            if (zNetView != null && zNetView.IsValid() && zNetView.IsOwner() && Game.instance != null)
            {
                zNetView.GetZDO().Set(selectedHash,selectedAvatar);
            }

            #endregion

            #region Load Asset Bundle

            Debug.Log("Loading " + selectedAvatar + " avatar");

            if (!Plugin.playerModelBundlePaths.ContainsKey(selectedAvatar))
            {
                Debug.LogError("Bundle list doesn't contain bundle for : " + selectedAvatar);
                Destroy(this);
                yield break;
            }

            AvatarLoaderBase loader;

            if (Plugin.playerModelBundleCache.ContainsKey(selectedAvatar))
            {
                while (Plugin.playerModelBundleCache[selectedAvatar] == null)
                { yield return null; }

                loader = Plugin.playerModelBundleCache[selectedAvatar];
            }
            else
            {
                if (!Plugin.playerModelBundleCache.ContainsKey(selectedAvatar))
                    Plugin.playerModelBundleCache.Add(selectedAvatar, null);

                string avatarFile = Plugin.playerModelBundlePaths[selectedAvatar];

                switch (Path.GetExtension(avatarFile).ToLower())
                {
                    case ".valavtr": loader = new ValavtrLoader(); break;
                    case ".vrm": loader = new VrmLoader(); break;
                    default:
                        Destroy(this);
                        yield break;
                }
                
                yield return StartCoroutine(loader.LoadFile(avatarFile));

                if (!loader.LoadedSuccessfully)
                {
                    Plugin.playerModelBundleCache.Remove(selectedAvatar);
                    Destroy(this);
                }

                Plugin.playerModelBundleCache[selectedAvatar] = loader;
            }

            #endregion

            avatar = loader.LoadAvatar(this);

            if (avatar == null)
                Destroy(this);

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
            avatar.Animator.applyRootMotion = true;
            avatar.Animator.updateMode = ogAnimator.updateMode;
            avatar.Animator.feetPivotActive = ogAnimator.feetPivotActive;
            avatar.Animator.layersAffectMassCenter = ogAnimator.layersAffectMassCenter;
            avatar.Animator.stabilizeFeet = ogAnimator.stabilizeFeet;

            if (ogPose == null) ogPose = new HumanPoseHandler(ogAnimator.avatar, ogAnimator.transform);
            if (pmPose == null) pmPose = new HumanPoseHandler(avatar.Animator.avatar, avatar.Animator.transform);

            footOffset = ((avatar.LeftFoot.position.y - avatar.Transform.position.y) +
                          (avatar.RightFoot.position.y - avatar.Transform.position.y)) / 2.0f;

            ogAttachments = new List<AttachTransform>();
            SetAttachParent(visEquipment.m_backAtgeir, avatar.AvatarDescriptor.backAtgeir);
            SetAttachParent(visEquipment.m_backBow, avatar.AvatarDescriptor.backBow);
            SetAttachParent(visEquipment.m_backMelee, avatar.AvatarDescriptor.backMelee);
            SetAttachParent(visEquipment.m_backShield, avatar.AvatarDescriptor.backShield);
            SetAttachParent(visEquipment.m_backTool, avatar.AvatarDescriptor.backTool);
            SetAttachParent(visEquipment.m_backTwohandedMelee, avatar.AvatarDescriptor.backTwohandedMelee);
            SetAttachParent(visEquipment.m_helmet, avatar.AvatarDescriptor.helmet);
            SetAttachParent(visEquipment.m_leftHand, avatar.AvatarDescriptor.leftHand);
            SetAttachParent(visEquipment.m_rightHand, avatar.AvatarDescriptor.rightHand);

            ToggleAvatar();
        }

        public void ToggleAvatar(bool visible = true)
        {
            playerModelVisible = visible;
            avatar.Animator?.gameObject.SetActive(visible);

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in ogVisual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.forceRenderingOff = visible;
                skinnedMeshRenderer.updateWhenOffscreen = true;
            }

            ToggleEquipments(!visible);
        }

        public void Hide()
        {
            avatar.Animator.gameObject.SetActive(false);
        }

        public void Show()
        {
            if(avatar.Animator)
                avatar.Animator.gameObject.SetActive(true);
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
                visEquipment.m_helmetItemInstance?.SetActive(visible || avatar.AvatarDescriptor.showHelmet);

                if(visEquipment.m_shoulderItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_shoulderItemInstances)
                    {
                        if (visEquipment.m_shoulderItem.ToLower().Contains("cape"))
                        {
                            itemInstance?.SetActive(visible || avatar.AvatarDescriptor.showCape);
                            foreach (SkinnedMeshRenderer skinnedMeshRenderer in itemInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {
                                skinnedMeshRenderer.forceRenderingOff = false;
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
                        attachTransform.ogAttach.localScale = attachTransform.ogScale;
                    }

                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in ogVisual.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        skinnedMeshRenderer.forceRenderingOff = false;
                        skinnedMeshRenderer.updateWhenOffscreen = true;
                    }
                }

                requestHide = false;
            }
        }

        public void SetupRagdoll(Ragdoll ragdoll)
        {
            ragdoll.gameObject.AddComponent<CustomRagdoll>().Setup(ogAnimator,avatar.Animator);
        }

        public void RemoveAvatar(bool forced = false)
        {
            if ((!PluginConfig.enableCustomRagdoll.Value || forced) && avatar != null) Destroy(avatar.AvatarObject);
            avatar = null;
            if (ogPose != null)
            {
                ogPose.Dispose();
                ogPose = null;
            }

            if (pmPose != null)
            {
                pmPose.Dispose();
                pmPose = null;
            }
            playerModelLoaded = false;
        }

        public void ReloadAvatar()
        {
            if (playerModelLoaded)
            {
                ToggleAvatar(false);
                RemoveAvatar(true);
                StartCoroutine(LoadAvatar());
            }
        }

        #endregion
    }
}
#endif