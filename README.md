# Optional<T> for C# HTTP SDKs

A simple, type-safe way to differentiate between **undefined** and **null** values in C# HTTP SDKs.

## The Problem

When building HTTP SDKs, you need to distinguish between:
- **Undefined**: Don't send this field (PATCH - don't update)
- **Null**: Send `null` (PATCH - clear the field)
- **Value**: Send the actual value (PATCH - update the field)

Standard C# nullable types can't represent all three states.

## The Solution

`Optional<T>` provides two states:
1. **Undefined** - field not set
2. **Defined** - field is set (use nullable types for null values)

```csharp
// Use Optional<T?> for nullable fields
public class UpdateUserRequest
{
    public Optional<string?> Name { get; set; } = Optional<string?>.Undefined;
    public Optional<string?> Email { get; set; } = Optional<string?>.Undefined;
    public Optional<int?> Age { get; set; } = Optional<int?>.Undefined;
}
```

## Usage

### Creating Values

```csharp
// Undefined - not set
var undefined = Optional<string?>.Undefined;

// Defined with value
var value = Optional<string?>.Of("John");

// Defined as null
var null_ = Optional<string?>.Of(null);

// Implicit conversion
Optional<string?> name = "John";  // Of("John")
Optional<string?> email = null;   // Of(null)
```

### Checking State

```csharp
var opt = Optional<string?>.Of("test");

opt.IsUndefined  // false
opt.IsDefined    // true

// Check for null using C# patterns
if (opt.IsDefined && opt.Value is null)
{
    Console.WriteLine("Explicitly set to null");
}
```

### Getting Values

```csharp
// Direct access (throws if undefined)
string? value = opt.Value;

// Try get value
if (opt.TryGetValue(out var val))
{
    Console.WriteLine(val);
}

// Get value or default
string? result = opt.GetValueOrDefault("default");
// Returns: value if defined, default if undefined
```

## Real-World Example

```csharp
public class UpdateUserRequest
{
    public Optional<string?> Name { get; set; } = Optional<string?>.Undefined;
    public Optional<string?> Email { get; set; } = Optional<string?>.Undefined;
    public Optional<int?> Age { get; set; } = Optional<int?>.Undefined;

    // Regular nullable properties - null values omitted from JSON
    public string? Description { get; set; }
    public int? Score { get; set; }

    public string ToJson()
    {
        var dict = new Dictionary<string, object?>();

        // Add Optional properties only if defined (even if null)
        Name.AddTo(dict, "name");
        Email.AddTo(dict, "email");
        Age.AddTo(dict, "age");

        // Add regular nullable properties only if not null
        Description.AddIfNotNull(dict, "description");
        Score.AddIfNotNull(dict, "score");

        return JsonSerializer.Serialize(dict, options);
    }
}

// Usage
var request = new UpdateUserRequest
{
    Name = "Alice",                       // Update name
    Email = Optional<string?>.Of(null),   // Clear email (explicitly set to null)
    Age = Optional<int?>.Undefined,       // Don't touch age (undefined)
    Description = null,                   // Regular nullable = null (omitted)
    Score = 100                           // Regular nullable with value (included)
};

var json = request.ToJson();
// Result: {"name":"Alice","email":null,"score":100}
// Note: "age" and "description" are NOT included
```

## Key Behavior: Nullable vs Optional

This is the critical distinction:

| Property Type | Value | Included in JSON? |
|--------------|-------|------------------|
| `string? Description` | `null` | ❌ No - omitted |
| `string? Description` | `"text"` | ✅ Yes - as value |
| `Optional<string?> Email` | `Undefined` | ❌ No - omitted |
| `Optional<string?> Email` | `Of(null)` | ✅ Yes - as `null` |
| `Optional<string?> Email` | `Of("text")` | ✅ Yes - as value |

**The magic:** When you wrap a nullable type in `Optional<T?>`, you make the `null` value **explicit and meaningful**. A `null` inside `Optional` means "clear this field," while a `null` on a regular nullable property means "nothing to see here, skip it."

## API Reference

### Properties

- `IsDefined` - true if the value is set (even if null)
- `IsUndefined` - true if the value is not set
- `Value` - gets the value (throws if undefined)

### Methods

- `static Optional<T> Undefined` - creates undefined value
- `static Optional<T> Of(T value)` - creates defined value
- `T GetValueOrDefault(T defaultValue)` - gets value or returns default
- `bool TryGetValue(out T value)` - tries to get the value

### Extension Methods

#### For Optional<T>:
- `AddTo(Dictionary<string, object?> dict, string key)` - adds value to dictionary if defined (even if null)
- `IfDefined(Action<T> action)` - executes action if defined
- `Map<TResult>(Func<T, TResult> mapper)` - transforms the value if defined

#### For nullable types:
- `AddIfNotNull(Dictionary<string, object?> dict, string key)` - adds value to dictionary only if not null

## Design Philosophy

`Optional<T>` is **only** about defined vs undefined. For null handling, use C#'s native nullable types:

- `Optional<T>` = Is the field being sent?
- `T?` = Can the value be null?

This separation of concerns keeps the API simple and aligns with C# idioms.

### Why not `Optional<T>` with `IsNull`?

We considered adding `IsNull` property and `Null` factory, but decided against it:
- ❌ Duplicates C#'s nullable type system
- ❌ Larger API surface to maintain
- ❌ Mixes two concerns (defined/undefined + null/value)

The slight verbosity of `Optional<string?>` is worth the conceptual clarity.

## Examples

### PATCH Request - Update Specific Fields

```csharp
// User wants to update only email
var request = new UpdateUserRequest
{
    Email = "newemail@example.com"
    // Other fields undefined - won't be updated
};
```

### Clear a Field

```csharp
// User wants to clear their middle name
var request = new UpdateUserRequest
{
    Name = Optional<string?>.Of(null)
};
```

### Mixed Update

```csharp
var request = new UpdateUserRequest
{
    Name = "Alice",                         // Update
    Email = null,                            // Clear (implicit Of(null))
    Age = Optional<int?>.Undefined           // Don't touch
};
```

## Installation

Copy `ExplicitNull.cs` into your project. No external dependencies.

## Testing

Run the included tests:

```bash
dotnet test
```

All tests should pass ✅

## Files

- **Optional.cs** - Core `Optional<T>` implementation
- **HttpRequestExample.cs** - Example HTTP request class with JSON serialization
- **UnitTest1.cs** - Comprehensive test suite
- **README.md** - This file

## Design Decisions

Key decisions:
- ✅ No null semantics in Optional (use `T?` instead)
- ✅ Only two states: Defined/Undefined
- ✅ Leverage C#'s type system for null handling
- ✅ Smaller API surface, simpler mental model
