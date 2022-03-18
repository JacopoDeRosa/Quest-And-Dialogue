using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;

public class DialogueEditorWindow : EditorWindow
{
    private const float curveSmoothness = 100;
    private const float connectorsSize = 25;
    private const float canvasSize = 5000;
    private const float sidebarWidth = 250;

    private Dialogue _activeDialogue;

    [NonSerialized]
    private GUIStyle _nodeStyle = null;
    [NonSerialized]
    private GUIStyle _headerStyle = null;
    [NonSerialized]
    private GUIStyle _inputStyle = null;
    [NonSerialized]
    private GUIStyle _outputStyle = null;
    [NonSerialized]
    private DialogueNode _draggingNode;
    [NonSerialized]
    private DialogueNode _parentingNode;
    [NonSerialized]
    private DialogueNode _unparentingNode;
    [NonSerialized]
    private Vector2 _dragOffset;
    [NonSerialized]
    private Vector2 _utilityCurveController;
    [NonSerialized]
    private DialogueNode _creatingNode = null;
    [NonSerialized]
    private DialogueNode _nodeToDelete = null;
    [NonSerialized]
    private bool _menuOpen = false;
    [NonSerialized]
    private Vector2 _menuPosition;

    private Vector2 _scrollPosition = Vector2.zero;



    private Vector2 AdjustedMousePosition { get => Event.current.mousePosition + _scrollPosition - new Vector2(sidebarWidth, 0); }

    [MenuItem("Window/Dialogue Editor")]
    public static DialogueEditorWindow ShowEditorWindow()
    {
        return GetWindow<DialogueEditorWindow>(false, "Dialogue Editor");
    }

    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        Dialogue dialogue = EditorUtility.InstanceIDToObject(instanceID) as Dialogue;

