# Completion Algorithm

The completion engine follows this sequence:

1. Capture text before cursor.
2. Detect active token / typed prefix.
3. Detect syntactic context:
   - root symbol
   - member access after `.`
   - operator after scalar path
   - value after comparison operator
   - logical connector after completed value
4. Resolve any path to a `RuleCompletionNode`.
5. Infer expected type from the resolved node.
6. Generate candidates:
   - root / child nodes
   - member nodes
   - type-compatible operators
   - literal values
   - logical operators
7. Prefix-filter candidates.
8. Rank candidates.
9. Return replacement span and completion items.

## Examples

### Root

```text
Pro
```

Suggests root-level properties matching `Pro`.

### Member access

```text
Property.
```

Suggests child properties under `Property`.

### Operator

```text
Property.Value
```

If `Property.Value` is scalar, suggests operators valid for its type.

### Value

```text
Property.Value >= 
```

Suggests values compatible with the type of `Property.Value`.

### Logical connector

```text
Property.Value >= 500
```

Suggests:

```text
and
or
&&
||
```
