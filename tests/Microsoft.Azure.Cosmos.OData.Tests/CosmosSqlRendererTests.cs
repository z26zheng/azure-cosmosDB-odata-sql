using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Azure.Cosmos.OData.Ast;
using Microsoft.Azure.Cosmos.OData.Rendering;
using Xunit;

namespace Microsoft.Azure.Cosmos.OData.Tests
{
    /// <summary>
    /// Unit tests for <see cref="CosmosSqlRenderer"/> covering all SqlExpression node types.
    /// </summary>
    public class CosmosSqlRendererTests
    {
        // -------- Inline mode tests --------

        public class InlineModeTests
        {
            private readonly CosmosSqlRenderer _renderer = new(ParameterizationMode.Inline);

            private string Render(SqlExpression expr)
            {
                var parameters = new Dictionary<string, object?>();
                return _renderer.Render(expr, parameters);
            }

            [Fact]
            public void SqlNull_RendersNull()
            {
                Render(SqlNull.Instance).Should().Be("null");
            }

            [Fact]
            public void SqlLiteral_String_RendersQuoted()
            {
                Render(new SqlLiteral("hello")).Should().Be("'hello'");
            }

            [Fact]
            public void SqlLiteral_AlreadyQuotedString_PassesThrough()
            {
                Render(new SqlLiteral("'already quoted'")).Should().Be("'already quoted'");
            }

            [Fact]
            public void SqlLiteral_Integer_RendersInline()
            {
                Render(new SqlLiteral(42)).Should().Be("42");
            }

            [Fact]
            public void SqlLiteral_Boolean_RendersLowercase()
            {
                Render(new SqlLiteral(true)).Should().Be("true");
                Render(new SqlLiteral(false)).Should().Be("false");
            }

            [Fact]
            public void SqlLiteral_Double_RendersInvariant()
            {
                Render(new SqlLiteral(3.14)).Should().Be("3.14");
            }

            [Fact]
            public void SqlLiteral_Null_RendersNull()
            {
                Render(new SqlLiteral(null)).Should().Be("null");
            }

            [Fact]
            public void SqlMember_RendersPath()
            {
                Render(new SqlMember("c.name")).Should().Be("c.name");
            }

            [Fact]
            public void SqlRaw_RendersVerbatim()
            {
                Render(new SqlRaw("IS_DEFINED(c.x)")).Should().Be("IS_DEFINED(c.x)");
            }

            [Fact]
            public void SqlBinary_Equal_RendersCorrectly()
            {
                var expr = new SqlBinary(SqlBinaryOperator.Equal, new SqlMember("c.x"), new SqlLiteral(5));
                Render(expr).Should().Be("c.x = 5");
            }

            [Fact]
            public void SqlBinary_And_RendersCorrectly()
            {
                var left = new SqlBinary(SqlBinaryOperator.Equal, new SqlMember("c.x"), new SqlLiteral(1));
                var right = new SqlBinary(SqlBinaryOperator.GreaterThan, new SqlMember("c.y"), new SqlLiteral(2));
                var and = new SqlBinary(SqlBinaryOperator.And, left, right);
                Render(and).Should().Be("c.x = 1 AND c.y > 2");
            }

            [Fact]
            public void SqlBinary_Or_WithPrecedenceParens()
            {
                var a = new SqlBinary(SqlBinaryOperator.Equal, new SqlMember("c.x"), new SqlLiteral(1));
                var b = new SqlBinary(SqlBinaryOperator.Equal, new SqlMember("c.y"), new SqlLiteral(2));
                var or = new SqlBinary(SqlBinaryOperator.Or, a, b);
                var and = new SqlBinary(SqlBinaryOperator.And, or, new SqlMember("c.z"));

                // OR has lower precedence than AND, so OR should be parenthesized
                Render(and).Should().Contain("(c.x = 1 OR c.y = 2)");
            }

