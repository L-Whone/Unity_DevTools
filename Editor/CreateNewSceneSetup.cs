#if UNITY_EDITOR
using System.Collections.Generic;
using SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class CreateNewSceneSetup : EditorWindow
{
    [SerializeField] private string scenePrefix = "NewScene";
    [SerializeField] private List<SceneInfo> additiveScenes = new List<SceneInfo>
    {
        new SceneInfo { sceneName = "Cameras"},
        new SceneInfo { sceneName = "Rendering"},
        new SceneInfo { sceneName = "UI"},
        new SceneInfo { sceneName = "World"},
        new SceneInfo { sceneName = "CHARACTERS"},
    };

    SerializedObject serializedObject;

    SerializedProperty additiveScenesProperty;

    [MenuItem("Tools/New Scene Setup")]
    public static void ShowWindow()
    {
        GetWindow<CreateNewSceneSetup>("New Scene Setup Menu");
    }

    private void OnEnable()
    {
        // wrapper around the class it's in
        serializedObject = new SerializedObject(this);



        // pointer to field of serialized bject
        additiveScenesProperty = serializedObject.FindProperty("additiveScenes");
    }

    private void OnGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("New Scene Setup Menu", EditorStyles.boldLabel);

        // Set Scene Name
        scenePrefix = EditorGUILayout.TextField("Scene Prefix", scenePrefix);

        // set Additive Scenes
        EditorGUILayout.PropertyField(additiveScenesProperty, true);

        //spawn button
        if (GUILayout.Button("Create New Scene Setup"))
        {
            SaveOpenScenes();
            CreateScenes(scenePrefix);
        }


        serializedObject.ApplyModifiedProperties();
    }


    private void SaveOpenScenes()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.SaveOpenScenes();
    }

    private void CreateScenes(string prefix = "NewScene")
    {
        string basePath = "Assets/Scenes/";
        System.IO.Directory.CreateDirectory(basePath);
        Scene baseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(baseScene, $"{basePath + prefix}.unity");
        // add scene to build settings
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        ArrayUtility.Add(ref original, new EditorBuildSettingsScene(basePath + prefix, true));

        // create additive scene manager
        GameObject additiveSceneManagerObj = new GameObject("Additive Scene Manager");
        AdditiveSceneManager additiveSceneManager = additiveSceneManagerObj.AddComponent<AdditiveSceneManager>();

        /// create additive scenes
        foreach (SceneInfo sceneInfo in additiveScenes)
        {
            Scene additiveScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            string scenePath = $"{basePath}{prefix}-{sceneInfo.sceneName}.unity";
            EditorSceneManager.SaveScene(additiveScene, scenePath);
            EditorSceneManager.SetActiveScene(additiveScene);

            //spawn prefabs in each additive scene
            foreach (GameObject prefab in sceneInfo.prefabsToSpawn)
            {
                if (prefab == null) continue;
                PrefabUtility.InstantiatePrefab(prefab, additiveScene);
            }

            EditorSceneManager.SaveScene(additiveScene);
            ArrayUtility.Add(ref original, new EditorBuildSettingsScene(scenePath, true));
        }

        // access quin's scene list backing field 
        SerializedObject managerSerliazedObject = new SerializedObject(additiveSceneManager);
        SerializedProperty sceneListProperty = managerSerliazedObject.FindProperty("<SceneList>k__BackingField");
        sceneListProperty.ClearArray();

        // add new additive scenes to quin's scene list
        foreach (SceneInfo sceneInfo in additiveScenes)
        {
            string scenePath = $"{basePath}{prefix}-{sceneInfo.sceneName}.unity";
            sceneListProperty.InsertArrayElementAtIndex(sceneListProperty.arraySize);
            sceneListProperty.GetArrayElementAtIndex(sceneListProperty.arraySize - 1).stringValue = scenePath;
        }
        managerSerliazedObject.ApplyModifiedProperties();

        // set build settings scenes to have all new scenes created
        EditorBuildSettings.scenes = original;
        EditorSceneManager.SetActiveScene(baseScene);
        EditorSceneManager.SaveOpenScenes();
    }
}

#endif