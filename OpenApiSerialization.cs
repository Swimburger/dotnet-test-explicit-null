using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotnet_test_explicit_null;

/// <summary>
/// Demonstrates the proper way to model OpenAPI properties in C# with correct serialization semantics.
/// </summary>
public class OpenApiModelExample
{
    // ====================
    // REQUIRED PROPERTIES
    // ====================

    /// <summary>
    /// Required, not nullable property.
    /// In valid code, this should never be null.
    /// OpenAPI: required: true, nullable: false
    /// Serialization: Always included. If null at runtime (invalid), omitted by default.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Required, nullable property.
    /// Can legitimately be null.
    /// OpenAPI: required: true, nullable: true
    /// Serialization: Always included, even when null.
    /// </summary>
    [Nullable]
    public string? Description { get; set; }

    // ====================
    // OPTIONAL PROPERTIES
    // ====================

    /// <summary>
    /// Optional, not nullable property.
    /// Use Optional<T> where T is non-nullable.
    /// OpenAPI: required: false, nullable: false
    /// Serialization:
    ///   - Undefined -> omitted from JSON
    ///   - Defined with value -> included in JSON
    ///   - Defined with null (invalid) -> omitted from JSON
    /// </summary>
    [Optional]
    public Optional<string> OptionalName { get; set; } = Optional<string>.Undefined;

    /// <summary>
    /// Optional, nullable property.
    /// Use Optional<T?> where T? is nullable.
    /// OpenAPI: required: false, nullable: true
    /// Serialization:
    ///   - Undefined -> omitted from JSON
    ///   - Defined with null -> included as null in JSON
    ///   - Defined with value -> included in JSON
    /// </summary>
    [Optional, Nullable]
    public Optional<string?> OptionalDescription { get; set; } = Optional<string?>.Undefined;

    /// <summary>
    /// Optional, nullable value type.
    /// OpenAPI: required: false, nullable: true
    /// </summary>
    [Optional, Nullable]
    public Optional<int?> OptionalAge { get; set; } = Optional<int?>.Undefined;

    /// <summary>
    /// Optional, not nullable value type.
    /// OpenAPI: required: false, nullable: false
    /// </summary>
    [Optional]
    public Optional<int> OptionalScore { get; set; } = Optional<int>.Undefined;
}

/// <summary>
/// Helper for OpenAPI-compliant JSON serialization
/// </summary>
public static class OpenApiSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new OptionalJsonConverterFactory() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            // Default: omit null values (unless marked with [Nullable])
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
            {
                Modifiers = { NullableOptionalModifier },
            },
        };
        return options;
    }

    private static void NullableOptionalModifier(System.Text.Json.Serialization.Metadata.JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != System.Text.Json.Serialization.Metadata.JsonTypeInfoKind.Object)
            return;

        var allProperties = typeInfo.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in typeInfo.Properties)
        {
            var isOptionalType = property.PropertyType.IsGenericType &&
                                 property.PropertyType.GetGenericTypeDefinition() == typeof(Optional<>);

            var propertyInfo = property.AttributeProvider as PropertyInfo;

            if (propertyInfo == null)
                continue;

            var hasNullableAttribute = propertyInfo.GetCustomAttribute<NullableAttribute>() != null;

            if (isOptionalType)
            {
                var originalGetter = property.Get;
                if (originalGetter != null)
                {
                    var capturedIsNullable = hasNullableAttribute;

                    property.ShouldSerialize = (obj, value) =>
                    {
                        var optionalValue = originalGetter(obj);
                        if (optionalValue is not IOptional optional)
                            return false;

                        if (!optional.IsDefined)
                            return false;

                        if (!capturedIsNullable)
                        {
                            var innerValue = optional.GetBoxedValue();
                            if (innerValue == null)
                                return false;
                        }

                        return true;
                    };
                }
            }
            else if (hasNullableAttribute)
            {
                property.ShouldSerialize = (obj, value) => true;
            }
        }
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, DefaultOptions);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Demonstrates all the OpenAPI serialization cases
    /// </summary>
    public static void RunDemo()
    {
        Console.WriteLine("OpenAPI Serialization Patterns\n");
        Console.WriteLine("=".PadRight(70, '=') + "\n");

        // Case 1: Not nullable + null (runtime edge case)
        Console.WriteLine("Case 1: Required not-nullable with null (shouldn't happen)");
        Console.WriteLine("  → Omitted from JSON via JsonIgnore condition\n");
        var case1 = new OpenApiModelExample
        {
            Name = null!, // Invalid state
            Description = "valid"
        };
        Console.WriteLine(Serialize(case1));
        Console.WriteLine();

        // Case 2: Nullable + null
        Console.WriteLine("Case 2: Required nullable with null");
        Console.WriteLine("  → Written as null in JSON\n");
        var case2 = new OpenApiModelExample
        {
            Name = "John",
            Description = null
        };
        Console.WriteLine(Serialize(case2));
        Console.WriteLine();

        // Case 3: Optional nullable + null
        Console.WriteLine("Case 3: Optional nullable with explicit null");
        Console.WriteLine("  → Written as null in JSON\n");
        var case3 = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalDescription = Optional<string?>.Of(null)
        };
        Console.WriteLine(Serialize(case3));
        Console.WriteLine();

        // Case 4: Optional nullable + undefined
        Console.WriteLine("Case 4: Optional nullable that is undefined");
        Console.WriteLine("  → Omitted from JSON\n");
        var case4 = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalDescription = Optional<string?>.Undefined
        };
        Console.WriteLine(Serialize(case4));
        Console.WriteLine();

        // Case 5: Optional + undefined
        Console.WriteLine("Case 5: Optional not-nullable that is undefined");
        Console.WriteLine("  → Omitted from JSON\n");
        var case5 = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalName = Optional<string>.Undefined
        };
        Console.WriteLine(Serialize(case5));
        Console.WriteLine();

        // Case 6: Optional + null (runtime edge case)
        Console.WriteLine("Case 6: Optional not-nullable with null (shouldn't happen)");
        Console.WriteLine("  → Omitted from JSON via JsonIgnore condition\n");
        var case6 = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalName = Optional<string>.Of(null!)
        };
        Console.WriteLine(Serialize(case6));
        Console.WriteLine();

        // Case 7: All properties set
        Console.WriteLine("Case 7: All properties with valid values");
        Console.WriteLine("  → All included in JSON\n");
        var case7 = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalName = "Johnny",
            OptionalDescription = "Optional description",
            OptionalAge = 30,
            OptionalScore = 100
        };
        Console.WriteLine(Serialize(case7));
        Console.WriteLine();

        Console.WriteLine("=".PadRight(70, '='));
    }
}