            [Theory]
            [InlineData(SqlBinaryOperator.Equal, "=")]
            [InlineData(SqlBinaryOperator.NotEqual, "!=")]
            [InlineData(SqlBinaryOperator.GreaterThan, ">")]
            [InlineData(SqlBinaryOperator.GreaterThanOrEqual, ">=")]
            [InlineData(SqlBinaryOperator.LessThan, "<")]
            [InlineData(SqlBinaryOperator.LessThanOrEqual, "<=")]
            [InlineData(SqlBinaryOperator.And, "AND")]
            [InlineData(SqlBinaryOperator.Or, "OR")]
            [InlineData(SqlBinaryOperator.Add, "+")]
            [InlineData(SqlBinaryOperator.Subtract, "-")]
            [InlineData(SqlBinaryOperator.Multiply, "*")]
            [InlineData(SqlBinaryOperator.Divide, "/")]
            [InlineData(SqlBinaryOperator.Modulo, "%")]
            public void SqlBinary_AllOperators_RenderCorrectSymbol(SqlBinaryOperator op, string expectedSymbol)
            {
                var expr = new SqlBinary(op, new SqlMember("c.x"), new SqlMember("c.y"));
                Render(expr).Should().Contain(expectedSymbol);
            }

            [Fact]
            public void SqlUnary_Not_RendersCorrectly()
            {
                var expr = new SqlUnary(SqlUnaryOperator.Not, new SqlMember("c.active"));
                Render(expr).Should().Be("NOT(c.active)");
            }

            [Fact]
            public void SqlUnary_Negate_RendersCorrectly()
            {
                var expr = new SqlUnary(SqlUnaryOperator.Negate, new SqlMember("c.value"));
                Render(expr).Should().Be("-c.value");
            }

            [Fact]
            public void SqlFunctionCall_RendersCorrectly()
            {
                var expr = new SqlFunctionCall("CONTAINS", new SqlExpression[]
                {
                    new SqlMember("c.name"),
                    new SqlLiteral("test"),
                });
                Render(expr).Should().Be("CONTAINS(c.name,'test')");
            }

            [Fact]
            public void SqlFunctionCall_NestedFunctions()
            {
                var inner = new SqlFunctionCall("RTRIM", new SqlExpression[] { new SqlMember("c.name") });
                var outer = new SqlFunctionCall("LTRIM", new SqlExpression[] { inner });
                Render(outer).Should().Be("LTRIM(RTRIM(c.name))");
            }

            [Fact]
            public void SqlExists_RendersSubquery()
            {
                var expr = new SqlExists("t", new SqlMember("c.tags"),
                    new SqlBinary(SqlBinaryOperator.Equal, new SqlMember("t"), new SqlLiteral("foo")));
                var result = Render(expr);
                result.Should().Contain("EXISTS(SELECT VALUE t FROM t IN c.tags WHERE t = 'foo')");
            }

            [Fact]
            public void SqlExists_WithoutPredicate_RendersWithoutWhere()
            {
                var expr = new SqlExists("t", new SqlMember("c.tags"), null);
                var result = Render(expr);
                result.Should().Be("EXISTS(SELECT VALUE t FROM t IN c.tags)");
            }
        }

        // -------- Parameterized mode tests --------

        public class ParameterizedModeTests
        {
            private readonly CosmosSqlRenderer _renderer = new(ParameterizationMode.Parameters);

            [Fact]
            public void SqlLiteral_BecomesParameter()
            {
                var parameters = new Dictionary<string, object?>();
                var result = _renderer.Render(new SqlLiteral("test"), parameters);
                result.Should().Be("@p0");
                parameters.Should().ContainKey("@p0");
                parameters["@p0"].Should().Be("test");
            }

            [Fact]
            public void MultipleLiterals_GetIncrementingParameters()
            {
                var parameters = new Dictionary<string, object?>();
                var expr = new SqlBinary(SqlBinaryOperator.And,
                    new SqlBinary(SqlBinaryOperator.Equal, new SqlMember("c.x"), new SqlLiteral("a")),
                    new SqlBinary(SqlBinaryOperator.Equal, new SqlMember("c.y"), new SqlLiteral("b")));
                var result = _renderer.Render(expr, parameters);

                result.Should().Contain("@p0").And.Contain("@p1");
                parameters.Should().HaveCount(2);
                parameters["@p0"].Should().Be("a");
                parameters["@p1"].Should().Be("b");
            }

            [Fact]
            public void SqlNull_RendersNullNotParameter()
            {
                var parameters = new Dictionary<string, object?>();
                var result = _renderer.Render(SqlNull.Instance, parameters);
                result.Should().Be("null");
                parameters.Should().BeEmpty();
            }

            [Fact]
            public void SqlMember_RendersDirectly()
            {
                var parameters = new Dictionary<string, object?>();
                var result = _renderer.Render(new SqlMember("c.name"), parameters);
                result.Should().Be("c.name");
                parameters.Should().BeEmpty();
            }
        }
    }
}
