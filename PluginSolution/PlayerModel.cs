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

        public Player player { get; private set; }
        private VisEquipment visEquipment;
        private GameObject ogVisual;
        private ZNetView zNetView;
        private string selectedAvatar = "";
        private int selectedHash = Animator.StringToHash("SelectedCustomAvatar");

        private AvatarLoaderBase loader;
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

        public static bool showActionMenu { get; private set; }
        public static bool showAvatarMenu { get; private set; }
        private Rect ActionWindowRect;
        private Rect AvatarWindowRect;
        private CursorLockMode oldCursorLockState = CursorLockMode.Confined;
        private bool oldCursorVisible = false;
        private const int ActionWindowId = -48;
        private const int AvatarWindowId = -49;
        private Vector2 actionMenuWindowScrollPos;
        private Vector2 avatarMenuWindowScrollPos;
        private int hasChangedParam = 0;

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
            RemoveAvatar(false);
            showActionMenu = false;
            showAvatarMenu = false;
            ResetCursor();
            StopAllCoroutines();
        }

        private void Update()
        {
            if (PluginConfig.actionMenuKey.Value.IsDown() && !showAvatarMenu)
            {
                if (Player.m_localPlayer == player)
                {
                    showActionMenu = !showActionMenu;
                    if (showActionMenu)
                    {
                        SetUnlockCursor();
                        GUI.FocusWindow(ActionWindowId);
                    }
                    else ResetCursor();
                }
            }

            if (PluginConfig.avatarMenuKey.Value.IsDown() && !showActionMenu)
            {
                if (Player.m_localPlayer == player || Game.instance == null)
                {
                    showAvatarMenu = !showAvatarMenu;
                    if (showAvatarMenu)
                    {
                        SetUnlockCursor();
                        GUI.FocusWindow(AvatarWindowId);
                    }
                    else ResetCursor();
                }
            }
        }

        private void FixedUpdate()
        {
            if (Game.instance != null && playerModelLoaded && playerModelVisible && !dead && zNetView.IsValid())
            {
                if (!zNetView.IsOwner())
                {
                    ZDO zdo = zNetView.GetZDO();
                    foreach (KeyValuePair<int, AvatarLoaderBase.AvatarParameter> avatarParameter in loader.Parameters)
                    {
                        switch (avatarParameter.Value.type)
                        {
                            case AvatarLoaderBase.ParameterType.Bool:
                                bool boolValue = zdo.GetBool(438569 + avatarParameter.Key);
                                loader.Animator.SetBool(avatarParameter.Key, boolValue);
                                break;
                            case AvatarLoaderBase.ParameterType.Int:
                                int intValue = zdo.GetInt(438569 + avatarParameter.Key);
                                loader.Animator.SetInteger(avatarParameter.Key, intValue);
                                break;
                            case AvatarLoaderBase.ParameterType.Float:
                                float floatValue = zdo.GetFloat(438569 + avatarParameter.Key);
                                loader.Animator.SetFloat(avatarParameter.Key, floatValue);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    string newAvatar = zdo.GetString(selectedHash);
                    if (newAvatar != selectedAvatar)
                    {
                        selectedAvatar = newAvatar;
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
                    loader.Transform.localPosition = Vector3.zero;
                    ogPose.GetHumanPose(ref pose);
                    pmPose.SetHumanPose(ref pose);

                    Transform ogHips = ogAnimator.GetBoneTransform(HumanBodyBones.Hips);

                    loader.Hips.position = new Vector3(loader.Hips.position.x, ogHips.position.y, loader.Hips.position.z);

                    float groundOffset = Mathf.Min(loader.LeftFoot.position.y - loader.Transform.position.y, loader.RightFoot.position.y - loader.Transform.position.y, 0);

                    loader.Hips.Translate(0, -groundOffset + footOffset, 0, Space.World);
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
                    if (!PluginConfig.enableCustomRagdoll.Value)
                        Hide();
                }
            }

            if (showActionMenu)
            {
                if(hasChangedParam > 0)
                    hasChangedParam--;
            }
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            if (showActionMenu)
            {
                ActionWindowRect = new Rect(Screen.width, Screen.height, 250, 400);
                ActionWindowRect.x -= ActionWindowRect.width;
                ActionWindowRect.y -= ActionWindowRect.height;

                if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), string.Empty, GUIStyle.none) &&
                    !ActionWindowRect.Contains(Input.mousePosition) || Input.GetKeyDown(KeyCode.Escape))
                {
                    showActionMenu = false;
                    ResetCursor();
                }

                GUI.Box(ActionWindowRect, GUIContent.none);
                GUILayout.Window(ActionWindowId, ActionWindowRect, ActionMenuWindow, "Action Menu");

                Input.ResetInputAxes();
            }

            if (showAvatarMenu)
            {
                AvatarWindowRect = new Rect(20, 100, 250, 400);

                if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), string.Empty, GUIStyle.none) &&
                    !AvatarWindowRect.Contains(Input.mousePosition) || Input.GetKeyDown(KeyCode.Escape))
                {
                    showAvatarMenu = false;
                    ResetCursor();
                }

                GUI.Box(AvatarWindowRect, GUIContent.none);
                GUILayout.Window(AvatarWindowId, AvatarWindowRect, AvatarMenuWindow, "Avatar Menu");

                Input.ResetInputAxes();
            }
        }

        private void ActionMenuWindow(int id)
        {
            actionMenuWindowScrollPos = GUILayout.BeginScrollView(actionMenuWindowScrollPos, false, true);

            var scrollPosition = actionMenuWindowScrollPos.y;
            var scrollHeight = ActionWindowRect.height;

            GUILayout.BeginVertical();

            float controlHeight = 21;
            float currentHeight = 0;

            for (int i = 0; i < loader.MenuControls.Count; i++)
            {
                int paramId = Animator.StringToHash(loader.MenuControls[i].parameter);

                if (string.IsNullOrEmpty(loader.MenuControls[i].name) ||
                    string.IsNullOrEmpty(loader.MenuControls[i].parameter) ||
                    !loader.Parameters.ContainsKey(paramId)) continue;

                var visible = controlHeight == 0 || currentHeight + controlHeight >= scrollPosition && currentHeight <= scrollPosition + scrollHeight;

                if (visible)
                {
                    try
                    {
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        GUILayout.Label(loader.MenuControls[i].name);

                        float parameterValue = GetParameterValue(paramId);

                        switch (loader.MenuControls[i].type)
                        {
                            case ControlType.Button:
                                if (GUILayout.Button("Press"))
                                {
                                    if (parameterValue == 0)
                                    {
                                        SetParameterValue(paramId, loader.MenuControls[i].value);
                                        hasChangedParam = 5;
                                    }
                                }
                                else
                                {
                                    if (parameterValue != 0)
                                    {
                                        if (hasChangedParam <= 0)
                                        {
                                            SetParameterValue(paramId, 0);
                                        }
                                    }
                                }
                                break;
                            case ControlType.Toggle:
                                bool menuToggleValue = parameterValue != 0;

                                bool toggleValue = GUILayout.Toggle(menuToggleValue, string.Empty);
                                if (toggleValue != menuToggleValue)
                                {
                                    SetParameterValue(paramId, toggleValue ? loader.MenuControls[i].value : 0);
                                }
                                break;
                            case ControlType.Slider:

                                float sliderValue = GUILayout.HorizontalSlider(parameterValue, 0.0f, 1.0f);
                                if (Mathf.Abs(sliderValue - parameterValue) > 0.01f)
                                {
                                    SetParameterValue(paramId, sliderValue);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        GUILayout.EndHorizontal();
                    }
                    catch (ArgumentException) { }
                }
                else
                {
                    try
                    {
                        GUILayout.Space(controlHeight);
                    }
                    catch (ArgumentException) { }
                }

                currentHeight += controlHeight;
            }

            GUILayout.Space(70);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void AvatarMenuWindow(int id)
        {
            avatarMenuWindowScrollPos = GUILayout.BeginScrollView(avatarMenuWindowScrollPos, false, true);

            var scrollPosition = avatarMenuWindowScrollPos.y;
            var scrollHeight = AvatarWindowRect.height;

            GUILayout.BeginVertical();

            float controlHeight = 21;
            float currentHeight = 0;

            foreach (string avatar in Plugin.playerModelBundlePaths.Keys)
            {
                var visible = controlHeight == 0 || currentHeight + controlHeight >= scrollPosition && currentHeight <= scrollPosition + scrollHeight;

                if (visible)
                {
                    try
                    {
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        GUILayout.Label(avatar);

                        if (selectedAvatar == avatar)
                            GUI.enabled = false;

                        if (GUILayout.Button("Select"))
                        {
                            PluginConfig.selectedAvatar.BoxedValue = avatar;
                        }

                        GUI.enabled = true;

                        GUILayout.EndHorizontal();
                    }
                    catch (ArgumentException) { }
                }
                else
                {
                    try
                    {
                        GUILayout.Space(controlHeight);
                    }
                    catch (ArgumentException) { }
                }

                currentHeight += controlHeight;
            }
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        #endregion

        #region Utility Methods

        private void SetUnlockCursor()
        {
            if (Cursor.lockState == CursorLockMode.None) return;

            oldCursorLockState = Cursor.lockState;
            oldCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ResetCursor()
        {
            Cursor.lockState = oldCursorLockState;
            Cursor.visible = oldCursorVisible;
        }

        #endregion

        #region avatar methods

        IEnumerator LoadAvatar()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Plugin.RefreshBundlePaths();

            #region Get Selected Avatar

            if (player == Player.m_localPlayer || Game.instance == null)
                selectedAvatar = PluginConfig.selectedAvatar.Value;

            if (zNetView != null && zNetView.IsValid() && !zNetView.IsOwner() && Game.instance != null)
            {
                int tries = 0;
                while (selectedAvatar == "" && tries < 100)
                {
                    selectedAvatar = zNetView.GetZDO().GetString(selectedHash);
                    tries++;
                    yield return new WaitForSecondsRealtime(0.05f);
                }
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

            #endregion

            #region Load Asset Bundle

            Debug.Log("Loading " + selectedAvatar + " avatar");

            if (!Plugin.playerModelBundlePaths.ContainsKey(selectedAvatar))
            {
                Debug.LogError("Bundle list doesn't contain bundle for : " + selectedAvatar);
                Destroy(this);
                yield break;
            }

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

            if (!loader.LoadAvatar(this))
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
            loader.Animator.applyRootMotion = true;
            loader.Animator.updateMode = ogAnimator.updateMode;
            loader.Animator.feetPivotActive = ogAnimator.feetPivotActive;
            loader.Animator.layersAffectMassCenter = ogAnimator.layersAffectMassCenter;
            loader.Animator.stabilizeFeet = ogAnimator.stabilizeFeet;

            if (ogPose == null) ogPose = new HumanPoseHandler(ogAnimator.avatar, ogAnimator.transform);
            if (pmPose == null) pmPose = new HumanPoseHandler(loader.Animator.avatar, loader.Animator.transform);

            footOffset = ((loader.LeftFoot.position.y - loader.Transform.position.y) +
                          (loader.RightFoot.position.y - loader.Transform.position.y)) / 2.0f;

            ogAttachments = new List<AttachTransform>();
            SetAttachParent(visEquipment.m_backAtgeir, loader.AvatarDescriptor.backAtgeir);
            SetAttachParent(visEquipment.m_backBow, loader.AvatarDescriptor.backBow);
            SetAttachParent(visEquipment.m_backMelee, loader.AvatarDescriptor.backMelee);
            SetAttachParent(visEquipment.m_backShield, loader.AvatarDescriptor.backShield);
            SetAttachParent(visEquipment.m_backTool, loader.AvatarDescriptor.backTool);
            SetAttachParent(visEquipment.m_backTwohandedMelee, loader.AvatarDescriptor.backTwohandedMelee);
            SetAttachParent(visEquipment.m_helmet, loader.AvatarDescriptor.helmet);
            SetAttachParent(visEquipment.m_leftHand, loader.AvatarDescriptor.leftHand);
            SetAttachParent(visEquipment.m_rightHand, loader.AvatarDescriptor.rightHand);

            ToggleAvatar();
        }

        public void ToggleAvatar(bool visible = true)
        {
            playerModelVisible = visible;
            loader.Animator?.gameObject.SetActive(visible);

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in ogVisual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.forceRenderingOff = visible;
                skinnedMeshRenderer.updateWhenOffscreen = true;
            }

            ToggleEquipments(!visible);
        }

        public void Hide()
        {
            loader.Animator.gameObject.SetActive(false);
        }

        public void Show()
        {
            if(loader.Animator)
                loader.Animator.gameObject.SetActive(true);
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
                visEquipment.m_helmetItemInstance?.SetActive(visible || loader.AvatarDescriptor.showHelmet);

                if(visEquipment.m_shoulderItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_shoulderItemInstances)
                    {
                        if (visEquipment.m_shoulderItem.ToLower().Contains("cape"))
                        {
                            itemInstance?.SetActive(visible || loader.AvatarDescriptor.showCape);
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
            ragdoll.gameObject.AddComponent<CustomRagdoll>().Setup(ogAnimator,loader.Animator);
        }

        public void RemoveAvatar(bool forced = false)
        {
            if ((!PluginConfig.enableCustomRagdoll.Value || forced) && loader != null) loader.Destroy();
            loader = null;
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

        public void Unload()
        {
            if (loader != null) loader.Unload();
        }

        #endregion

        #region Animator Params Methods

        private void SetParameterValue(string name, float value)
        {
            int hash = Animator.StringToHash(name);
            SetParameterValue(hash,value);
        }

        private void SetParameterValue(int hash, float value)
        {
            if (!loader.Parameters.ContainsKey(hash)) return;
            switch (loader.Parameters[hash].type)
            {
                case AvatarLoaderBase.ParameterType.Bool:
                    SetBool(hash, value != 0);
                    break;
                case AvatarLoaderBase.ParameterType.Int:
                    SetInt(hash, (int)value);
                    break;
                case AvatarLoaderBase.ParameterType.Float:
                    SetFloat(hash, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetParameterValue(string name)
        {
            int hash = Animator.StringToHash(name);
            return GetParameterValue(hash);
        }

        private float GetParameterValue(int hash)
        {
            if (!loader.Parameters.ContainsKey(hash)) return 0;

            switch (loader.Parameters[hash].type)
            {
                case AvatarLoaderBase.ParameterType.Bool:
                    return loader.Parameters[hash].boolValue ? 1 : 0;
                case AvatarLoaderBase.ParameterType.Int:
                    return loader.Parameters[hash].intValue;
                case AvatarLoaderBase.ParameterType.Float:
                    return loader.Parameters[hash].floatValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool SetBool(string name, bool value)
        {
            int hash = Animator.StringToHash(name);
            return SetBool(hash, value);
        }

        public bool SetBool(int hash, bool value)
        {
            if (!loader.Parameters.ContainsKey(hash)) return false;

            loader.Parameters[hash].boolValue = value;
            loader.Animator.SetBool(hash, value);

            if (zNetView.GetZDO() != null && zNetView.IsOwner())
                zNetView.GetZDO().Set(438569 + hash, value);
            return true;
        }

        public bool SetInt(string name, int value)
        {
            int hash = Animator.StringToHash(name);
            return SetInt(hash, value);
        }

        public bool SetInt(int hash, int value)
        {
            if (!loader.Parameters.ContainsKey(hash)) return false;

            loader.Parameters[hash].intValue = value;
            loader.Animator.SetInteger(hash, value);

            if (zNetView.GetZDO() != null && zNetView.IsOwner())
                zNetView.GetZDO().Set(438569 + hash, value);
            return true;
        }

        public bool SetFloat(string name, float value)
        {
            int hash = Animator.StringToHash(name);
            return SetFloat(hash, value);
        }

        public bool SetFloat(int hash, float value)
        {
            if (!loader.Parameters.ContainsKey(hash)) return false;

            loader.Parameters[hash].floatValue = value;
            loader.Animator.SetFloat(hash, value);

            if (zNetView.GetZDO() != null && zNetView.IsOwner())
                zNetView.GetZDO().Set(438569 + hash, value);
            return true;
        }
        #endregion
    }
}
#endif