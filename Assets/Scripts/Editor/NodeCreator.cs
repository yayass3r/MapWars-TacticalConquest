// ===================================================================
// Map Wars: Tactical Conquest - Node Creator Editor
// Description: Editor tool to quickly create node prefabs
//              with all required components pre-configured.
// ===================================================================

using UnityEngine;
using UnityEditor;

public class NodeCreator : EditorWindow
{
    private NodeType _nodeType = NodeType.Small;
    private Faction _owner = Faction.Neutral;
    private int _initialSoldiers = 10;
    private Color _playerColor = new Color(0.2f, 0.6f, 1f);
    private Color _enemyColor = new Color(1f, 0.2f, 0.3f);
    private Color _neutralColor = new Color(0.7f, 0.7f, 0.7f);
    private float _nodeRadius = 0.5f;

    [MenuItem("Map Wars/Create Node Prefab")]
    public static void ShowWindow()
    {
        GetWindow<NodeCreator>("Node Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Node Prefab", EditorStyles.boldLabel);
        GUILayout.Space(10);

        _nodeType = (NodeType)EditorGUILayout.EnumPopup("Node Type", _nodeType);
        _owner = (Faction)EditorGUILayout.EnumPopup("Owner", _owner);
        _initialSoldiers = EditorGUILayout.IntField("Initial Soldiers", _initialSoldiers);

        GUILayout.Space(10);
        GUILayout.Label("Colors", EditorStyles.boldLabel);
        _playerColor = EditorGUILayout.ColorField("Player", _playerColor);
        _enemyColor = EditorGUILayout.ColorField("Enemy", _enemyColor);
        _neutralColor = EditorGUILayout.ColorField("Neutral", _neutralColor);

        GUILayout.Space(15);

        if (GUILayout.Button("Create Node Prefab", GUILayout.Height(35)))
        {
            CreateNodePrefab();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Create All Node Prefabs", GUILayout.Height(30)))
        {
            CreateAllNodePrefabs();
        }
    }

    private void CreateNodePrefab()
    {
        GameObject nodeObj = new GameObject($"Node_{_nodeType}_{_owner}");

        // Add required components
        SpriteRenderer bg = nodeObj.AddComponent<SpriteRenderer>();
        CircleCollider2D col = nodeObj.AddComponent<Collider2D>() as CircleCollider2D;
        NodeController ctrl = nodeObj.AddComponent<NodeController>();

        // Setup collider
        col.radius = _nodeRadius;
        col.isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("Node");

        // Setup sprite (placeholder circle)
        bg.color = _owner == Faction.Player ? _playerColor : _owner == Faction.Enemy ? _enemyColor : _neutralColor;
        bg.drawMode = SpriteDrawMode.Sliced;

        Debug.Log($"[NodeCreator] Created: {nodeObj.name}");
    }

    private void CreateAllNodePrefabs()
    {
        CreateNodeOfType(NodeType.Small, "Node_Small");
        CreateNodeOfType(NodeType.Medium, "Node_Medium");
        CreateNodeOfType(NodeType.Large, "Node_Large");
        CreateNodeOfType(NodeType.Fortress, "Node_Fortress");

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Complete", "All node prefabs created in Assets/Prefabs/", "OK");
    }

    private void CreateNodeOfType(NodeType type, string name)
    {
        GameObject nodeObj = new GameObject(name);

        CircleCollider2D col = nodeObj.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        SpriteRenderer bg = nodeObj.AddComponent<SpriteRenderer>();
        bg.color = Color.white;
        bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NodeCircle.png");

        // Add ring
        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(nodeObj.transform);
        SpriteRenderer ringSR = ringObj.AddComponent<SpriteRenderer>();
        ringSR.color = Color.white;
        ringSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NodeRing.png");
        ringObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        // Add text canvas
        GameObject textObj = new GameObject("SoldierText");
        textObj.transform.SetParent(nodeObj.transform);
        // TextMeshPro text would be added here

        nodeObj.AddComponent<NodeController>();

        // Save as prefab
        string path = $"Assets/Prefabs/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(nodeObj, path);
        DestroyImmediate(nodeObj);

        Debug.Log($"[NodeCreator] Prefab saved: {path}");
    }
}
