#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.IO;

namespace ValheimPlayerModels
{
    [DefaultExecutionOrder(int.MaxValue-1)]
    class PlayerModel : MonoBehaviour
    {
        public class AttachTransform
        {
            public Transform ogAttach;
            public Transform pmAttach;
        }

        private Player player;
        private VisEquipment visEquipment;
        private GameObject ogVisual;

        private AssetBundle avatarBundle;
        private GameObject avatarObject;
        private bool playerModelLoaded = false;

        private Animator ogAnimator;
        //private Dictionary<Transform, OgAttachTransform> ogAttachments;
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

        private void Awake()
        {
            player = GetComponent<Player>();
            visEquipment = GetComponent<VisEquipment>();
            ogVisual = player.m_animator.gameObject;

            ogAnimator = player.m_animator;
            ogAnimator.keepAnimatorControllerStateOnDisable = true;
            ogAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ogAttachments = new List<AttachTransform>();

            StartCoroutine(LoadAvatar());
        }

        private void OnDestroy()
        {
            if(avatarObject) Destroy(avatarObject);
            if (ogPose != null) ogPose.Dispose();
            if (pmPose != null) pmPose.Dispose();
            StopAllCoroutines();
            //if(avatarBundle) avatarBundle.Unload(true);
        }

        private void LateUpdate()
        {
            if (playerModelLoaded)
            {
                pmTranform.localPosition = Vector3.zero;
                ogPose.GetHumanPose(ref pose);
                pmPose.SetHumanPose(ref pose);

                Transform ogHips = ogAnimator.GetBoneTransform(HumanBodyBones.Hips);

                pmHips.position = new Vector3(pmHips.position.x, ogHips.position.y, pmHips.position.z);

                float groundOffset = Mathf.Min(pmLeftFoot.position.y - pmTranform.position.y, pmRightFoot.position.y - pmTranform.position.y, 0);

                pmHips.Translate(0,-groundOffset+ footOffset, 0,Space.World);

                foreach (AttachTransform attachTransform in ogAttachments)
                {
                    attachTransform.ogAttach.position = attachTransform.pmAttach.position;
                    attachTransform.ogAttach.rotation = attachTransform.pmAttach.rotation;
                }

                if (player.IsDead() && !dead)
                {
                    dead = true;
                    Hide();
                }
            }
        }

        IEnumerator LoadAvatar()
        {
            yield return new WaitForSeconds(1);
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
            Plugin.RefreshBundlePaths();

            Debug.Log("Loading " + playerName + " avatar");

            if (!Plugin.playerModelBundlePaths.ContainsKey(playerName))
            {
                Debug.LogError("Bundle list doesn't contain bundle for : " + playerName);
                Destroy(this);
                yield break;
            }

            if (Plugin.playerModelBundleCache.ContainsKey(playerName) && Plugin.playerModelBundleCache[playerName])
            {
                
                avatarBundle = Plugin.playerModelBundleCache[playerName];
            }
            else
            {
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

                Plugin.playerModelBundleCache.Add(playerName, avatarBundle);
            }
            

            GameObject avatarAsset = avatarBundle.LoadAsset<GameObject>("_avatar");
            if (!avatarAsset)
            {
                Debug.LogError("Couldn't find avatar prefab for " + playerName);
                Destroy(this);
                yield break;
            }

            avatarObject = Instantiate(avatarAsset);
            avatarDescriptor = avatarObject.GetComponent<ValheimAvatarDescriptor>();

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
            pmTranform = avatarObject.transform;
            pmTranform.SetParent(transform,false);
            ApplyAvatar();
        }

        private void SetAttachParent(Transform attach, Transform newAttach)
        {
            ogAttachments.Add(new AttachTransform
            {
                ogAttach = attach,
                pmAttach = newAttach
            });
        }

        private void ApplyAvatar()
        {
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in ogVisual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.forceRenderingOff = true;
                skinnedMeshRenderer.updateWhenOffscreen = true;
            }

            pmAnimator = avatarObject.GetComponent<Animator>();
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

            SetAttachParent(visEquipment.m_backAtgeir,avatarDescriptor.backAtgeir);
            SetAttachParent(visEquipment.m_backBow, avatarDescriptor.backBow);
            SetAttachParent(visEquipment.m_backMelee,avatarDescriptor.backMelee);
            SetAttachParent(visEquipment.m_backShield,avatarDescriptor.backShield);
            SetAttachParent(visEquipment.m_backTool,avatarDescriptor.backTool);
            SetAttachParent(visEquipment.m_backTwohandedMelee,avatarDescriptor.backTwohandedMelee);
            SetAttachParent(visEquipment.m_helmet,avatarDescriptor.helmet);
            SetAttachParent(visEquipment.m_leftHand,avatarDescriptor.leftHand);
            SetAttachParent(visEquipment.m_rightHand, avatarDescriptor.rightHand);
            visEquipment.m_helmet.localScale = Vector3.zero;


            playerModelLoaded = true;
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

        public void HideEquipmentNextFrame()
        {
            StartCoroutine(HideEquipmentNextFrameCoroutine());
        }

        private IEnumerator HideEquipmentNextFrameCoroutine()
        {
            yield return new WaitForEndOfFrame();
            HideEquipments();
        }

        public void HideEquipments()
        {
            if (visEquipment)
            {
                visEquipment.m_beardItemInstance?.SetActive(false);
                visEquipment.m_hairItemInstance?.SetActive(false);
                visEquipment.m_helmetItemInstance?.SetActive(false);

                if(visEquipment.m_shoulderItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_shoulderItemInstances) { itemInstance?.SetActive(false); }
                if (visEquipment.m_legItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_legItemInstances) { itemInstance?.SetActive(false); }
                if (visEquipment.m_chestItemInstances != null)
                    foreach (GameObject itemInstance in visEquipment.m_chestItemInstances) { itemInstance?.SetActive(false); }

            }
        }
    }
}
#endif