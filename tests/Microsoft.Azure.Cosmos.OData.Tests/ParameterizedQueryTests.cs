using FluentAssertions;
using Microsoft.Azure.Cosmos.OData.Tests.Helpers;
using Xunit;

namespace Microsoft.Azure.Cosmos.OData.Tests
{
    /// <summary>
    /// Tests for <see cref="ParameterizationMode.Parameters"/> (the default mode).
    /// Every literal value should be substituted with @p0, @p1, ... and the actual
    /// values should appear in <see cref="TranslatedQuery.Parameters"/>.
    /// </summary>
    public class ParameterizedQueryTests
    {
        private static readonly ODataToCosmosSqlTranslator Translator = new();

        private static readonly TranslationOptions Parameterized = new TranslationOptions
        {
            Parameterization = ParameterizationMode.Parameters,
        };

        private static readonly TranslationOptions ParamFilterOnly = new TranslationOptions
        {
            Parameterization = ParameterizationMode.Parameters,
            Clauses = TranslationClauses.Filter,
        };

        [Fact]
        public void Filter_StringEqual_SubstitutesParameter()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=EnglishName eq 'Microsoft'");
            var result = Translator.Translate(clauses, ParamFilterOnly);

            result.Sql.Should().Contain("@p0");
            result.Sql.Should().NotContain("'Microsoft'");
            result.Parameters.Should().ContainKey("@p0");
            result.Parameters["@p0"].Should().Be("Microsoft");
        }

        [Fact]
        public void Filter_IntegerComparison_SubstitutesParameter()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=IntField gt 5");
            var result = Translator.Translate(clauses, ParamFilterOnly);

            result.Sql.Should().Contain("@p0");
            result.Parameters.Should().ContainKey("@p0");
            result.Parameters["@p0"].Should().Be(5);
        }

        [Fact]
        public void Filter_MultipleValues_CreatesMultipleParameters()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=EnglishName eq 'Microsoft' and IntField le 5");
            var result = Translator.Translate(clauses, ParamFilterOnly);

            result.Sql.Should().Contain("@p0");
            result.Sql.Should().Contain("@p1");
            result.Parameters.Should().HaveCount(2);
        }

        [Fact]
        public void Filter_ContainsFunction_ParameterizesArguments()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=contains(EnglishName,'Limited')");
            var result = Translator.Translate(clauses, ParamFilterOnly);

            result.Sql.Should().Contain("@p0");
            result.Parameters.Should().ContainKey("@p0");
            result.Parameters["@p0"].Should().Be("Limited");
        }

        [Fact]
        public void FullQuery_Parameterized_ContainsNoInlinedLiterals()
        {
            var clauses = ODataTestHelper.FromQuery(
                "$select=EnglishName&$filter=IntField gt 5&$orderby=IntField desc&$top=10&$skip=0");
            var result = Translator.Translate(clauses, Parameterized);

            result.Sql.Should().StartWith("SELECT c.EnglishName FROM c");
            result.Sql.Should().Contain("@p0");
            result.Sql.Should().NotContain(" 5 ");
            result.Parameters.Should().NotBeEmpty();
        }

        [Fact]
        public void SelectAll_NoParameters()
        {
            var clauses = ODataTestHelper.FromQuery("");
            var result = Translator.Translate(clauses, Parameterized);

            result.Sql.Should().Be("SELECT * FROM c");
            result.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void AdditionalWhereClause_WithParameters()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=IntField gt 5");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Parameters,
                Clauses = TranslationClauses.Filter,
                AdditionalWhereClause = "c.type = @type",
                AdditionalParameters = new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["@type"] = "company",
                },
            };
            var result = Translator.Translate(clauses, opts);

            result.Sql.Should().Contain("c.type = @type");
            result.Parameters.Should().ContainKey("@type");
            result.Parameters["@type"].Should().Be("company");
        }

        [Fact]
        public void CountSql_IsGenerated_WhenCountTrue()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=IntField gt 5&$count=true");
            var result = Translator.Translate(clauses, Parameterized);

            result.CountSql.Should().NotBeNullOrEmpty();
            result.CountSql.Should().StartWith("SELECT VALUE COUNT(1) FROM c");
        }

        [Fact]
        public void TranslatedQuery_ImplicitStringConversion()
        {
            var clauses = ODataTestHelper.FromQuery("");
            var result = Translator.Translate(clauses, Parameterized);

            string sql = result; // implicit conversion
            sql.Should().Be("SELECT * FROM c");
        }

        [Fact]
        public void TranslatedQuery_ToString_ReturnsSql()
        {
            var clauses = ODataTestHelper.FromQuery("");
            var result = Translator.Translate(clauses, Parameterized);

            result.ToString().Should().Be("SELECT * FROM c");
        }
    }
}
