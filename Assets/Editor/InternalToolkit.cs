using UnityEngine;
using UnityEditor;


public class InternalToolkit : EditorWindow
{
    
    [MenuItem("Window/Tools/Tool Kit")]
    public static void ShowWindow()
    {
       var window = GetWindow<InternalToolkit>("Toolkit");
    }

    [SerializeField] private string _filePath;

    private GameObject _currentSelection;

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Game Toolkit", EditorStyles.boldLabel);

        if(GUILayout.Button("Make Blue"))
        {
            SetColor(Color.blue);
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Make Red"))
        {
            SetColor(Color.red);
        }
        if (GUILayout.Button("Make Green"))
        {
            SetColor(Color.green);
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Spawning", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Spawn Sphere"))
        {
          
        }
        if (GUILayout.Button("Spawn Cube"))
        {
            
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();  
    }

    private void OnFocus()
    {
        _currentSelection = Selection.activeGameObject;
    }

    private void OnLostFocus()
    {
        _currentSelection = null;
    }

    private void SetColor(Color color)
    {
        if (_currentSelection == null) return;
        Renderer renderer = _currentSelection.GetComponent<Renderer>();
        if (renderer == null) return;
        renderer.sharedMaterial.color = color;
    }
    
    
}

