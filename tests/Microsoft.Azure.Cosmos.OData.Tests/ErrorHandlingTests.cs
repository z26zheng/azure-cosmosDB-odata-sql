using System;
using FluentAssertions;
using Microsoft.Azure.Cosmos.OData.Ast;
using Microsoft.Azure.Cosmos.OData.Functions;
using Microsoft.Azure.Cosmos.OData.Rendering;
using Xunit;

namespace Microsoft.Azure.Cosmos.OData.Tests
{
    /// <summary>
    /// Tests for error handling, edge cases, and null inputs.
    /// </summary>
    public class ErrorHandlingTests
    {
        private static readonly ODataToCosmosSqlTranslator Translator = new();

        [Fact]
        public void Translate_NullClauses_ThrowsArgumentNullException()
        {
            var act = () => Translator.Translate(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("clauses");
        }

        [Fact]
        public void Translate_NullOptions_ThrowsArgumentNullException()
        {
            var act = () => Translator.Translate(new ODataQueryClauses(), null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void EmptyClauses_ProducesSelectStarFromC()
        {
            var result = Translator.Translate(new ODataQueryClauses(), TranslationOptions.Default);
            result.Sql.Should().Be("SELECT * FROM c");
        }

        [Fact]
        public void FilterOnly_NoSelect_ProducesWhereClause()
        {
            var clauses = Helpers.ODataTestHelper.FromQuery("$filter=IntField gt 5");
            var opts = new TranslationOptions
            {
                Clauses = TranslationClauses.Filter,
                Parameterization = ParameterizationMode.Inline,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().StartWith("WHERE");
        }

        [Fact]
        public void OrderByOnly_ProducesOrderByClause()
        {
            var clauses = Helpers.ODataTestHelper.FromQuery("$orderby=EnglishName asc");
            var opts = new TranslationOptions
            {
                Clauses = TranslationClauses.OrderBy,
                Parameterization = ParameterizationMode.Inline,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().StartWith("ORDER BY");
        }

        [Fact]
        public void NoClauses_EmptyResult()
        {
            var clauses = Helpers.ODataTestHelper.FromQuery("");
            var opts = new TranslationOptions
            {
                Clauses = TranslationClauses.None,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().BeEmpty();
        }

        [Fact]
        public void Renderer_NullExpression_ThrowsArgumentNullException()
        {
            var renderer = new CosmosSqlRenderer();
            var act = () => renderer.Render(null!, new System.Collections.Generic.Dictionary<string, object?>());
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Renderer_NullParameters_ThrowsArgumentNullException()
        {
            var renderer = new CosmosSqlRenderer();
            var act = () => renderer.Render(new SqlMember("c.x"), null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DefaultFunctionMapper_NullName_CanMapReturnsFalse()
        {
            var mapper = new DefaultFunctionMapper();
            mapper.CanMap(null!).Should().BeFalse();
        }

        [Fact]
        public void DefaultFunctionMapper_NullName_Map_ThrowsArgumentNull()
        {
            var mapper = new DefaultFunctionMapper();
            var act = () => mapper.Map(null!, new SqlExpression[] { });
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullFieldNames_ThrowsArgumentNull()
        {
            var act = () => new ODataToCosmosSqlTranslator(null!, ODataToCosmosSqlTranslator.DefaultFunctions(), m => new CosmosSqlRenderer(m));
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullFunctions_ThrowsArgumentNull()
        {
            var act = () => new ODataToCosmosSqlTranslator(new Naming.DefaultFieldNameResolver(), null!, m => new CosmosSqlRenderer(m));
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullRendererFactory_ThrowsArgumentNull()
        {
            var act = () => new ODataToCosmosSqlTranslator(new Naming.DefaultFieldNameResolver(), ODataToCosmosSqlTranslator.DefaultFunctions(), null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TopOnly_OffsetLimitMode_ProducesOffset()
        {
            var clauses = Helpers.ODataTestHelper.FromQuery("$top=10");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Pagination = PaginationMode.OffsetLimit,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Contain("OFFSET 0 LIMIT 10");
        }

        [Fact]
        public void SkipOnly_ProducesOffset()
        {
            var clauses = Helpers.ODataTestHelper.FromQuery("$skip=20");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                Pagination = PaginationMode.OffsetLimit,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Contain("OFFSET 20 LIMIT");
        }

        [Fact]
        public void CustomDocumentAlias_UsesIt()
        {
            var clauses = Helpers.ODataTestHelper.FromQuery("");
            var opts = new TranslationOptions
            {
                Parameterization = ParameterizationMode.Inline,
                FromName = "root",
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Contain("FROM root");
        }
    }
}
