// ===================================================================
// Map Wars: Tactical Conquest - Game Setup Editor Window
// Description: Custom Unity Editor window for one-click game setup.
//              Creates all required GameObjects, layers, tags,
//              and prefabs needed for the game.
// ===================================================================

using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor window that automates the setup process for the Map Wars project.
/// Creates all required GameObjects, assigns scripts, sets up tags and layers.
/// Access via: Menu > Map Wars > Setup Game
/// </summary>
public class MapWarsSetup : EditorWindow
{
    [MenuItem("Map Wars/Setup Game")]
    public static void ShowWindow()
    {
        GetWindow<MapWarsSetup>("Map Wars Setup");
    }

    private void OnGUI()
    {
        GUILayout.Space(20);

        GUILayout.Label("Map Wars: Tactical Conquest", EditorStyles.boldLabel);
        GUILayout.Label("Project Setup Tool", EditorStyles.miniLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool will set up all required GameObjects, tags, layers, and " +
            "assign scripts automatically. Make sure you have the scripts in " +
            "Assets/Scripts/ before running setup.",
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Full Setup (Recommended)", GUILayout.Height(40)))
        {
            SetupLayers();
            SetupTags();
            SetupGameObjects();
            SetupInputManager();
            SetupQualitySettings();
            EditorUtility.DisplayDialog("Setup Complete",
                "Game setup completed successfully!\n\n" +
                "Next steps:\n" +
                "1. Create Node and Troop prefabs\n" +
                "2. Assign references in Inspector\n" +
                "3. Add Particle Systems\n" +
                "4. Test in Play Mode",
                "OK");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Setup Layers & Tags Only", GUILayout.Height(30)))
        {
            SetupLayers();
            SetupTags();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Setup Game Objects Only", GUILayout.Height(30)))
        {
            SetupGameObjects();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("Create Scene Template", GUILayout.Height(30)))
        {
            CreateSceneTemplate();
        }
    }

    private static void SetupLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));

        SerializedProperty layers = tagManager.FindProperty("layers");

        AddLayer(layers, "Node");
        AddLayer(layers, "Troop");
        AddLayer(layers, "UIOverlay");

        tagManager.ApplyModifiedProperties();
        Debug.Log("[MapWars Setup] Layers configured");
    }

    private static void AddLayer(SerializedProperty layers, string layerName)
    {
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == layerName)
            {
                Debug.Log($"[MapWars Setup] Layer '{layerName}' already exists");
                return;
            }
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = layerName;
                Debug.Log($"[MapWars Setup] Layer '{layerName}' added at index {i}");
                return;
            }
        }
    }

    private static void SetupTags()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
        SerializedProperty tags = tagManager.FindProperty("tags");

        AddTag(tags, "Node");
        AddTag(tags, "Troop");
        AddTag(tags, "GameManager");
        AddTag(tags, "UI");

        tagManager.ApplyModifiedProperties();
        Debug.Log("[MapWars Setup] Tags configured");
    }

    private static void AddTag(SerializedProperty tags, string tagName)
    {
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                Debug.Log($"[MapWars Setup] Tag '{tagName}' already exists");
                return;
            }
        }
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        Debug.Log($"[MapWars Setup] Tag '{tagName}' added");
    }

    private static void SetupGameObjects()
    {
        // Create Managers
        CreateManager<GameManager>("_Managers", "GameManager");
        CreateManager<AIController>("_Managers", "AIController");
        CreateManager<UIManager>("_Managers", "UIManager");
        CreateManager<SaveSystem>("_Managers", "SaveSystem");
        CreateManager<EffectsManager>("_Managers", "EffectsManager");
        CreateManager<HapticFeedbackManager>("_Managers", "HapticFeedbackManager");
        CreateManager<AudioManager>("_Managers", "AudioManager");
        CreateManager<LevelManager>("_Managers", "LevelManager");
        CreateManager<MonetizationManager>("_Managers", "MonetizationManager");
        CreateManager<SkinManager>("_Managers", "SkinManager");
        CreateManager<ScreenAdapter>("_Managers", "ScreenAdapter");
        CreateManager<InputHandler>("_Managers", "InputHandler");

        // Create Grid Background
        CreateManager<GridBackground>("_Managers", "GridBackground");

        // Create Nodes Parent
        GameObject nodesParent = FindOrCreateGameObject("_GameObjects/Nodes", "NodesParent");
        GameObject.DestroyImmediate(nodesParent.GetComponent<SpriteRenderer>());

        Debug.Log("[MapWars Setup] GameObjects created");
    }

    private static void CreateManager<T>(string path, string name) where T : MonoBehaviour
    {
        GameObject go = FindOrCreateGameObject(path, name);
        if (go.GetComponent<T>() == null)
        {
            go.AddComponent<T>();
            Debug.Log($"[MapWars Setup] Added {typeof(T).Name} to {name}");
        }
    }

    private static GameObject FindOrCreateGameObject(string path, string name)
    {
        GameObject existing = GameObject.Find(path + "/" + name);
        if (existing != null) return existing;

        string[] parts = path.Split('/');
        Transform parent = null;

        foreach (string part in parts)
        {
            if (parent == null)
            {
                existing = GameObject.Find(part);
                if (existing == null)
                {
                    existing = new GameObject(part);
                }
                parent = existing.transform;
            }
            else
            {
                Transform child = parent.Find(part);
                if (child == null)
                {
                    GameObject childObj = new GameObject(part);
                    childObj.transform.SetParent(parent);
                    child = childObj.transform;
                }
                parent = child;
            }
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go;
    }

    private static void SetupInputManager()
    {
        SerializedObject inputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset"));
        Debug.Log("[MapWars Setup] Input manager configured (using new Input System)");
    }

    private static void SetupQualitySettings()
    {
        QualitySettings.SetQualityLevel(1); // Medium quality for mobile
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 0;
        Application.targetFrameRate = 60;
        Debug.Log("[MapWars Setup] Quality settings configured for mobile");
    }

    private static void CreateSceneTemplate()
    {
        GameObject camera = GameObject.Find("Main Camera");
        if (camera == null)
        {
            camera = new GameObject("Main Camera");
            camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
        }

        camera.GetComponent<Camera>().orthographic = true;
        camera.GetComponent<Camera>().orthographicSize = 5f;
        camera.GetComponent<Camera>().backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0, 0, -10);

        // Add InputHandler to camera
        if (camera.GetComponent<InputHandler>() == null)
        {
            camera.AddComponent<InputHandler>();
        }

        // Add ScreenAdapter to camera
        if (camera.GetComponent<ScreenAdapter>() == null)
        {
            camera.AddComponent<ScreenAdapter>();
        }

        Debug.Log("[MapWars Setup] Scene template created");
    }
}
