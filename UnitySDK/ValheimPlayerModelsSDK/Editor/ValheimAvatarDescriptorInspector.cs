using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ValheimPlayerModels;
using UnityEditor.SceneManagement;
using System.IO;
using NUnit.Framework.Api;

[CustomEditor(typeof(ValheimAvatarDescriptor))]
public class ValheimAvatarDescriptorInspector : Editor
{
    private SerializedProperty avatarName;

    private SerializedProperty leftHand;
    private SerializedProperty rightHand;
    private SerializedProperty helmet;
    private SerializedProperty backShield;
    private SerializedProperty backMelee;
    private SerializedProperty backTwohandedMelee;
    private SerializedProperty backBow;
    private SerializedProperty backTool;
    private SerializedProperty backAtgeir;

    private SerializedProperty showHelmet;
    private SerializedProperty showCape;

    private SerializedProperty boolParameters;
    private SerializedProperty boolParametersDefault;
    private SerializedProperty intParameters;
    private SerializedProperty intParametersDefault;
    private SerializedProperty floatParameters;
    private SerializedProperty floatParametersDefault;

    private SerializedProperty controlName;
    private SerializedProperty controlTypes;
    private SerializedProperty controlParameterNames;
    private SerializedProperty controlValues;

    private void OnEnable()
    {
        avatarName = serializedObject.FindProperty("avatarName");

        leftHand = serializedObject.FindProperty("leftHand");
        rightHand = serializedObject.FindProperty("rightHand");
        helmet = serializedObject.FindProperty("helmet");
        backShield = serializedObject.FindProperty("backShield");
        backMelee = serializedObject.FindProperty("backMelee");
        backTwohandedMelee = serializedObject.FindProperty("backTwohandedMelee");
        backBow = serializedObject.FindProperty("backBow");
        backTool = serializedObject.FindProperty("backTool");
        backAtgeir = serializedObject.FindProperty("backAtgeir");

        showHelmet = serializedObject.FindProperty("showHelmet");
        showCape = serializedObject.FindProperty("showCape");

        boolParameters = serializedObject.FindProperty("boolParameters");
        boolParametersDefault = serializedObject.FindProperty("boolParametersDefault");
        intParameters = serializedObject.FindProperty("intParameters");
        intParametersDefault = serializedObject.FindProperty("intParametersDefault");
        floatParameters = serializedObject.FindProperty("floatParameters");
        floatParametersDefault = serializedObject.FindProperty("floatParametersDefault");

        controlName = serializedObject.FindProperty("controlName");
        controlTypes = serializedObject.FindProperty("controlTypes");
        controlParameterNames = serializedObject.FindProperty("controlParameterNames");
        controlValues = serializedObject.FindProperty("controlValues");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("It's recommended to use the \"Valheim/Standard\" shader\non the avatar or you'll get weird results.", MessageType.Info);

        ValheimAvatarDescriptor descriptor = (ValheimAvatarDescriptor)target;
        Animator animator = descriptor.GetComponent<Animator>();

        bool valid = true;
        valid = valid && !string.IsNullOrEmpty(descriptor.avatarName);
        valid = valid && descriptor.leftHand != null;
        valid = valid && descriptor.rightHand != null;
        valid = valid && descriptor.helmet != null;
        valid = valid && descriptor.backShield != null;
        valid = valid && descriptor.backMelee != null;
        valid = valid && descriptor.backTwohandedMelee != null;
        valid = valid && descriptor.backBow != null;
        valid = valid && descriptor.backTool != null;
        valid = valid && descriptor.backAtgeir != null;
        valid = valid && animator != null;

        GUI.enabled = false;

        if (animator && !animator.avatar.isHuman)
        {
            EditorGUILayout.HelpBox("Avatar is not Humanoid, no animation will play if kept this way.", MessageType.Warning);
        }

        if (animator && animator.avatar.isHuman) GUI.enabled = true;
        if (GUILayout.Button("Auto-Setup"))
        {
            Undo.SetCurrentGroupName("Auto-created avatar objects");

            animator.transform.localScale = Vector3.one;
            float headHeight = animator.GetBoneTransform(HumanBodyBones.Head).position.y - animator.transform.position.y;
            float pmScale = 1.669556f / headHeight;
            animator.transform.localScale = new Vector3(pmScale, pmScale, pmScale);

            if (!descriptor.leftHand)
            {
                descriptor.leftHand = new GameObject("LeftHand_Attach").transform;
                descriptor.leftHand.SetParent(animator.GetBoneTransform(HumanBodyBones.LeftHand), false);
                descriptor.leftHand.localEulerAngles = new Vector3(0, 0, -180);
                descriptor.leftHand.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.leftHand,"BowPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.leftHand.gameObject, "added attach point");
            }

            if (!descriptor.rightHand)
            {
                descriptor.rightHand = new GameObject("RightHand_Attach").transform;
                descriptor.rightHand.SetParent(animator.GetBoneTransform(HumanBodyBones.RightHand), false);
                descriptor.rightHand.localEulerAngles = new Vector3(0, 0, 0);
                descriptor.rightHand.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.rightHand, "OneHandedPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.rightHand.gameObject, "added attach point");
            }

            if (!descriptor.helmet)
            {
                descriptor.helmet = new GameObject("Helmet_attach").transform;
                descriptor.helmet.SetParent(animator.GetBoneTransform(HumanBodyBones.Head), false);
                descriptor.helmet.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.helmet, "HelmetPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.helmet.gameObject, "added attach point");
            }

            if (!descriptor.backShield)
            {
                descriptor.backShield = new GameObject("BackShield_attach").transform;
                descriptor.backShield.SetParent(animator.GetBoneTransform(HumanBodyBones.Chest), false);
                descriptor.backShield.localEulerAngles = new Vector3(260, -75, -130);
                descriptor.backShield.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.backShield, "ShieldPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.backShield.gameObject, "added attach point");
            }

            if (!descriptor.backMelee)
            {
                descriptor.backMelee = new GameObject("BackOneHanded_attach").transform;
                descriptor.backMelee.SetParent(animator.GetBoneTransform(HumanBodyBones.Chest), false);
                descriptor.backMelee.localEulerAngles = new Vector3(120, 90, 95);
                descriptor.backMelee.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.backMelee, "OneHandedPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.backMelee.gameObject, "added attach point");
            }

            if (!descriptor.backTwohandedMelee)
            {
                descriptor.backTwohandedMelee = new GameObject("BackTwohanded_attach").transform;
                descriptor.backTwohandedMelee.SetParent(animator.GetBoneTransform(HumanBodyBones.Chest), false);
                descriptor.backTwohandedMelee.localEulerAngles = new Vector3(125, 90, 100);
                descriptor.backTwohandedMelee.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.backTwohandedMelee, "TwoHandedPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.backTwohandedMelee.gameObject, "added attach point");
            }

            if (!descriptor.backBow)
            {
                descriptor.backBow = new GameObject("BackBow_attach").transform;
                descriptor.backBow.SetParent(animator.GetBoneTransform(HumanBodyBones.Chest), false);
                descriptor.backBow.localEulerAngles = new Vector3(-115, -45, 136);
                descriptor.backBow.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.backBow, "BowPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.backBow.gameObject, "added attach point");
            }

            if (!descriptor.backTool)
            {
                descriptor.backTool = new GameObject("BackTool_attach").transform;
                descriptor.backTool.SetParent(animator.GetBoneTransform(HumanBodyBones.Hips), false);
                descriptor.backTool.localEulerAngles = new Vector3(100, -90, -180);
                descriptor.backTool.gameObject.AddComponent<ForceSelection>();
                AddPreview(descriptor.backTool, "ToolPreview");
                Undo.RegisterCreatedObjectUndo(descriptor.backTool.gameObject, "added attach point");
            }

            if (!descriptor.backAtgeir)
            {
                descriptor.backAtgeir = new GameObject("BackAtgeir_attach").transform;
                descriptor.backAtgeir.SetParent(animator.GetBoneTransform(HumanBodyBones.Chest), false);
                descriptor.backAtgeir.localEulerAngles = new Vector3(-75, 45, -225);
                descriptor.backAtgeir.gameObject.AddComponent<ForceSelection>();
                Undo.RegisterCreatedObjectUndo(descriptor.backAtgeir.gameObject, "added attach point");
            }
            Undo.IncrementCurrentGroup();
        }

        GUI.enabled = valid;

        if (GUILayout.Button("Export"))
        {
            string path = EditorUtility.SaveFilePanel("Save object file", "", descriptor.avatarName + ".valavtr", "valavtr");

            if (path != "")
            {
                string fileName = Path.GetFileName(path);
                string folderPath = Path.GetDirectoryName(path);

                Selection.activeObject = descriptor.gameObject;
                EditorUtility.SetDirty(descriptor);
                EditorSceneManager.MarkSceneDirty(descriptor.gameObject.scene);
                EditorSceneManager.SaveScene(descriptor.gameObject.scene);

                GameObject avatarClone = Instantiate(descriptor.gameObject);
                foreach (Transform child in avatarClone.GetComponentsInChildren<Transform>())
                {
                    if(child != null && child.CompareTag("EditorOnly")) DestroyImmediate(child.gameObject);
                }

                foreach (ForceSelection child in avatarClone.GetComponentsInChildren<ForceSelection>())
                {
                    if (child != null) DestroyImmediate(child);
                }

                PrefabUtility.SaveAsPrefabAsset(avatarClone, "Assets/_avatar.prefab");
                DestroyImmediate(avatarClone);
                AssetBundleBuild assetBundleBuild = default(AssetBundleBuild);
                assetBundleBuild.assetNames = new string[] {
                    "Assets/_avatar.prefab"
                };

                assetBundleBuild.assetBundleName = fileName;

                BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

                BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { assetBundleBuild }, 0, EditorUserBuildSettings.activeBuildTarget);
                EditorPrefs.SetString("currentBuildingAssetBundlePath", folderPath);
                EditorUserBuildSettings.SwitchActiveBuildTarget(selectedBuildTargetGroup, activeBuildTarget);
                AssetDatabase.DeleteAsset("Assets/_avatar.prefab");
                if(File.Exists(path))
                    File.Delete(path);
                File.Move(Application.temporaryCachePath + "/" + fileName, path);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Exportation Successful!", "Exportation Successful!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Exportation Failed!", "Path is invalid.", "OK");
            }

        }

