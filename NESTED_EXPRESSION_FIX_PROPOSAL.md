# Proposal: Support Direct Node Parameters for [NestedExpression]

## Problem

The current `[NestedExpression]` attribute handler only accepts string literals that get parsed at runtime:

```csharp
@mapreduce('@filter(payload()["items"], "predicate")', '@map(...)', '@reduce(...)')
```

This causes two issues:

### Issue 1: Escaping Single Quotes is Impossible

When the nested expression contains single quotes (from bracket notation like `payload()['key']`), it must be escaped. But **Compo's parser doesn't support escape sequences**!

**Example from ExpressionParser.cs:80-81:**
```csharp
private static readonly Parser<char, Node> StringValue = AnyCharExcept('\'').ManyString()
    .Between(Quote).Select<Node>(x => new ValueNode<string>(x));
```

String literals can contain **any character EXCEPT single quotes** with **NO escape mechanism**.

### Issue 2: Parse Overhead

The nested expression gets parsed twice:
1. First parse: Outer expression with string literals
2. Runtime parse: Each [NestedExpression] string parameter gets parsed again

## Solution

Allow `[NestedExpression]` to accept already-parsed `Node` objects directly:

```csharp
@mapreduce(filter(payload().items, neq(item().userId, 'test')), map(...), reduce(...))
```

**Benefits:**
1. ✅ No escaping needed - avoids the single quote problem entirely
2. ✅ Parse once at profile upload, not every evaluation (performance)
3. ✅ Cleaner syntax - no nested string wrapping
4. ✅ Works with any Compo syntax - bracket notation, dot notation, everything

## Implementation

**Current behavior (ExpressionEvaluator.cs:140-165):**
```csharp
if (paramInfo?.GetCustomAttribute<NestedExpressionAttribute>() != null)
{
    // Only accepts ValueNode<string>
    if (argNode is ValueNode<string> stringNode)
    {
        var parser = (ExpressionParser)serviceProvider.GetService(typeof(ExpressionParser))!;
        var expressionText = stringNode.Value.TrimStart();
        if (!expressionText.StartsWith('@'))
        {
            expressionText = "@" + expressionText;
        }
        var parseResult = parser.BuildAst(expressionText);
        args[i] = parseResult.Value;
    }
    else
    {
        throw new InvalidOperationException($"[NestedExpression] parameter must receive a string value node, got {argNode.GetType().Name}");
    }
}
```

**Proposed fix:**
```csharp
if (paramInfo?.GetCustomAttribute<NestedExpressionAttribute>() != null)
{
    // Accept either string (parse it) OR direct Node (use as-is)
    if (argNode is ValueNode<string> stringNode)
    {
        // Existing behavior: parse string to Node
        var parser = (ExpressionParser)serviceProvider.GetService(typeof(ExpressionParser))!;
        var expressionText = stringNode.Value.TrimStart();
        if (!expressionText.StartsWith('@'))
        {
            expressionText = "@" + expressionText;
        }
        var parseResult = parser.BuildAst(expressionText);
        if (parseResult.Value == null)
        {
            throw new InvalidOperationException($"Failed to parse nested expression: {stringNode.Value}");
        }
        args[i] = parseResult.Value;
    }
    else if (argNode is FunctionNode || argNode is AccessNode || argNode is ValueNode<int> || argNode is ValueNode<bool> || argNode is ValueNode<decimal>)
    {
        // NEW: Accept already-parsed Node directly
        args[i] = argNode;
    }
    else
    {
        throw new InvalidOperationException($"[NestedExpression] parameter must receive a string value node or a Node, got {argNode.GetType().Name}");
    }
}
```

## Test Cases

Created comprehensive tests in `NestedExpressionAttributeTest.cs`:

1. **NestedExpression_WithSingleQuotesInString_ShouldFail** - Demonstrates the escaping problem
2. **NestedExpression_WithDirectFunctionNode_ShouldWork** - Tests direct Node passing
3. **Filter_WithStringPredicate_CurrentBehavior** - Current string-based approach works
4. **Filter_WithDirectPredicate_ProposedBehavior** - Proposed direct Node approach (currently fails)
5. **Filter_WithBracketNotationInPredicate_ShowsEscapingProblem** - Shows parse error with escaped quotes

## Migration Path

### Backward Compatibility

The fix is **100% backward compatible**:
- Existing profiles using string literals continue to work
- New profiles can use direct Node syntax
- Both syntaxes can coexist

### For FieldDefinitionConverter

In `MaaS.Common/Serialization/FieldDefinitionConverter.cs`, remove the `EscapeForExpression` quotes:

**Current:**
```csharp
private static string EscapeForExpression(string value)
{
    var escaped = value.Replace("'", "\\'");  // Doesn't work!
    return $"'{value}'";
}
```

**After fix:**
```csharp
private static string EscapeForExpression(string value)
{
    // No wrapping needed - expressions passed directly as Nodes
    return value;
}
```

Then profiles can use:
```json
{
  "@type": "mapreduce",
  "@items": "filter(payload().participants, neq(item().userId, parent().sender.userId))",
  "@map": "if(equals(item().userType, 'CUSTOMER'), customer(...), systemuser(...))",
  "@reduce": "activityParty(current())"
}
```

No quotes! The JSON deserializer reads the strings, FieldDefinitionConverter doesn't wrap them, and the parser handles them as direct function calls.

## Testing the Fix

1. Run the test suite:
   ```bash
   dotnet test src/Compo.Test/Compo.Test.csproj --filter "FullyQualifiedName~NestedExpression"
   ```

2. After implementing the fix, update test expectations:
   - `NestedExpression_WithDirectFunctionNode_ShouldWork` should pass
   - `Filter_WithDirectPredicate_ProposedBehavior` should pass

## Next Steps

1. ✅ Created test cases demonstrating the issue
2. ⏳ Review and approve the proposed fix
3. ⏳ Implement the fix in `ExpressionEvaluator.cs`
4. ⏳ Update `FieldDefinitionConverter.cs` to not wrap in quotes
5. ⏳ Update profiles to use direct syntax (remove single quotes from nested expressions)
6. ⏳ Verify all tests pass
