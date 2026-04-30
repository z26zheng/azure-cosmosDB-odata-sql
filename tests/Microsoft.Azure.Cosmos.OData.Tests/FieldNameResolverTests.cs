using System;
using FluentAssertions;
using Microsoft.Azure.Cosmos.OData.Naming;
using Xunit;

namespace Microsoft.Azure.Cosmos.OData.Tests
{
    /// <summary>
    /// Tests for <see cref="DefaultFieldNameResolver"/>.
    /// </summary>
    public class FieldNameResolverTests
    {
        [Fact]
        public void TranslateFieldName_PrependsAlias()
        {
            var resolver = new DefaultFieldNameResolver("c");
            resolver.TranslateFieldName("name").Should().Be("c.name");
        }

        [Fact]
        public void TranslateFieldName_TrimsWhitespace()
        {
            var resolver = new DefaultFieldNameResolver("c");
            resolver.TranslateFieldName("  name  ").Should().Be("c.name");
        }

        [Fact]
        public void TranslateFieldName_CustomAlias()
        {
            var resolver = new DefaultFieldNameResolver("doc");
            resolver.TranslateFieldName("name").Should().Be("doc.name");
        }

        [Fact]
        public void TranslateSource_CombinesParentChild()
        {
            var resolver = new DefaultFieldNameResolver("c");
            resolver.TranslateSource("c.address", "city").Should().Be("c.address.city");
        }

        [Fact]
        public void TranslateSource_PrependsAliasIfMissing()
        {
            var resolver = new DefaultFieldNameResolver("c");
            resolver.TranslateSource("address", "city").Should().Be("c.address.city");
        }

        [Fact]
        public void TranslateSource_DoesNotDoublePrepend()
        {
            var resolver = new DefaultFieldNameResolver("c");
            resolver.TranslateSource("c.address", "city").Should().Be("c.address.city");
            resolver.TranslateSource("c.address", "city").Should().NotBe("c.c.address.city");
        }

        [Fact]
        public void TranslateEnumValue_StripsNamespace()
        {
            var resolver = new DefaultFieldNameResolver("c");
            resolver.TranslateEnumValue("MyNamespace.MyEnum'VALUE'", "MyNamespace.MyEnum")
                .Should().Be("'VALUE'");
        }

        [Fact]
        public void TranslateEnumValue_NoNamespace_PassesThrough()
        {
            var resolver = new DefaultFieldNameResolver("c");
            resolver.TranslateEnumValue("'VALUE'", "").Should().Be("'VALUE'");
        }

        [Fact]
        public void Constructor_EmptyAlias_Throws()
        {
            var act = () => new DefaultFieldNameResolver("");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullAlias_Throws()
        {
            var act = () => new DefaultFieldNameResolver(null!);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TranslateFieldName_NullField_Throws()
        {
            var resolver = new DefaultFieldNameResolver("c");
            var act = () => resolver.TranslateFieldName(null!);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