        if (dialogue != null)
        {
            var window = ShowEditorWindow();
            window.SetActiveDialogue(dialogue);
            return true;
        }
        return false;
    }

    private void OnGUI()
    {
        if (_activeDialogue == null)
        {
            ProcessScrollMove();

            // If no dialogue is selected just draw an empty window 

            EditorGUILayout.BeginHorizontal();

            // Start drawing the sidebar
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("No asset selected");
            EditorGUILayout.Space(50);
            EditorGUILayout.EndVertical();
            // Stop drawing the sidebar

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, true, true);

            // Used to reserve a canavas for the scroll view
            Rect canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);

            // Draw Grid Background
            DrawGridBackground(canvas);
            DrawScrollShadow();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            ProcessEvents();
            EditorGUILayout.BeginHorizontal();

            // Start drawing the sidebar
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            EditorGUILayout.Space(10);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 25;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.clipping = TextClipping.Overflow;

            EditorGUILayout.LabelField(_activeDialogue.name, titleStyle);

            EditorGUILayout.Space(25);

            if (GUILayout.Button("Add New Node"))
            {
                _activeDialogue.CreateNewNode();
            }
            EditorGUILayout.EndVertical();
            // Stop drawing the sidebar


            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Used to reserve a canavas for the scroll view
            Rect canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);

            // Draw Grid Background
            DrawGridBackground(canvas);

            // Draw utility curve for node parenting and unparenting
            if (_parentingNode != null)
            {
                DrawBezierAuto(GetNodeOutputPosition(_parentingNode), _utilityCurveController);
            }
            else if (_unparentingNode != null)
            {
                IEnumerable<DialogueNode> parents = _activeDialogue.GetNodeParents(_unparentingNode);
                foreach (var parent in parents)
                {
                    if (parent != null)
                    {
                        Vector3 outPut = GetNodeOutputPosition(parent);
                        DrawBezierAuto(outPut, _utilityCurveController);
                    }
                }
            }

            // Draw Nodes and Connectors
            foreach (var node in _activeDialogue.Nodes)
            {
                DrawConnectorsGUI(node);
            }
            foreach (var node in _activeDialogue.Nodes)
            {
                DrawNodeGUI(node);
            }

            DrawScrollShadow();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();

            if (_menuOpen)
            {
                DrawMouseMenu();
            }

            // Delete or create nodes after all iterations to avoid conflicts
            if (_creatingNode != null)
            {
                Undo.RecordObject(_activeDialogue, "Created new node");
                _activeDialogue.CreateNewNode(_creatingNode);
                _creatingNode = null;
            }
            if (_nodeToDelete != null)
            {

                Undo.RecordObject(_activeDialogue, "Deleted node");
                _activeDialogue.DeleteNode(_nodeToDelete);
                _nodeToDelete = null;
            }


        }
    }

    private void DrawGridBackground(Rect canvas)
    {
        Texture2D backgroundTexture = EditorGUIUtility.Load("nodes_background.png") as Texture2D;

        Rect gridRect = new Rect(0, 0, 100, 100);

        GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, gridRect);


    }
    private void DrawScrollShadow()
    {
        Texture2D shadowTexture = EditorGUIUtility.Load("nodes_shadow.png") as Texture2D;
        Rect shadowRect = new Rect(_scrollPosition.x, _scrollPosition.y, position.width - sidebarWidth - 13, position.height - 13);
        GUI.DrawTexture(shadowRect, shadowTexture);
    }
    private void ProcessEvents()
    {
        ProcessScrollMove();
        ProcessNodeParenting();
        ProcessNodeUnparenting();
        ProcessNodeDrag();
        ProcessMouseMenu();
    }
    private void ProcessScrollMove()
    {
        if (Event.current.button != 2) return;

        if (Event.current.type == EventType.MouseDrag)
        {
            _scrollPosition -= Event.current.delta;
            GUI.changed = true;
        }
    }
    private void ProcessNodeDrag()
    {
        if (Event.current.button != 0) return;

        if (Event.current.type == EventType.MouseDown && _draggingNode == null)
        {
            SetDraggingNode();
            if (_draggingNode != null)
            {
                _dragOffset = _draggingNode.NodeRect.position - Event.current.mousePosition;
            }
        }
        else if (Event.current.type == EventType.MouseDrag && _draggingNode != null)
        {
            Undo.RecordObject(_activeDialogue, "Node Move");
            _draggingNode.SetRectPosition(Event.current.mousePosition + _dragOffset);
            GUI.changed = true;

        }
        else if (Event.current.type == EventType.MouseUp && _draggingNode != null)
        {
            _draggingNode = null;
        }
    }
    private void ProcessNodeParenting()
    {
        if (Event.current.button != 0) return;

        if (Event.current.type == EventType.MouseDown && _parentingNode == null)
        {
            SetParentingNode();
        }

        else if (Event.current.type == EventType.MouseDrag && _parentingNode != null)
        {
            _utilityCurveController = AdjustedMousePosition;
            GUI.changed = true;
        }
        else if (Event.current.type == EventType.MouseUp && _parentingNode != null)
        {
            Undo.RecordObject(_activeDialogue, "Changed Node Parent");
            foreach (var node in _activeDialogue.Nodes)
            {
                if (node.Equals(_parentingNode)) continue;

                Rect inputRect = GetNodeInputRect(node);
                if (inputRect.Contains(AdjustedMousePosition))
                {
                    _parentingNode.AddChild(node);
                    break;
                }
            }
            _parentingNode = null;
            GUI.changed = true;
        }

    }
    private void ProcessNodeUnparenting()
    {
        if (Event.current.button != 0) return;

        if (Event.current.type == EventType.MouseDown && _unparentingNode == null)
        {
            SetUnparentingNode();
        }

        else if (Event.current.type == EventType.MouseDrag && _unparentingNode != null)
        {
            _utilityCurveController = AdjustedMousePosition;
            GUI.changed = true;
        }
        else if (Event.current.type == EventType.MouseUp && _unparentingNode != null)
        {
            Undo.RecordObject(_activeDialogue, "Changed Node Parent");
            foreach (var node in _activeDialogue.Nodes)
            {
                Rect output = GetNodeOutputRect(node);
                if (output.Contains(AdjustedMousePosition))
                {
                    if (node.HadChild(_unparentingNode) == false) break;
                    node.RemoveChild(_unparentingNode);
                    break;
                }
            }
            _unparentingNode = null;
            GUI.changed = true;
        }

    }
    private void ProcessMouseMenu()
    {
        if (Event.current.button != 1) return;
        if (Event.current.type == EventType.MouseDown && _menuOpen == false)
        {
            _menuOpen = true;
            _menuPosition = Event.current.mousePosition;
            GUI.changed = true;
        }
        else if (Event.current.type == EventType.MouseUp && _menuOpen)
        {
            _menuOpen = false;
            GUI.changed = true;
        }
    }
    private void SetDraggingNode()
    {
        foreach (var node in _activeDialogue.Nodes)
        {
            Rect headerRect = GetHeaderRect(node);
            if (headerRect.Contains(AdjustedMousePosition))
            {
                _draggingNode = node;
            }
        }
    }
    private void SetParentingNode()
    {
        foreach (var node in _activeDialogue.Nodes)
        {
            Rect ouptutRect = GetNodeOutputRect(node);
            if (ouptutRect.Contains(AdjustedMousePosition))
            {
                _parentingNode = node;
            }
        }
    }
    private void SetUnparentingNode()
    {
        foreach (var node in _activeDialogue.Nodes)
        {
            Rect inputRect = GetNodeInputRect(node);
            if (inputRect.Contains(AdjustedMousePosition))
            {
                _unparentingNode = node;
            }
        }
    }
    private void DrawMouseMenu()
    {
        float menuWith = 100;
        float menuHeight = 400;
        Vector2 positionAdj = _menuPosition - new Vector2(menuWith / 2, -20);
        Rect menuRect = new Rect(positionAdj, new Vector2(menuWith, menuHeight));

        GUILayout.BeginArea(menuRect);
        GUILayout.Label("Controls Menu");
        GUILayout.Button("Add Node");
        GUILayout.EndArea();
    }
    private void DrawNodeGUI(DialogueNode node)
    {
        // Draw a header to allow dragging without impacting text labels    
        GUILayout.BeginArea(GetHeaderRect(node), _headerStyle);

        GUIStyle nodeLableStyle = new GUIStyle(GUI.skin.label);
        nodeLableStyle.alignment = TextAnchor.MiddleCenter;
        nodeLableStyle.padding = new RectOffset(0, 0, 0, 0);
        nodeLableStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.LabelField("Dialogue Node", nodeLableStyle);
        GUILayout.EndArea();
        // End of header

        // Draw the main body of the node
        GUILayout.BeginArea(node.NodeRect, _nodeStyle);
        node.SetScrollPosition(EditorGUILayout.BeginScrollView(node.ScrollPosition));

        EditorGUI.BeginChangeCheck();

        string content = EditorGUILayout.TextArea(node.Content);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_activeDialogue, "Change Content");
            node.SetContent(content);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+"))
        {
            _creatingNode = node;
        }
        if (GUILayout.Button("-"))
        {
            _nodeToDelete = node;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
        // End of main body

        // Draw node Input
        GUILayout.BeginArea(GetNodeInputRect(node), _inputStyle);
        GUILayout.EndArea();
        // End of Input

        // Draw node Output
        GUILayout.BeginArea(GetNodeOutputRect(node), _outputStyle);
        GUILayout.EndArea();
        // End of Output
    }
    private Rect GetHeaderRect(DialogueNode node)
    {
        return new Rect(node.NodeRect.x, node.NodeRect.y - 25, node.NodeRect.width, 38);
    }
    private Rect GetNodeInputRect(DialogueNode node)
    {
        Rect nodeRect = node.NodeRect;
        return new Rect(nodeRect.center.x - (nodeRect.width / 2) - (connectorsSize / 4), nodeRect.center.y - (connectorsSize / 2), connectorsSize, connectorsSize);
    }
    private Rect GetNodeOutputRect(DialogueNode node)
    {
        Rect nodeRect = node.NodeRect;
        return new Rect(nodeRect.center.x + (nodeRect.width / 2) - (connectorsSize / 1.5f), nodeRect.center.y - (connectorsSize / 2), connectorsSize, connectorsSize);
    }
    private void DrawConnectorsGUI(DialogueNode node)
    {
        Vector3 startPos = GetNodeOutputPosition(node);

        foreach (var child in _activeDialogue.GetNodeChildren(node))
        {
            if (_unparentingNode != null)
            {
                if (child.Equals(_unparentingNode)) continue;
            }
            Vector3 endPos = GetNodeInputPosition(child);
            DrawBezierAuto(startPos, endPos);
        }
    }
    private Vector3 GetNodeOutputPosition(DialogueNode node)
    {
        return node.NodeRect.center + new Vector2(node.NodeRect.width / 2, 0);
    }
    private Vector3 GetNodeInputPosition(DialogueNode node)
    {
        return node.NodeRect.center + new Vector2(-node.NodeRect.width / 2, 0);
    }
    private void DrawBezierAuto(Vector3 startPos, Vector3 endPos)
    {
        Vector3 offSet = endPos - startPos;
        offSet.y = 0;
        offSet.x = Mathf.Clamp(offSet.x, 0, curveSmoothness);
        Vector3 endHandle = endPos - offSet;
        Vector3 startHandle = startPos + offSet;

        Handles.DrawBezier(startPos, endPos, startHandle, endHandle, Color.white, null, 4f);
    }

    private void OnEnable()
    {
        GenerateStyles();
    }
    private void GenerateStyles()
    {
        // use node4 for output and node3 for input
        // use node6 for the exit node?
        Texture2D nodeTexture = (Texture2D)EditorGUIUtility.Load("node0");
        Texture2D headerTexture = (Texture2D)EditorGUIUtility.Load("node1");
        Texture2D inputTexture = (Texture2D)EditorGUIUtility.Load("node3");
        Texture2D outputTexture = (Texture2D)EditorGUIUtility.Load("node4");
        RectOffset borderRect = new RectOffset(12, 12, 12, 12);

        _inputStyle = new GUIStyle();
        _inputStyle.normal.background = inputTexture;
        _inputStyle.border = borderRect;

        _outputStyle = new GUIStyle();
        _outputStyle.normal.background = outputTexture;
        _outputStyle.border = borderRect;

        _headerStyle = new GUIStyle();
        _headerStyle.normal.background = headerTexture;
        _headerStyle.padding = new RectOffset(20, 20, 7, 7);
        _headerStyle.border = borderRect;

        _nodeStyle = new GUIStyle();
        _nodeStyle.normal.background = nodeTexture;
        _nodeStyle.padding = new RectOffset(20, 20, 10, 10);
        _nodeStyle.border = borderRect;
    }

    private void OnSelectionChange()
    {
        Dialogue dialogue = Selection.activeObject as Dialogue;
        if (dialogue != null)
        {
            SetActiveDialogue(dialogue);
        }
    }
    public void SetActiveDialogue(Dialogue dialogue)
    {
        _activeDialogue = dialogue;
        Repaint();
    }

    private void OnLostFocus()
    {
        _draggingNode = null;
        _nodeToDelete = null;
        _parentingNode = null;
        _unparentingNode = null;
    }
}
