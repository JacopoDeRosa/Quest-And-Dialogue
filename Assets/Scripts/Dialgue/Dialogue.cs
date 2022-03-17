using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "New Dialogue")]
public class Dialogue : ScriptableObject
{
    [SerializeField] private List<DialogueNode> _dialogueNodes = new List<DialogueNode>();

    private Dictionary<string, DialogueNode> _nodesDictionary = new Dictionary<string, DialogueNode>();

    private void Awake()
    {
        BuildNodesDictionary();
        if (_dialogueNodes.Count == 0)
        {
            var newNode = new DialogueNode();
            newNode.SetID(System.Guid.NewGuid().ToString());
            _dialogueNodes.Add(newNode);
        }
    }

    // On validate is not actually called in build use awake instead
    private void OnValidate()
    {
        BuildNodesDictionary();
    }

    private void BuildNodesDictionary()
    {
        _nodesDictionary.Clear();
        foreach (var node in _dialogueNodes)
        {
            _nodesDictionary.Add(node.UniqueID, node);
        }
    }
    public IEnumerable<DialogueNode> Nodes { get => _dialogueNodes; }

    public DialogueNode RootNode { get => _dialogueNodes[0]; }

    public IEnumerable<DialogueNode> GetNodeChildren(DialogueNode node)
    { 
        foreach(var child in node.Children)
        {
            if (_nodesDictionary.ContainsKey(child))
            {
                yield return _nodesDictionary[child];
            }
        }
    }

    public void CreateNewNode()
    {
        var newNode = new DialogueNode();
        newNode.SetID(System.Guid.NewGuid().ToString());
        _dialogueNodes.Add(newNode);
        _nodesDictionary.Add(newNode.UniqueID, newNode);
    }

    public void CreateNewNode(DialogueNode parent)
    {
        var newNode = new DialogueNode();
        newNode.SetID(System.Guid.NewGuid().ToString());
        _dialogueNodes.Add(newNode);
        parent.Children.Add(newNode.UniqueID);
        _nodesDictionary.Add(newNode.UniqueID, newNode);
        newNode.SetRectPosition(parent.NodeRect.position + new Vector2(parent.NodeRect.width + 20, 0));
    }

    public void DeleteNode(DialogueNode node)
    {
        _dialogueNodes.Remove(node);
        BuildNodesDictionary();
        foreach (DialogueNode parentNode in _dialogueNodes)
        {
            node.Children.Remove(node.UniqueID);
        }
    }

    public DialogueNode GetNodeParent(DialogueNode node)
    {
        foreach (DialogueNode possibleParent in _dialogueNodes)
        {
            if(possibleParent.Children.Contains(node.UniqueID))
            {
                return possibleParent;
            }
        }

        return null;
    }

    public IEnumerable<DialogueNode> GetNodeParents(DialogueNode node)
    {
        foreach (DialogueNode possibleParent in _dialogueNodes)
        {
            if (possibleParent.Children.Contains(node.UniqueID))
            {
               yield return possibleParent;
            }
        }

    }

  
}
