using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class DialogueNode: IEquatable<DialogueNode>
{
    [SerializeField] private string _uniqueID;
    [SerializeField][TextArea] private string _content;
    [SerializeField] private List<string> _children;
    [SerializeField] private Rect _nodeRect = new Rect(10, 50, 200, 100);
    [SerializeField] private Vector2 _nodeScroll = new Vector2(0, 0);


    public DialogueNode()
    {
        _uniqueID = "";
        _content = "";
        _children = new List<string>();
        _nodeScroll = new Vector2(0, 0);
    }
    public DialogueNode(string id, string content)
    {
        _uniqueID = id;
        _content = content;
        _children = new List<string>();
    }
    public string UniqueID { get => _uniqueID; }
    public string Content { get => _content; }
    public List<string> Children { get => _children; }
    public Rect NodeRect { get => _nodeRect; }
    public Vector2 ScrollPosition { get => _nodeScroll; }

    public bool Equals(DialogueNode other)
    {
        return other.UniqueID.Equals(UniqueID);
    }

    public void SetContent(string content)
    {
        _content = content;
    }
    public void SetID(string ID)
    {
        _uniqueID = ID;
    }
    public void SetRectPosition(Vector2 position)
    {
        position = new Vector2(Mathf.Clamp(position.x, 0, 5000), Mathf.Clamp(position.y, 20, 5000));
        _nodeRect.position = position;
    }
    public void SetScrollPosition(Vector2 scroll)
    {
        _nodeScroll = scroll;
    }
    public void RemoveChild(DialogueNode node)
    {
        RemoveChild(node.UniqueID);
    }
    public void RemoveChild(string id)
    {
        _children.Remove(id);
    }
    public void AddChild(DialogueNode node)
    {
        AddChild(node.UniqueID);
    }
    public void AddChild(string id)
    {
        if (_children.Contains(id)) return;
        _children.Add(id);
    }
    public bool HadChild(DialogueNode node)
    {
        return HasChild(node.UniqueID);
    }
    public bool HasChild(string id)
    {
        return _children.Contains(id);
    }
}
