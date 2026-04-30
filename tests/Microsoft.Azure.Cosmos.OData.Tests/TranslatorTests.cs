using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Azure.Cosmos.OData.Tests.Helpers;
using Xunit;

namespace Microsoft.Azure.Cosmos.OData.Tests
{
    /// <summary>
    /// Tests ported from the v1 ODataToSqlSamples + new modern-feature tests.
    /// All tests use inline mode for easy comparison; parameterized mode has its own suite.
    /// </summary>
    public class TranslatorTests
    {
        private static readonly ODataToCosmosSqlTranslator Translator = new();

        private static readonly TranslationOptions Inline = new TranslationOptions
        {
            Parameterization = ParameterizationMode.Inline,
        };

        private static readonly TranslationOptions InlineTopMode = new TranslationOptions
        {
            Parameterization = ParameterizationMode.Inline,
            Pagination = PaginationMode.Top,
        };

        // -------- v1 compat: SELECT --------

        [Fact]
        public void SelectAll_ReturnsSelectStarFromC()
        {
            var clauses = ODataTestHelper.FromQuery("");
            var result = Translator.Translate(clauses, Inline);
            result.Sql.Should().Be("SELECT * FROM c");
        }

        [Fact]
        public void Select_SpecificFields()
        {
            var clauses = ODataTestHelper.FromQuery("$select=EnglishName,Id");
            var result = Translator.Translate(clauses, Inline);
            result.Sql.Should().Be("SELECT c.EnglishName, c.Id FROM c");
        }

        [Fact]
        public void SelectAllWithTop_UsingTopMode()
        {
            var clauses = ODataTestHelper.FromQuery("$top=15");
            var result = Translator.Translate(clauses, InlineTopMode);
            result.Sql.Should().Be("SELECT TOP 15 * FROM c");
        }

        [Fact]
        public void SelectWithTop_UsingTopMode()
        {
            var clauses = ODataTestHelper.FromQuery("$select=P1,P2,P3&$top=15");
            var result = Translator.Translate(clauses, InlineTopMode);
            result.Sql.Should().Be("SELECT TOP 15 c.P1, c.P2, c.P3 FROM c");
        }

        // -------- v1 compat: WHERE --------

        [Fact]
        public void Filter_EqualAndLessThanOrEqual()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=EnglishName eq 'Microsoft' and IntField le 5");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE c.EnglishName = 'Microsoft' AND c.IntField <= 5");
        }

        [Fact]
        public void Filter_NotEqual()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=Property ne 'str1'");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE c.Property != 'str1'");
        }

        [Fact]
        public void Filter_Contains()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=contains(EnglishName,'Limited')");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE CONTAINS(c.EnglishName,'Limited')");
        }

        [Fact]
        public void Filter_StartsWith()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=startswith(EnglishName,'Micro')");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE STARTSWITH(c.EnglishName,'Micro')");
        }

        [Fact]
        public void Filter_EndsWith()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=endswith(EnglishName,'soft')");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE ENDSWITH(c.EnglishName,'soft')");
        }

        [Fact]
        public void Filter_ToUpper()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=toupper(EnglishName) eq 'MICROSOFT'");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE UPPER(c.EnglishName) = 'MICROSOFT'");
        }

        [Fact]
        public void Filter_ToLower()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=tolower(EnglishName) eq 'microsoft'");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE LOWER(c.EnglishName) = 'microsoft'");
        }

        [Fact]
        public void Filter_Length()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=length(EnglishName) gt 10");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE LENGTH(c.EnglishName) > 10");
        }

        [Fact]
        public void Filter_IndexOf()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=indexof(EnglishName,'soft') gt 0");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE INDEX_OF(c.EnglishName,'soft') > 0");
        }

        [Fact]
        public void Filter_Trim()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=trim(EnglishName) eq 'Microsoft'");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("WHERE LTRIM(RTRIM(c.EnglishName)) = 'Microsoft'");
        }

        // -------- ORDER BY --------

        [Fact]
        public void OrderBy_MultipleFields()
        {
            var clauses = ODataTestHelper.FromQuery("$orderby=CompanyId desc,Id asc");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.OrderBy,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Be("ORDER BY c.CompanyId DESC, c.Id ASC");
        }

        // -------- OFFSET / LIMIT (new in v3) --------

        [Fact]
        public void TopAndSkip_ProduceOffsetLimit()
        {
            var clauses = ODataTestHelper.FromQuery("$top=10&$skip=20");
            var result = Translator.Translate(clauses, Inline);
            result.Sql.Should().Contain("OFFSET 20 LIMIT 10");
        }

        // -------- Parameterization --------

        [Fact]
        public void Parameterized_FilterUsesParameters()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=EnglishName eq 'Microsoft'");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Parameters,
                Clauses = TranslationClauses.Filter,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Contain("@p0");
            result.Parameters.Should().ContainKey("@p0");
        }

        // -------- Combined query (full SQL) --------

        [Fact]
        public void FullQuery_SelectFilterOrderByOffsetLimit()
        {
            var clauses = ODataTestHelper.FromQuery("$select=EnglishName&$filter=IntField gt 5&$orderby=IntField desc&$top=10&$skip=0");
            var result = Translator.Translate(clauses, Inline);
            result.Sql.Should().StartWith("SELECT c.EnglishName FROM c WHERE");
            result.Sql.Should().Contain("c.IntField > 5");
            result.Sql.Should().Contain("ORDER BY c.IntField DESC");
            result.Sql.Should().Contain("OFFSET 0 LIMIT 10");
        }

        // -------- $count=true --------

        [Fact]
        public void CountTrue_EmitsCountSql()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=IntField gt 5&$count=true");
            var result = Translator.Translate(clauses, Inline);
            result.CountSql.Should().NotBeNullOrEmpty();
            result.CountSql.Should().StartWith("SELECT VALUE COUNT(1) FROM c");
        }

        // -------- AdditionalWhereClause --------

        [Fact]
        public void AdditionalWhereClause_CombinesWithFilter()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=IntField gt 5");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Clauses = TranslationClauses.Filter,
                AdditionalWhereClause = "c.type = 'company'",
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Contain("c.type = 'company'");
            result.Sql.Should().Contain("c.IntField > 5");
        }
    }
}
