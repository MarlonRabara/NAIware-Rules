namespace NAIware.Rules.Models;

/// <summary>
/// A grouping mechanism under a <see cref="RuleContext"/> that organizes
/// <see cref="RuleExpression"/> instances into named execution sets.
/// <para>
/// Categories form a tree: a category may contain any number of nested
/// <see cref="Subcategories"/> and tracks its <see cref="ParentCategory"/>
/// (null when the category is attached directly to a <see cref="RuleContext"/>).
/// </para>
/// <para>
/// A category has a many-to-many relationship with expressions via
/// <see cref="RuleCategoryExpression"/>.
/// </para>
/// </summary>
public class RuleCategory
{
    private readonly Guid _identity;
    private readonly string _name;
    private readonly List<RuleCategoryExpression> _categoryExpressions = [];
    private readonly List<RuleCategory> _subcategories = [];

    /// <summary>Creates a new rule category.</summary>
    public RuleCategory(string name, string description = "")
        : this(Guid.NewGuid(), name, description)
    {
    }

    /// <summary>Creates a new rule category with an explicit identity.</summary>
    public RuleCategory(Guid identity, string name, string description = "")
    {
        _identity = identity;
        _name = name;
        Description = description;
    }

    /// <summary>Gets the unique identity of the category.</summary>
    public Guid Identity => _identity;

    /// <summary>Gets the name of the category.</summary>
    public string Name => _name;

    /// <summary>Gets or sets the description of the category.</summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets the parent category, or null when this category is a top-level
    /// category attached directly to a <see cref="RuleContext"/>.
    /// </summary>
    public RuleCategory? ParentCategory { get; private set; }

    /// <summary>Gets the nested subcategories of this category.</summary>
    public IReadOnlyList<RuleCategory> Subcategories => _subcategories;

    /// <summary>Gets the join entities linking this category to its expressions.</summary>
    public List<RuleCategoryExpression> CategoryExpressions => _categoryExpressions;

    /// <summary>
    /// Gets the dotted path from the root category to this category
    /// (e.g., <c>"Eligibility.Age"</c>). For top-level categories this
    /// equals <see cref="Name"/>.
    /// </summary>
    public string Path => ParentCategory is null ? _name : $"{ParentCategory.Path}.{_name}";

    /// <summary>Gets the depth of this category in the hierarchy. Top-level categories return 0.</summary>
    public int Depth
    {
        get
        {
            int depth = 0;
            RuleCategory? cursor = ParentCategory;
            while (cursor is not null)
            {
                depth++;
                cursor = cursor.ParentCategory;
            }
            return depth;
        }
    }

    /// <summary>
    /// Adds a rule expression to this category with the specified ordinal.
    /// If no ordinal is provided, it is appended at the end.
    /// </summary>
    public RuleCategoryExpression AddExpression(RuleExpression expression, int? ordinal = null)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var join = new RuleCategoryExpression(
            _identity,
            expression.Identity,
            ordinal ?? _categoryExpressions.Count,
            expression);

        _categoryExpressions.Add(join);
        return join;
    }

    /// <summary>
    /// Creates a new subcategory owned by this category and returns it.
    /// The returned category has its <see cref="ParentCategory"/> set to this instance.
    /// </summary>
    /// <param name="name">The subcategory name.</param>
    /// <param name="description">Optional subcategory description.</param>
    public RuleCategory AddSubcategory(string name, string description = "")
    {
        var child = new RuleCategory(name, description);
        AttachSubcategory(child);
        return child;
    }

    /// <summary>
    /// Attaches an existing category as a subcategory of this category.
    /// Throws if the candidate already has a parent or would introduce a cycle.
    /// </summary>
    /// <param name="subcategory">The category to attach as a child.</param>
    public void AttachSubcategory(RuleCategory subcategory)
    {
        ArgumentNullException.ThrowIfNull(subcategory);

        if (subcategory == this)
            throw new InvalidOperationException("A category cannot be a subcategory of itself.");

        if (subcategory.ParentCategory is not null)
            throw new InvalidOperationException(
                $"Category '{subcategory.Name}' is already attached to '{subcategory.ParentCategory.Name}'. " +
                "Detach it before re-attaching.");

        if (IsDescendantOf(subcategory))
            throw new InvalidOperationException(
                $"Cannot attach '{subcategory.Name}' under '{Name}' because it would create a cycle.");

        subcategory.ParentCategory = this;
        _subcategories.Add(subcategory);
    }

    /// <summary>
    /// Detaches a subcategory from this category and clears its parent link.
    /// Returns true when the subcategory was found and removed.
    /// </summary>
    public bool DetachSubcategory(RuleCategory subcategory)
    {
        ArgumentNullException.ThrowIfNull(subcategory);

        if (!_subcategories.Remove(subcategory)) return false;
        subcategory.ParentCategory = null;
        return true;
    }

    /// <summary>
    /// Finds a direct subcategory by name (non-recursive).
    /// Use <see cref="FindByPath"/> for dotted-path lookup.
    /// </summary>
    public RuleCategory? FindSubcategoryByName(string name) =>
        _subcategories.Find(c => string.Equals(c.Name, name, StringComparison.Ordinal));

    /// <summary>
    /// Resolves a descendant category by dotted path relative to this category
    /// (e.g., <c>"Age.Senior"</c>). Returns null if any segment is missing.
    /// </summary>
    /// <param name="path">A dotted path of category names.</param>
    public RuleCategory? FindByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        RuleCategory? cursor = this;
        foreach (string segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            cursor = cursor?.FindSubcategoryByName(segment);
            if (cursor is null) return null;
        }
        return cursor;
    }

    /// <summary>Gets the ordered rule expressions attached directly to this category.</summary>
    public IEnumerable<RuleExpression> GetExpressions() =>
        _categoryExpressions
            .OrderBy(ce => ce.Ordinal)
            .Select(ce => ce.Expression);

    /// <summary>Gets only the active rule expressions attached directly to this category.</summary>
    public IEnumerable<RuleExpression> GetActiveExpressions() =>
        GetExpressions().Where(e => e.IsActive);

    /// <summary>
    /// Gets every rule expression in this category and all nested subcategories,
    /// deduplicated by <see cref="RuleExpression.Identity"/>.
    /// </summary>
    public IEnumerable<RuleExpression> GetAllExpressions()
    {
        var seen = new HashSet<Guid>();
        foreach (RuleExpression expression in EnumerateAll(this))
        {
            if (seen.Add(expression.Identity))
                yield return expression;
        }
    }

    /// <summary>
    /// Gets every active rule expression in this category and all nested subcategories,
    /// deduplicated by <see cref="RuleExpression.Identity"/>.
    /// </summary>
    public IEnumerable<RuleExpression> GetAllActiveExpressions() =>
        GetAllExpressions().Where(e => e.IsActive);

    /// <summary>
    /// Enumerates this category followed by every descendant, depth-first.
    /// </summary>
    public IEnumerable<RuleCategory> EnumerateDescendants()
    {
        yield return this;
        foreach (RuleCategory child in _subcategories)
            foreach (RuleCategory descendant in child.EnumerateDescendants())
                yield return descendant;
    }

    private bool IsDescendantOf(RuleCategory candidate)
    {
        foreach (RuleCategory node in candidate.EnumerateDescendants())
            if (node == this) return true;
        return false;
    }

    private static IEnumerable<RuleExpression> EnumerateAll(RuleCategory category)
    {
        foreach (RuleExpression expression in category.GetExpressions())
            yield return expression;

        foreach (RuleCategory child in category._subcategories)
            foreach (RuleExpression expression in EnumerateAll(child))
                yield return expression;
    }
}