        GUI.enabled = true;

        EditorGUILayout.LabelField("Infos", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(avatarName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attachment points", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(leftHand);
        EditorGUILayout.PropertyField(rightHand);
        EditorGUILayout.PropertyField(helmet);
        EditorGUILayout.PropertyField(backShield);
        EditorGUILayout.PropertyField(backMelee);
        EditorGUILayout.PropertyField(backTwohandedMelee);
        EditorGUILayout.PropertyField(backBow);
        EditorGUILayout.PropertyField(backTool);
        EditorGUILayout.PropertyField(backAtgeir);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Show Features", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showHelmet);
        EditorGUILayout.PropertyField(showCape);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
        DrawListAsDictionary(boolParameters,boolParametersDefault, "param_");
        DrawListAsDictionary(intParameters,intParametersDefault, "param_");
        DrawListAsDictionary(floatParameters,floatParametersDefault, "param_");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Menu", EditorStyles.boldLabel);
        DrawCombinedLists(new string[]{"Name","Type","Parameter name","Value"}, 75,controlName,controlTypes,controlParameterNames,controlValues);

        serializedObject.ApplyModifiedProperties();
    }

    private void AddPreview(Transform parent, string previewName)
    {
        GameObject preview = Instantiate(Resources.Load<GameObject>(previewName));
        preview.transform.SetParent(parent,true);
        preview.transform.localPosition = Vector3.zero;
        preview.transform.localRotation = Quaternion.identity;
    }

    private void DrawListAsDictionary(SerializedProperty keyList, SerializedProperty valueList, string defaultName)
    {
        if (valueList.arraySize != keyList.arraySize)
        {
            valueList.arraySize = keyList.arraySize;
        }

        EditorGUILayout.PropertyField(keyList,false);
        
        if (keyList.isExpanded)
        {
            EditorGUI.indentLevel += 1;

            for (int i = 0; i < keyList.arraySize; i++)
            {
                for (int j = 0; j < keyList.arraySize; j++)
                {
                    if (j != i && keyList.GetArrayElementAtIndex(j).stringValue == keyList.GetArrayElementAtIndex(i).stringValue)
                    {
                        EditorGUILayout.HelpBox("A Parameter with the same name already exists.", MessageType.Error);
                        break;
                    }
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(keyList.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUILayout.PropertyField(valueList.GetArrayElementAtIndex(i), GUIContent.none);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel -= 1;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+"))
            {
                keyList.InsertArrayElementAtIndex(keyList.arraySize);
                valueList.InsertArrayElementAtIndex(valueList.arraySize);

                int count = 0;
                bool exists = false;
                do
                {
                    exists = false;
                    for (int i = 0; i < keyList.arraySize; i++)
                    {
                        if (keyList.GetArrayElementAtIndex(i).stringValue == defaultName + count)
                        {
                            exists = true;
                            count++;
                            break;
                        }
                    }
                } while (exists);

                keyList.GetArrayElementAtIndex(keyList.arraySize - 1).stringValue = defaultName + count;
            }

            if (GUILayout.Button("-"))
            {
                if (keyList.arraySize > 0)
                {
                    keyList.DeleteArrayElementAtIndex(keyList.arraySize - 1);
                    valueList.DeleteArrayElementAtIndex(valueList.arraySize - 1);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCombinedLists(string[] labels, float labelWidth, params SerializedProperty[] lists)
    {
        EditorGUILayout.PropertyField(lists[0], false);

        if (lists[0].isExpanded)
        {
            EditorGUI.indentLevel += 1;

            for (int i = 0; i < lists[0].arraySize; i++)
            {
                EditorGUILayout.BeginVertical("Box");

                for (int j = 0; j < lists.Length; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(labels[j], GUILayout.MaxWidth(labelWidth));
                    EditorGUILayout.PropertyField(lists[j].GetArrayElementAtIndex(i),GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel -= 1;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+"))
            {
                for (int j = 0; j < lists.Length; j++)
                {
                    lists[j].InsertArrayElementAtIndex(lists[j].arraySize);
                }
            }

            if (GUILayout.Button("-"))
            {
                if (lists[0].arraySize > 0)
                {
                    for (int j = 0; j < lists.Length; j++)
                    {
                        lists[j].DeleteArrayElementAtIndex(lists[j].arraySize-1);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
