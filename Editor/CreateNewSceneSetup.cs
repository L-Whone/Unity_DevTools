#if UNITY_EDITOR
using SceneManagement;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class CreateNewSceneSetup : EditorWindow
{
    private const string SCENES_FOLDER_PREF_KEY = "CreateNewSceneSetup_ScenesFolder";
    private const string DEFAULT_SCENES_FOLDER = "Assets/Scenes/";
    private const string SAVE_FOLDER = "Assets/Editor/CreateNewSceneSetup";
    private const string SAVE_FILE = "Assets/Editor/CreateNewSceneSetup/additiveScenes.json";

    [SerializeField] private string scenePrefix = "NewScene";
    [SerializeField] private string scenesFolder = DEFAULT_SCENES_FOLDER;
    [SerializeField] private bool setActiveInBuildProfiles = true;

    private string projectRoot;

    [SerializeField]
    private List<SceneInfo> additiveScenes = new List<SceneInfo>
    {
        new SceneInfo { sceneName = "Cameras"},
        new SceneInfo { sceneName = "Lighting"},
        new SceneInfo { sceneName = "UI"},
        new SceneInfo { sceneName = "Level"},
        new SceneInfo { sceneName = "Props"},
        new SceneInfo { sceneName = "Players"},
        new SceneInfo { sceneName = "Enemies"},
    };

    SerializedObject serializedObject;

    SerializedProperty additiveScenesProperty;

    // helper classes for JSON serialization — store GUIDs instead of object references
    // so the data stays valid even if assets are moved


    [MenuItem("Tools/New Scene Setup")]
    public static void ShowWindow()
    {
        GetWindow<CreateNewSceneSetup>("New Scene Setup Menu");
    }

    private void OnEnable()
    {
        // load before creating serializedObject so it sees the restored state
        LoadAdditiveScenes();

        // wrapper around the class it's in
        serializedObject = new SerializedObject(this);

        // pointer to field of serialized bject
        additiveScenesProperty = serializedObject.FindProperty("additiveScenes");

        // Restore the saved folder path, falling back to the default if none is saved yet
        scenesFolder = EditorPrefs.GetString(SCENES_FOLDER_PREF_KEY, DEFAULT_SCENES_FOLDER);

        projectRoot = System.IO.Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
    }

    private void OnDisable()
    {
        SaveAdditiveScenes();
    }

    private void SaveAdditiveScenes()
    {
        SavedSceneList saved = new SavedSceneList();

        foreach (SceneInfo info in additiveScenes)
        {
            SavedSceneInfo savedInfo = new SavedSceneInfo { sceneName = info.sceneName };

            foreach (GameObject prefab in info.prefabsToSpawn)
            {
                // store the GUID so the reference survives asset moves
                string guid = prefab != null
                    ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab))
                    : "";
                savedInfo.prefabGuids.Add(guid);
            }

            saved.scenes.Add(savedInfo);
        }

        // create the save folder if this is the first run
        if (!System.IO.Directory.Exists(SAVE_FOLDER))
            System.IO.Directory.CreateDirectory(SAVE_FOLDER);

        System.IO.File.WriteAllText(SAVE_FILE, JsonUtility.ToJson(saved, true));
        AssetDatabase.Refresh();
    }

    private void LoadAdditiveScenes()
    {
        if (!System.IO.File.Exists(SAVE_FILE)) return;

        string json = System.IO.File.ReadAllText(SAVE_FILE);
        if (string.IsNullOrEmpty(json)) return;

        SavedSceneList saved = JsonUtility.FromJson<SavedSceneList>(json);
        if (saved?.scenes == null) return;

        additiveScenes.Clear();

        foreach (SavedSceneInfo savedInfo in saved.scenes)
        {
            SceneInfo info = new SceneInfo { sceneName = savedInfo.sceneName };

            foreach (string guid in savedInfo.prefabGuids)
            {
                if (!string.IsNullOrEmpty(guid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    info.prefabsToSpawn.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                }
                else
                {
                    info.prefabsToSpawn.Add(null);
                }
            }

            additiveScenes.Add(info);
        }
    }

    private void OnGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("New Scene Setup Menu", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Set Scene Name
        scenePrefix = EditorGUILayout.TextField("Scene Prefix", scenePrefix);
        EditorGUILayout.Space();

        // Set Scenes Folder
        EditorGUILayout.LabelField("Scenes Folder", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        scenesFolder = EditorGUILayout.TextField(scenesFolder);
        if (EditorGUI.EndChangeCheck())
        {
            // Persist immediately whenever the user edits the text field directly
            SaveFolderPref();
        }

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            // Open a native folder picker starting from the current selection
            string absoluteStart = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(projectRoot, scenesFolder)
            ).Replace("\\", "/");

            string absPath = EditorUtility.OpenFolderPanel(
                "Choose Scenes Folder",
                absoluteStart,
                ""
            );

            // OpenFolderPanel returns an absolute path; convert it to a project-relative one
            if (!string.IsNullOrEmpty(absPath))
            {
                string normalizedAbs = absPath.Replace("\\", "/");

                if (normalizedAbs.StartsWith(projectRoot))
                {
                    scenesFolder = normalizedAbs.Substring(projectRoot.Length).TrimStart('/');

                    if (!scenesFolder.EndsWith("/"))
                        scenesFolder += "/";
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Folder",
                        "Please choose a folder inside this Unity project.",
                        "OK"
                    );
                }

                SaveFolderPref();
                GUI.FocusControl(null); // clear keyboard focus so the text field refreshes
            }
        }

        EditorGUILayout.EndHorizontal();

        // Small hint showing the resolved path so the user can confirm it looks right
        EditorGUILayout.HelpBox($"Scenes will be saved to: {scenesFolder}", MessageType.None);
        EditorGUILayout.Space();

        setActiveInBuildProfiles = EditorGUILayout.Toggle("Set Active in Build Profiles", setActiveInBuildProfiles);
        EditorGUILayout.HelpBox(
            setActiveInBuildProfiles
                ? "All created scenes will be added to the Build Profiles scene list as enabled."
                : "All created scenes will be added to the Build Profiles scene list as disabled.",
            MessageType.None
        );
        EditorGUILayout.Space();

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

    // Write the current scenesFolder value to EditorPrefs
    private void SaveFolderPref()
    {
        EditorPrefs.SetString(SCENES_FOLDER_PREF_KEY, scenesFolder);
    }

    private void SaveOpenScenes()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.SaveOpenScenes();
    }

    private void CreateScenes(string prefix = "NewScene")
    {
        // Use the user chosen folder, check the directory exists on disk
        string basePath = scenesFolder;
        if (!basePath.EndsWith("/")) basePath += "/";
        basePath += prefix + "/";
        System.IO.Directory.CreateDirectory(basePath);

        string baseScenePath = $"{basePath}{prefix}.unity";

        Scene baseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(baseScene, baseScenePath);

        // add scene to build settings
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        ArrayUtility.Add(ref original, new EditorBuildSettingsScene(baseScenePath, setActiveInBuildProfiles));

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
            ArrayUtility.Add(ref original, new EditorBuildSettingsScene(scenePath, setActiveInBuildProfiles));
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

[Serializable]
public class SceneInfo
{
    public string sceneName;
    public List<GameObject> prefabsToSpawn = new List<GameObject>();
}

[System.Serializable]
public class SavedSceneInfo
{
    public string sceneName;
    public List<string> prefabGuids = new List<string>();
}

[System.Serializable]
public class SavedSceneList
{
    public List<SavedSceneInfo> scenes = new List<SavedSceneInfo>();
}
#endif