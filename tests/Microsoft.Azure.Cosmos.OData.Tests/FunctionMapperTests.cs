using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Azure.Cosmos.OData.Ast;
using Microsoft.Azure.Cosmos.OData.Functions;
using Xunit;

namespace Microsoft.Azure.Cosmos.OData.Tests
{
    /// <summary>
    /// Unit tests for all ISqlFunctionMapper implementations.
    /// </summary>
    public class FunctionMapperTests
    {
        // -------- DefaultFunctionMapper --------

        public class DefaultFunctionMapperTests
        {
            private readonly DefaultFunctionMapper _mapper = new();

            [Theory]
            [InlineData("contains", "CONTAINS")]
            [InlineData("startswith", "STARTSWITH")]
            [InlineData("endswith", "ENDSWITH")]
            [InlineData("length", "LENGTH")]
            [InlineData("indexof", "INDEX_OF")]
            [InlineData("substring", "SUBSTRING")]
            [InlineData("tolower", "LOWER")]
            [InlineData("toupper", "UPPER")]
            [InlineData("concat", "CONCAT")]
            [InlineData("matchespattern", "RegexMatch")]
            public void StringFunctions_MapCorrectly(string odataName, string expectedSqlName)
            {
                _mapper.CanMap(odataName).Should().BeTrue();
                var result = _mapper.Map(odataName, new SqlExpression[] { new SqlMember("c.field") });
                result.Should().BeOfType<SqlFunctionCall>();
                ((SqlFunctionCall)result).Name.Should().Be(expectedSqlName);
            }

            [Fact]
            public void Trim_MapsToLtrimRtrim()
            {
                _mapper.CanMap("trim").Should().BeTrue();
                var result = _mapper.Map("trim", new SqlExpression[] { new SqlMember("c.field") });
                result.Should().BeOfType<SqlFunctionCall>();
                var outer = (SqlFunctionCall)result;
                outer.Name.Should().Be("LTRIM");
                outer.Arguments.Should().HaveCount(1);
                outer.Arguments[0].Should().BeOfType<SqlFunctionCall>();
                ((SqlFunctionCall)outer.Arguments[0]).Name.Should().Be("RTRIM");
            }

            [Theory]
            [InlineData("round", "ROUND")]
            [InlineData("floor", "FLOOR")]
            [InlineData("ceiling", "CEILING")]
            public void MathFunctions_MapCorrectly(string odataName, string expectedSqlName)
            {
                _mapper.CanMap(odataName).Should().BeTrue();
                var result = _mapper.Map(odataName, new SqlExpression[] { new SqlMember("c.field") });
                ((SqlFunctionCall)result).Name.Should().Be(expectedSqlName);
            }

            [Theory]
            [InlineData("year")]
            [InlineData("month")]
            [InlineData("day")]
            [InlineData("hour")]
            [InlineData("minute")]
            [InlineData("second")]
            public void DateFunctions_MapToDateTimePart(string odataName)
            {
                _mapper.CanMap(odataName).Should().BeTrue();
                var result = _mapper.Map(odataName, new SqlExpression[] { new SqlMember("c.date") });
                var func = result.Should().BeOfType<SqlFunctionCall>().Subject;
                func.Name.Should().Be("DateTimePart");
                func.Arguments.Should().HaveCount(2);
                func.Arguments[0].Should().BeOfType<SqlLiteral>();
            }

            [Theory]
            [InlineData("isdefined", "IS_DEFINED")]
            [InlineData("arraycontains", "ARRAY_CONTAINS")]
            public void CosmosExtensions_MapCorrectly(string odataName, string expectedSqlName)
            {
                _mapper.CanMap(odataName).Should().BeTrue();
                var result = _mapper.Map(odataName, new SqlExpression[] { new SqlMember("c.field") });
                ((SqlFunctionCall)result).Name.Should().Be(expectedSqlName);
            }

            [Fact]
            public void UnknownFunction_CanMap_ReturnsFalse()
            {
                _mapper.CanMap("unknownfunc").Should().BeFalse();
            }

            [Fact]
            public void UnknownFunction_Map_Throws()
            {
                var act = () => _mapper.Map("unknownfunc", new SqlExpression[] { new SqlMember("c.x") });
                act.Should().Throw<UnsupportedODataFeatureException>();
            }

