using FluentAssertions;
using Microsoft.Azure.Cosmos.OData.Tests.Helpers;
using Xunit;

namespace Microsoft.Azure.Cosmos.OData.Tests
{
    /// <summary>
    /// Tests for query complexity limits (MaxTop, MaxOrderByProperties, MaxSelectProperties, MaxFilterDepth).
    /// </summary>
    public class ComplexityLimitTests
    {
        private static readonly ODataToCosmosSqlTranslator Translator = new();

        [Fact]
        public void MaxTop_Exceeded_Throws()
        {
            var clauses = ODataTestHelper.FromQuery("$top=100");
            var opts = new TranslationOptions { MaxTop = 50 };

            var act = () => Translator.Translate(clauses, opts);
            act.Should().Throw<ODataTranslationException>()
                .WithMessage("*exceeds the maximum*50*");
        }

        [Fact]
        public void MaxTop_NotExceeded_Succeeds()
        {
            var clauses = ODataTestHelper.FromQuery("$top=10");
            var opts = new TranslationOptions
            {
                MaxTop = 50,
                Parameterization = ParameterizationMode.Inline,
            };

            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().NotBeEmpty();
        }

        [Fact]
        public void MaxTop_Zero_Unlimited()
        {
            var clauses = ODataTestHelper.FromQuery("$top=999999");
            var opts = new TranslationOptions
            {
                MaxTop = 0, // unlimited
                MaxSkipValue = 0, // unlimited
                MaxFilterDepth = 0, // unlimited
                Parameterization = ParameterizationMode.Inline,
            };

            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().NotBeEmpty();
        }

        [Fact]
        public void MaxOrderByProperties_Exceeded_Throws()
        {
            var clauses = ODataTestHelper.FromQuery("$orderby=EnglishName asc,IntField desc,CompanyId asc");
            var opts = new TranslationOptions { MaxOrderByProperties = 2 };

            var act = () => Translator.Translate(clauses, opts);
            act.Should().Throw<ODataTranslationException>()
                .WithMessage("*$orderby*more than 2*");
        }

        [Fact]
        public void MaxOrderByProperties_NotExceeded_Succeeds()
        {
            var clauses = ODataTestHelper.FromQuery("$orderby=EnglishName asc,IntField desc");
            var opts = new TranslationOptions
            {
                MaxOrderByProperties = 2,
                Parameterization = ParameterizationMode.Inline,
            };

            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Contain("ORDER BY");
        }

        [Fact]
        public void MaxSelectProperties_Exceeded_Throws()
        {
            var clauses = ODataTestHelper.FromQuery("$select=EnglishName,IntField,Property,CompanyId");
            var opts = new TranslationOptions { MaxSelectProperties = 2 };

            var act = () => Translator.Translate(clauses, opts);
            act.Should().Throw<ODataTranslationException>()
                .WithMessage("*$select*exceeding the maximum*2*");
        }

        [Fact]
        public void MaxSelectProperties_NotExceeded_Succeeds()
        {
            var clauses = ODataTestHelper.FromQuery("$select=EnglishName,IntField");
            var opts = new TranslationOptions
            {
                MaxSelectProperties = 5,
                Parameterization = ParameterizationMode.Inline,
            };

            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().Contain("c.EnglishName");
        }

        [Fact]
        public void MaxFilterDepth_Exceeded_Throws()
        {
            // Create a deeply nested filter: a and (b and (c and (d and e)))
            var clauses = ODataTestHelper.FromQuery(
                "$filter=EnglishName eq 'a' and (IntField gt 1 and (Property eq 'b' and (CompanyId eq 'c' and EnglishName eq 'd')))");
            var opts = new TranslationOptions { MaxFilterDepth = 2 };

            var act = () => Translator.Translate(clauses, opts);
            act.Should().Throw<ODataTranslationException>()
                .WithMessage("*depth*exceeds*");
        }

        [Fact]
        public void MaxFilterDepth_NotExceeded_Succeeds()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=EnglishName eq 'a' and IntField gt 1");
            var opts = new TranslationOptions
            {
                MaxFilterDepth = 10,
                Parameterization = ParameterizationMode.Inline,
            };

            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().NotBeEmpty();
        }


        [Fact]
        public void DefaultMaxTop_RejectsOver1000()
        {
            var clauses = ODataTestHelper.FromQuery("$top=1001");
            var act = () => Translator.Translate(clauses, TranslationOptions.Default);
            act.Should().Throw<ODataTranslationException>()
                .WithMessage("*exceeds the maximum*1000*");
        }

        [Fact]
        public void MaxSkipValue_Exceeded_Throws()
        {
            var clauses = ODataTestHelper.FromQuery("$skip=20000");
            var act = () => Translator.Translate(clauses, TranslationOptions.Default);
            act.Should().Throw<ODataTranslationException>()
                .WithMessage("*$skip*exceeds*10000*");
        }

        [Fact]
        public void RequireFilter_NoFilter_Throws()
        {
            var clauses = ODataTestHelper.FromQuery("");
            var opts = new TranslationOptions { RequireFilter = true };
            var act = () => Translator.Translate(clauses, opts);
            act.Should().Throw<ODataTranslationException>()
                .WithMessage("*$filter*required*");
        }

        [Fact]
        public void RequireFilter_WithFilter_Succeeds()
        {
            var clauses = ODataTestHelper.FromQuery("$filter=IntField gt 5");
            var opts = new TranslationOptions
            {
                RequireFilter = true,
                Parameterization = ParameterizationMode.Inline,
            };
            var result = Translator.Translate(clauses, opts);
            result.Sql.Should().NotBeEmpty();
        }

        [Fact]
        public void ErrorCode_IsSet_OnComplexityViolation()
        {
            var clauses = ODataTestHelper.FromQuery("$top=2000");
            try
            {
                Translator.Translate(clauses, TranslationOptions.Default);
            }
            catch (ODataTranslationException ex)
            {
                ex.ErrorCode.Should().Be(ODataTranslationErrorCode.ComplexityLimitExceeded);
                return;
            }
            throw new System.Exception("Expected ODataTranslationException");
        }
    }
}
