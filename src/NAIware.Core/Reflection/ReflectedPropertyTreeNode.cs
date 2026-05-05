using NAIware.Core.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NAIware.Core.Reflection
{
    /// <summary>
    /// A class that represents a tree node.
    /// </summary>
    public class ReflectedPropertyTreeNode : ITreeNode<PropertyInfo>
    {
        /// <summary>The value stored in this node.</summary>
        protected PropertyInfo? _value;
        /// <summary>The child nodes collection.</summary>
        protected ReflectedPropertyTreeNodes _nodes;
        /// <summary>The parent node reference.</summary>
        protected ITreeNode<PropertyInfo>? _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectedPropertyTreeNode"/> class.
        /// </summary>
        public ReflectedPropertyTreeNode() : this(null, null, null) { }

        /// <summary>
        /// Initializes a new instance with a parent and value.
        /// </summary>
        public ReflectedPropertyTreeNode(ITreeNode<PropertyInfo>? parent, PropertyInfo? value) : this(parent, value, null) { }

        /// <summary>
        /// Initializes a new instance with a parent.
        /// </summary>
        public ReflectedPropertyTreeNode(ITreeNode<PropertyInfo>? parent) : this(parent, null, null) { }

        /// <summary>
        /// Initializes a new instance with a value.
        /// </summary>
        public ReflectedPropertyTreeNode(PropertyInfo? value) : this(null, value, null) { }

        /// <summary>
        /// Initializes a new instance with a parent, value, and children.
        /// </summary>
        public ReflectedPropertyTreeNode(ITreeNode<PropertyInfo>? parent, PropertyInfo? value, ReflectedPropertyTreeNodes? children)
        {
            _parent = parent;
            _value = value;

            if (children is not null)
            {
                _nodes = children;
                _nodes.Parent = this;
            }
            else
            {
                _nodes = new ReflectedPropertyTreeNodes(this);
            }
        }

        /// <summary>
        /// Gets or sets the value stored in the node.
        /// </summary>
        /// <remarks>
        /// The underlying storage is nullable, but the interface contract returns non-null.
        /// Callers should be aware that a default-constructed node has a <c>null</c> value at runtime.
        /// </remarks>
        public PropertyInfo Value
        {
            get => _value!;
            set => _value = value;
        }

        /// <summary>
        /// Gets the collection of child nodes.
        /// </summary>
        public ReflectedPropertyTreeNodes Nodes => _nodes;

        /// <summary>
        /// Gets or sets the parent of the current node.
        /// </summary>
        public ITreeNode<PropertyInfo>? Parent
        {
            get => _parent;
            set => _parent = value;
        }

        ITreeNode<PropertyInfo>? ITreeNode<PropertyInfo>.Parent
        {
            get => _parent;
            set => _parent = value;
        }

        bool ITreeNode<PropertyInfo>.HasParent => _parent is not null;

        /// <summary>
        /// Gets whether this node has children.
        /// </summary>
        public bool HasChildren => _nodes is not null && _nodes.Count > 0;

        List<ITreeNode<PropertyInfo>> ITreeNode<PropertyInfo>.Children
        {
            get
            {
                var children = new List<ITreeNode<PropertyInfo>>();
                foreach (ReflectedPropertyTreeNode node in _nodes)
                    children.Add(node);
                return children;
            }
        }

        int ITreeNode<PropertyInfo>.Depth
        {
            get
            {
                if (!HasChildren) return 0;

                int largestDepth = -1;
                foreach (ITreeNode<PropertyInfo> node in _nodes)
                {
                    int currentDepth = node.Depth;
                    if (currentDepth > largestDepth) largestDepth = currentDepth;
                }

                return largestDepth;
            }
        }

        /// <summary>
        /// Gets the total number of nodes in this subtree (including self).
        /// </summary>
        public long Size
        {
            get
            {
                long totalSize = 1;
                foreach (ReflectedPropertyTreeNode node in _nodes)
                {
                    totalSize += node.Size;
                }
                return totalSize;
            }
        }
    }
}