            [Fact]
            public void NullFunctionName_CanMap_ReturnsFalse()
            {
                _mapper.CanMap(null!).Should().BeFalse();
            }
        }

        // -------- GeospatialFunctionMapper --------

        public class GeospatialFunctionMapperTests
        {
            private readonly GeospatialFunctionMapper _mapper = new();

            [Theory]
            [InlineData("geo.distance", "ST_DISTANCE")]
            [InlineData("geo.intersects", "ST_INTERSECTS")]
            [InlineData("geo.length", "ST_LENGTH")]
            public void GeospatialFunctions_MapCorrectly(string odataName, string expectedSqlName)
            {
                _mapper.CanMap(odataName).Should().BeTrue();
                var result = _mapper.Map(odataName, new SqlExpression[] { new SqlMember("c.loc") });
                ((SqlFunctionCall)result).Name.Should().Be(expectedSqlName);
            }

            [Fact]
            public void NonGeoFunction_CanMap_ReturnsFalse()
            {
                _mapper.CanMap("contains").Should().BeFalse();
            }
        }

        // -------- VectorSearchFunctionMapper --------

        public class VectorSearchFunctionMapperTests
        {
            private readonly VectorSearchFunctionMapper _mapper = new();

            [Fact]
            public void VectorDistance_MapsCorrectly()
            {
                _mapper.CanMap("vectordistance").Should().BeTrue();
                var result = _mapper.Map("vectordistance", new SqlExpression[]
                {
                    new SqlMember("c.embedding"),
                    new SqlLiteral("[1,2,3]"),
                });
                ((SqlFunctionCall)result).Name.Should().Be("VectorDistance");
            }

            [Fact]
            public void NonVectorFunction_CanMap_ReturnsFalse()
            {
                _mapper.CanMap("contains").Should().BeFalse();
            }
        }

        // -------- FullTextSearchFunctionMapper --------

        public class FullTextSearchFunctionMapperTests
        {
            private readonly FullTextSearchFunctionMapper _mapper = new();

            [Theory]
            [InlineData("fulltextcontains", "FullTextContains")]
            [InlineData("fulltextcontainsall", "FullTextContainsAll")]
            [InlineData("fulltextcontainsany", "FullTextContainsAny")]
            [InlineData("fulltextscore", "FullTextScore")]
            public void FullTextFunctions_MapCorrectly(string odataName, string expectedSqlName)
            {
                _mapper.CanMap(odataName).Should().BeTrue();
                var result = _mapper.Map(odataName, new SqlExpression[] { new SqlMember("c.text") });
                ((SqlFunctionCall)result).Name.Should().Be(expectedSqlName);
            }
        }

        // -------- CompositeFunctionMapper --------

        public class CompositeFunctionMapperTests
        {
            [Fact]
            public void Composite_DelegatesToFirstMatchingMapper()
            {
                var composite = new CompositeFunctionMapper(
                    new DefaultFunctionMapper(),
                    new GeospatialFunctionMapper());

                composite.CanMap("contains").Should().BeTrue();
                composite.CanMap("geo.distance").Should().BeTrue();
                composite.CanMap("unknownfunc").Should().BeFalse();
            }

            [Fact]
            public void Composite_FirstMapperWins()
            {
                var composite = new CompositeFunctionMapper(
                    new DefaultFunctionMapper(),
                    new GeospatialFunctionMapper());

                var result = composite.Map("contains", new SqlExpression[] { new SqlMember("c.x"), new SqlLiteral("y") });
                ((SqlFunctionCall)result).Name.Should().Be("CONTAINS");
            }

            [Fact]
            public void Composite_UnknownFunction_Throws()
            {
                var composite = new CompositeFunctionMapper(new DefaultFunctionMapper());
                var act = () => composite.Map("nonexistent", new SqlExpression[] { new SqlMember("c.x") });
                act.Should().Throw<UnsupportedODataFeatureException>();
            }

            [Fact]
            public void Composite_EmptyMappers_CanMapReturnsFalse()
            {
                var composite = new CompositeFunctionMapper();
                composite.CanMap("contains").Should().BeFalse();
            }
        }
    }
}
