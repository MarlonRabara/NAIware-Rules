using System.Text.Json.Serialization;

namespace NAIware.Rules.Models;

/// <summary>
/// A grouping mechanism under a <see cref="RuleContext"/> that organizes
/// <see cref="RuleExpression"/> instances into named execution sets.
/// Categories can be nested to any practical depth; rule expressions live at category leaf nodes.
/// </summary>
public class RuleCategory
{
    /// <summary>Creates a new rule category.</summary>
    public RuleCategory()
    {
        Identity = Guid.NewGuid();
        Name = string.Empty;
        Description = string.Empty;
    }

    /// <summary>Creates a new rule category.</summary>
    public RuleCategory(string name, string description = "")
        : this(Guid.NewGuid(), name, description)
    {
    }

    /// <summary>Creates a new rule category with an explicit identity.</summary>
    public RuleCategory(Guid identity, string name, string description = "")
    {
        Identity = identity;
        Name = name;
        Description = description;
    }

    /// <summary>Gets or sets the unique identity of the category.</summary>
    public Guid Identity { get; set; }

    /// <summary>Gets or sets the category name.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the category description.</summary>
    public string Description { get; set; }

    /// <summary>Gets the parent category, or null when this is a top-level category.</summary>
    [JsonIgnore]
    public RuleCategory? ParentCategory { get; private set; }

    /// <summary>Gets or sets nested subcategories.</summary>
    public List<RuleCategory> Categories { get; set; } = [];

    /// <summary>Gets or sets the persisted identity of the parent category, or null for a root category.</summary>
    public Guid? ParentCategoryIdentity { get; set; }

    /// <summary>Gets whether this category has no child categories and is directly executable.</summary>
    [JsonIgnore]
    public bool IsLeaf => Categories.Count == 0;

    /// <summary>Gets nested subcategories.</summary>
    [JsonIgnore]
    public IReadOnlyList<RuleCategory> Subcategories => Categories;

    /// <summary>Gets or sets the join entities linking this category to its expressions.</summary>
    [JsonIgnore]
    public List<RuleCategoryExpression> CategoryExpressions { get; set; } = [];

    /// <summary>Gets or sets expression ids used by the editor tree and persistence layer.</summary>
    public List<Guid> ExpressionIds { get; set; } = [];

    /// <summary>Gets the dotted path from the root category to this category.</summary>
    [JsonIgnore]
    public string Path => ParentCategory is null ? Name : $"{ParentCategory.Path}.{Name}";

    /// <summary>Gets the depth of this category in the hierarchy. Top-level categories return 0.</summary>
    [JsonIgnore]
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

    /// <summary>Adds a rule expression to this category.</summary>
    public RuleCategoryExpression AddExpression(RuleExpression expression, int? ordinal = null)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (!ExpressionIds.Contains(expression.Identity)) ExpressionIds.Add(expression.Identity);

        var existing = CategoryExpressions.FirstOrDefault(e => e.ExpressionIdentity == expression.Identity);
        if (existing is not null)
        {
            existing.Expression = expression;
            return existing;
        }

        var join = new RuleCategoryExpression(Identity, expression.Identity, ordinal ?? CategoryExpressions.Count, expression);
        CategoryExpressions.Add(join);
        return join;
    }

    /// <summary>Creates a nested subcategory owned by this category.</summary>
    public RuleCategory AddSubcategory(string name, string description = "")
    {
        var child = new RuleCategory(name, description);
        AttachSubcategory(child);
        return child;
    }

    /// <summary>Attaches an existing category as a subcategory of this category.</summary>
    public void AttachSubcategory(RuleCategory subcategory)
    {
        ArgumentNullException.ThrowIfNull(subcategory);
        if (subcategory == this) throw new InvalidOperationException("A category cannot be a subcategory of itself.");
        if (IsDescendantOf(subcategory)) throw new InvalidOperationException("The category move would create a cycle.");
        subcategory.ParentCategory = this;
        subcategory.ParentCategoryIdentity = Identity;
        if (!Categories.Contains(subcategory)) Categories.Add(subcategory);
    }

    /// <summary>Detaches a subcategory from this category.</summary>
    public bool DetachSubcategory(RuleCategory subcategory)
    {
        ArgumentNullException.ThrowIfNull(subcategory);
        if (!Categories.Remove(subcategory)) return false;
        subcategory.ParentCategory = null;
        subcategory.ParentCategoryIdentity = null;
        return true;
    }

    /// <summary>Finds a direct subcategory by name.</summary>
    public RuleCategory? FindSubcategoryByName(string name) =>
        Categories.Find(c => string.Equals(c.Name, name, StringComparison.Ordinal));

    /// <summary>Resolves a descendant category by dotted path relative to this category.</summary>
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
        CategoryExpressions
            .Where(ce => ce.Expression is not null)
            .OrderBy(ce => ce.Ordinal)
            .Select(ce => ce.Expression!);

    /// <summary>Gets only the active rule expressions attached directly to this category.</summary>
    public IEnumerable<RuleExpression> GetActiveExpressions() => GetExpressions().Where(e => e.IsActive);

    /// <summary>Gets every rule expression in this category and all nested subcategories.</summary>
    public IEnumerable<RuleExpression> GetAllExpressions()
    {
        var seen = new HashSet<Guid>();
        foreach (RuleExpression expression in EnumerateAll(this))
        {
            if (seen.Add(expression.Identity)) yield return expression;
        }
    }

    /// <summary>Gets every active rule expression in this category and all nested subcategories.</summary>
    public IEnumerable<RuleExpression> GetAllActiveExpressions() => GetAllExpressions().Where(e => e.IsActive);

    /// <summary>Enumerates this category followed by every descendant, depth-first.</summary>
    public IEnumerable<RuleCategory> EnumerateDescendants()
    {
        yield return this;
        foreach (RuleCategory child in Categories)
        {
            child.ParentCategory = this;
            foreach (RuleCategory descendant in child.EnumerateDescendants())
                yield return descendant;
        }
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

        foreach (RuleCategory child in category.Categories)
            foreach (RuleExpression expression in EnumerateAll(child))
                yield return expression;
    }
}
