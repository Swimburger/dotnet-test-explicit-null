using System.Text.Json;

namespace dotnet_test_explicit_null;

[TestFixture]
public class OpenApiSerializationTests
{
    private static string Serialize<T>(T value)
    {
        return OpenApiSerializer.Serialize(value);
    }

    [Test]
    public void Case1_RequiredNotNullable_WithNull_ShouldOmit()
    {
        // Arrange: Required not-nullable with null (shouldn't happen, but can at runtime)
        var model = new OpenApiModelExample
        {
            Name = null!, // Invalid state
            Description = "valid"
        };

        // Act
        var json = Serialize(model);

        // Assert: name should be omitted due to JsonIgnore condition
        Assert.That(json, Does.Not.Contain("name"));
        Assert.That(json, Does.Contain("description"));
    }

    [Test]
    public void Case2_RequiredNullable_WithNull_ShouldWriteNull()
    {
        // Arrange: Required nullable with null
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = null
        };

        // Act
        var json = Serialize(model);

        // Assert: description should be written as null
        Assert.That(json, Does.Contain("\"name\": \"John\""));
        Assert.That(json, Does.Contain("\"description\": null"));
    }

    [Test]
    public void Case3_OptionalNullable_WithExplicitNull_ShouldWriteNull()
    {
        // Arrange: Optional nullable with explicit null
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalDescription = Optional<string?>.Of(null)
        };

        // Act
        var json = Serialize(model);

        // Assert: optionalDescription should be written as null
        Assert.That(json, Does.Contain("\"optionalDescription\": null"));
    }

    [Test]
    public void Case4_OptionalNullable_WithUndefined_ShouldOmit()
    {
        // Arrange: Optional nullable that is undefined
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalDescription = Optional<string?>.Undefined
        };

        // Act
        var json = Serialize(model);

        // Assert: optionalDescription should be omitted
        Assert.That(json, Does.Not.Contain("optionalDescription"));
    }

    [Test]
    public void Case5_Optional_WithUndefined_ShouldOmit()
    {
        // Arrange: Optional not-nullable that is undefined
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalName = Optional<string>.Undefined
        };

        // Act
        var json = Serialize(model);

        // Assert: optionalName should be omitted
        Assert.That(json, Does.Not.Contain("optionalName"));
    }

    [Test]
    public void Case6_Optional_WithNull_ShouldOmit()
    {
        // Arrange: Optional not-nullable with null (shouldn't happen, but can at runtime)
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalName = Optional<string>.Of(null!)
        };

        // Act
        var json = Serialize(model);

        // Assert: optionalName should be omitted due to JsonIgnore condition
        Assert.That(json, Does.Not.Contain("optionalName"));
    }

    [Test]
    public void Case7_AllPropertiesSet_ShouldWriteAll()
    {
        // Arrange: All properties with valid values
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalName = "Johnny",
            OptionalDescription = "Optional description",
            OptionalAge = 30,
            OptionalScore = 100
        };

        // Act
        var json = Serialize(model);

        // Assert: All properties should be present
        Assert.That(json, Does.Contain("\"name\": \"John\""));
        Assert.That(json, Does.Contain("\"description\": \"A user\""));
        Assert.That(json, Does.Contain("\"optionalName\": \"Johnny\""));
        Assert.That(json, Does.Contain("\"optionalDescription\": \"Optional description\""));
        Assert.That(json, Does.Contain("\"optionalAge\": 30"));
        Assert.That(json, Does.Contain("\"optionalScore\": 100"));
    }

    [Test]
    public void OptionalValueType_WithValue_ShouldWrite()
    {
        // Arrange
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalScore = 95
        };

        // Act
        var json = Serialize(model);

        // Assert
        Assert.That(json, Does.Contain("\"optionalScore\": 95"));
    }

    [Test]
    public void OptionalNullableValueType_WithNull_ShouldWriteNull()
    {
        // Arrange
        var model = new OpenApiModelExample
        {
            Name = "John",
            Description = "A user",
            OptionalAge = Optional<int?>.Of(null)
        };

        // Act
        var json = Serialize(model);

        // Assert
        Assert.That(json, Does.Contain("\"optionalAge\": null"));
    }

    [Test]
    public void Deserialization_WithNullValue_ShouldCreateDefinedOptional()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"description\":null,\"optionalDescription\":null}";

        // Act
        var model = OpenApiSerializer.Deserialize<OpenApiModelExample>(json);

        // Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("John"));
        Assert.That(model.Description, Is.Null);
        Assert.That(model.OptionalDescription.IsDefined, Is.True);
        Assert.That(model.OptionalDescription.Value, Is.Null);
    }

    [Test]
    public void Deserialization_WithMissingOptionalField_ShouldBeUndefined()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"description\":\"A user\"}";

        // Act
        var model = OpenApiSerializer.Deserialize<OpenApiModelExample>(json);

        // Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.OptionalDescription.IsUndefined, Is.True);
        Assert.That(model.OptionalName.IsUndefined, Is.True);
    }
}
