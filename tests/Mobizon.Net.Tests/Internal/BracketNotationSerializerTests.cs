using System.Collections.Generic;
using Mobizon.Net.Internal;
using Xunit;

namespace Mobizon.Net.Tests.Internal
{
    public class BracketNotationSerializerTests
    {
        [Fact]
        public void Serialize_FlatProperties_ReturnsKeyValuePairs()
        {
            var obj = new Dictionary<string, object?>
            {
                ["recipient"] = "77001234567",
                ["text"] = "Hello"
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("77001234567", result["recipient"]);
            Assert.Equal("Hello", result["text"]);
        }

        [Fact]
        public void Serialize_NestedObject_UsesBracketNotation()
        {
            var obj = new Dictionary<string, object?>
            {
                ["data"] = new Dictionary<string, object?>
                {
                    ["type"] = 1,
                    ["from"] = "Alpha",
                    ["text"] = "Hello"
                }
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("1", result["data[type]"]);
            Assert.Equal("Alpha", result["data[from]"]);
            Assert.Equal("Hello", result["data[text]"]);
        }

        [Fact]
        public void Serialize_Array_UsesIndexedBrackets()
        {
            var obj = new Dictionary<string, object?>
            {
                ["ids"] = new object[] { 1, 2, 3 }
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("1", result["ids[0]"]);
            Assert.Equal("2", result["ids[1]"]);
            Assert.Equal("3", result["ids[2]"]);
        }

        [Fact]
        public void Serialize_NestedWithPrefix_ChainsCorrectly()
        {
            var obj = new Dictionary<string, object?>
            {
                ["criteria"] = new Dictionary<string, object?>
                {
                    ["from"] = "Alpha",
                    ["status"] = 1
                },
                ["pagination"] = new Dictionary<string, object?>
                {
                    ["currentPage"] = 2,
                    ["pageSize"] = 10
                }
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("Alpha", result["criteria[from]"]);
            Assert.Equal("1", result["criteria[status]"]);
            Assert.Equal("2", result["pagination[currentPage]"]);
            Assert.Equal("10", result["pagination[pageSize]"]);
        }

        [Fact]
        public void Serialize_NullValues_AreSkipped()
        {
            var obj = new Dictionary<string, object?>
            {
                ["key1"] = "value",
                ["key2"] = null
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Single(result);
            Assert.Equal("value", result["key1"]);
        }

        [Fact]
        public void Serialize_EmptyString_IsPreserved()
        {
            var obj = new Dictionary<string, object?>
            {
                ["key"] = ""
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("", result["key"]);
        }

        [Fact]
        public void Serialize_EmptyDictionary_ReturnsEmpty()
        {
            var obj = new Dictionary<string, object?>();
            var result = BracketNotationSerializer.Serialize(obj);
            Assert.Empty(result);
        }

        [Fact]
        public void Serialize_DeepNesting_ChainsCorrectly()
        {
            var obj = new Dictionary<string, object?>
            {
                ["params"] = new Dictionary<string, object?>
                {
                    ["validity"] = 60
                }
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("60", result["params[validity]"]);
        }

        [Fact]
        public void Serialize_StringArray_UsesIndexedBrackets()
        {
            var obj = new Dictionary<string, object?>
            {
                ["data"] = new object[] { "77001111111", "77002222222" }
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("77001111111", result["data[0]"]);
            Assert.Equal("77002222222", result["data[1]"]);
        }

        [Fact]
        public void Serialize_MixedTypes_HandlesCorrectly()
        {
            var obj = new Dictionary<string, object?>
            {
                ["id"] = 42,
                ["name"] = "test",
                ["active"] = true
            };

            var result = BracketNotationSerializer.Serialize(obj);

            Assert.Equal("42", result["id"]);
            Assert.Equal("test", result["name"]);
            Assert.Equal("True", result["active"]);
        }
    }
}
