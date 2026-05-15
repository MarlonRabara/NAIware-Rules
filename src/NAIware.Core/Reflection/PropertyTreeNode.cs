using NAIware.Core.Collections;

namespace NAIware.Core.Reflection;

/// <summary>
/// A strongly typed tree node whose value is a <see cref="ReflectedPropertyNode"/>.
/// </summary>
public sealed class PropertyTreeNode : ITreeNode<ReflectedPropertyNode>
{
    private ReflectedPropertyNode _value;
    private ITreeNode<ReflectedPropertyNode>? _parent;
    private readonly List<PropertyTreeNode> _children = new();

    /// <summary>Initializes a new instance of the <see cref="PropertyTreeNode"/> class.</summary>
    /// <param name="value">The reflected property descriptor stored in the node.</param>
    /// <param name="parent">The optional parent node.</param>
    public PropertyTreeNode(ReflectedPropertyNode value, PropertyTreeNode? parent = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        _parent = parent;
    }

    /// <inheritdoc/>
    public ReflectedPropertyNode Value
    {
        get => _value;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _value = value;
        }
    }

    /// <inheritdoc/>
    public ITreeNode<ReflectedPropertyNode>? Parent
    {
        get => _parent;
        set => _parent = value;
    }

    /// <summary>Gets the typed child nodes of this node.</summary>
    public IReadOnlyList<PropertyTreeNode> Children => _children;

    List<ITreeNode<ReflectedPropertyNode>> ITreeNode<ReflectedPropertyNode>.Children
    {
        get
        {
            var list = new List<ITreeNode<ReflectedPropertyNode>>(_children.Count);
            foreach (var child in _children)
            {
                list.Add(child);
            }
            return list;
        }
    }

    /// <inheritdoc/>
    public bool HasParent => _parent is not null;

    /// <inheritdoc/>
    public bool HasChildren => _children.Count > 0;

    /// <inheritdoc/>
    public int Depth
    {
        get
        {
            if (_children.Count == 0)
            {
                return 0;
            }

            int max = 0;
            foreach (var child in _children)
            {
                int d = child.Depth + 1;
                if (d > max)
                {
                    max = d;
                }
            }
            return max;
        }
    }

    /// <inheritdoc/>
    public long Size
    {
        get
        {
            long total = 1;
            foreach (var child in _children)
            {
                total += child.Size;
            }
            return total;
        }
    }

    /// <summary>Appends a child node, wiring up the parent relationship.</summary>
    /// <param name="child">The child node to append.</param>
    public void AddChild(PropertyTreeNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        child.Parent = this;
        _children.Add(child);
    }
}
