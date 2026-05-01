using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Azure.Cosmos.OData.Ast;
using Microsoft.Azure.Cosmos.OData.Functions;
using Microsoft.Azure.Cosmos.OData.Naming;
using Microsoft.Azure.Cosmos.OData.Rendering;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Top-level entry point for translating OData V4 query clauses into Cosmos DB SQL.
    /// </summary>
    /// <remarks>
    /// The translator is framework-agnostic — it accepts the parsed clauses produced by
    /// <c>Microsoft.OData.Core</c>'s URI parser and emits a <see cref="TranslatedQuery"/>.
    /// Adapter packages (<c>Microsoft.Azure.Cosmos.OData.AspNet</c>,
    /// <c>Microsoft.Azure.Cosmos.OData.AspNetCore</c>) provide thin extension methods over
    /// <c>ODataQueryOptions</c> that delegate here.
    /// <para>
    /// Pipeline (Single Responsibility):
    /// <list type="number">
    ///   <item><description><see cref="IFieldNameResolver"/> turns OData property paths into <c>c.field.subfield</c>.</description></item>
    ///   <item><description><see cref="ISqlFunctionMapper"/> maps OData functions onto Cosmos SQL functions.</description></item>
    ///   <item><description>An internal visitor builds an immutable <see cref="SqlExpression"/> tree.</description></item>
    ///   <item><description><see cref="ISqlExpressionRenderer"/> serializes the tree, optionally substituting parameters.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class ODataToCosmosSqlTranslator
    {
        private readonly IFieldNameResolver _fieldNames;
        private readonly ISqlFunctionMapper _functions;
        private readonly Func<ParameterizationMode, ISqlExpressionRenderer> _rendererFactory;

        /// <summary>
        /// Constructs a translator with the default Cosmos function set
        /// (string + math + date + null/array helpers + geospatial + vector + full-text search).
        /// </summary>
        public ODataToCosmosSqlTranslator() : this(
            fieldNames: new DefaultFieldNameResolver(),
            functions: DefaultFunctions(),
            rendererFactory: mode => new CosmosSqlRenderer(mode))
        {
        }

        /// <summary>Fully customized constructor; useful for DI.</summary>
        public ODataToCosmosSqlTranslator(
            IFieldNameResolver fieldNames,
            ISqlFunctionMapper functions,
            Func<ParameterizationMode, ISqlExpressionRenderer> rendererFactory)
        {
            _fieldNames = fieldNames ?? throw new ArgumentNullException(nameof(fieldNames));
            _functions = functions ?? throw new ArgumentNullException(nameof(functions));
            _rendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
        }

        /// <summary>The composed default Cosmos function mapper used when none is supplied.</summary>
        public static ISqlFunctionMapper DefaultFunctions() => new CompositeFunctionMapper(
            new DefaultFunctionMapper(),
            new GeospatialFunctionMapper(),
            new VectorSearchFunctionMapper(),
            new FullTextSearchFunctionMapper());

        /// <summary>
        /// Create an <see cref="IODataToSqlExpressionTranslator"/> that converts OData
        /// <see cref="Microsoft.OData.UriParser.QueryNode"/> instances into <see cref="Ast.SqlExpression"/> trees.
        /// Useful for custom AST manipulation or debugging.
        /// </summary>
        public IODataToSqlExpressionTranslator CreateExpressionTranslator()
            => new Translation.ODataExpressionVisitor(_fieldNames, _functions);

        /// <summary>Translate the given OData clauses into Cosmos SQL using the default options.</summary>
        public TranslatedQuery Translate(ODataQueryClauses clauses)
            => Translate(clauses, TranslationOptions.Default);

        /// <summary>Translate the given OData clauses into Cosmos SQL.</summary>
        public TranslatedQuery Translate(ODataQueryClauses clauses, TranslationOptions options)
        {
            if (clauses == null) throw new ArgumentNullException(nameof(clauses));
            if (options == null) throw new ArgumentNullException(nameof(options));

            // ----- Enforce query complexity limits -----
            ValidateComplexityLimits(clauses, options);

            var renderer = _rendererFactory(options.Parameterization);
            var visitor = new Translation.ODataExpressionVisitor(_fieldNames, _functions);
            var parameters = new Dictionary<string, object?>();

            // ----- $apply takes priority because it produces a different SELECT shape -----
            if ((options.Clauses & TranslationClauses.Apply) != 0 && clauses.Apply != null)
            {
                return TranslateApply(clauses, options, renderer, visitor, parameters);
            }

            var sb = new StringBuilder();
            string? selectFragment = null;
            string? whereFragment = null;
            string? orderByFragment = null;
            string? paginationFragment = null;
            string? countSql = null;

            // ----- WHERE -----
            if ((options.Clauses & TranslationClauses.Filter) != 0)
            {
                whereFragment = BuildWhere(clauses.Filter, clauses.Search, options, renderer, visitor, parameters);
            }

            // ----- SELECT (with optional TOP for legacy mode) -----
            if ((options.Clauses & TranslationClauses.Select) != 0)
            {
                selectFragment = BuildSelect(clauses.Select, options, useTop: options.Pagination == PaginationMode.Top
                    && (options.Clauses & TranslationClauses.Pagination) != 0
                    ? clauses.Top
                    : null);
            }

            // ----- ORDER BY -----
            if ((options.Clauses & TranslationClauses.OrderBy) != 0 && clauses.OrderBy != null)
            {
                orderByFragment = BuildOrderBy(clauses.OrderBy, renderer, visitor, parameters);
            }

            // ----- OFFSET ... LIMIT ... -----
            if ((options.Clauses & TranslationClauses.Pagination) != 0 && options.Pagination == PaginationMode.OffsetLimit)
            {
                paginationFragment = BuildOffsetLimit(clauses.Top, clauses.Skip);
            }

            // ----- $count=true -> companion COUNT query -----
            if ((options.Clauses & TranslationClauses.Count) != 0 && clauses.Count == true)
            {
                countSql = BuildCount(clauses.Filter, options, visitor);
            }

            // ----- assemble -----
            if (selectFragment != null)
            {
                sb.Append(selectFragment);
            }

            if (!string.IsNullOrEmpty(whereFragment))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append("WHERE ").Append(whereFragment).Append(' ');
            }
            else if (selectFragment != null)
            {
                sb.Append(' ');
            }

            if (!string.IsNullOrEmpty(orderByFragment))
            {
                sb.Append("ORDER BY ").Append(orderByFragment).Append(' ');
            }

            if (!string.IsNullOrEmpty(paginationFragment))
            {
                sb.Append(paginationFragment);
            }

            var sql = sb.ToString().TrimEnd();

            // SEC-09: Enforce SQL output length limit
            if (options.MaxGeneratedSqlLength > 0 && sql.Length > options.MaxGeneratedSqlLength)
            {
                throw new ODataTranslationException(
                    $"Generated SQL length ({sql.Length}) exceeds the maximum of {options.MaxGeneratedSqlLength} characters.",
                    ODataTranslationErrorCode.ComplexityLimitExceeded);
            }

            return new TranslatedQuery(sql, parameters, countSql);
        }

        // -------- WHERE --------

        private string? BuildWhere(
            FilterClause? filter,
            SearchClause? search,
            TranslationOptions options,
            ISqlExpressionRenderer renderer,
            Translation.ODataExpressionVisitor visitor,
            IDictionary<string, object?> parameters)
        {
            string? filterSql = null;
            if (filter != null)
            {
                var ast = visitor.Translate(filter.Expression);
                filterSql = renderer.Render(ast, parameters);
            }

            // $search → FullTextContains(c, 'term')
            if (search != null)
            {
                var searchAst = visitor.Translate(search.Expression);
                // Wrap the search term in FullTextContains(c, 'term') if it's a bare literal
                SqlExpression searchExpr;
                if (searchAst is SqlLiteral)
                {
                    searchExpr = new Ast.SqlFunctionCall("FullTextContains",
                        new Ast.SqlExpression[] { new Ast.SqlMember(options.DocumentAlias), searchAst });
                }
                else
                {
                    searchExpr = searchAst;
                }
                var searchSql = renderer.Render(searchExpr, parameters);
                filterSql = string.IsNullOrEmpty(filterSql)
                    ? searchSql
                    : filterSql + " AND " + searchSql;
            }

            string? additional = options.AdditionalWhereClause;
            if (!string.IsNullOrEmpty(additional))
            {
                if (options.AdditionalParameters != null)
                {
                    foreach (var kv in options.AdditionalParameters)
                    {
                        var key = kv.Key.StartsWith("@", StringComparison.Ordinal) ? kv.Key : "@" + kv.Key;
                        parameters[key] = kv.Value;
                    }
                }

                if (string.IsNullOrEmpty(filterSql))
                {
                    return additional;
                }

                return additional + " AND " + filterSql;
            }

            return filterSql;
        }

        // -------- SELECT --------

        private string BuildSelect(SelectExpandClause? select, TranslationOptions options, long? useTop)
        {
            var topPart = useTop.HasValue && useTop.Value > 0
                ? "TOP " + useTop.Value.ToString(CultureInfo.InvariantCulture) + " "
                : string.Empty;

            string projection;
            if (select == null || select.AllSelected || !select.SelectedItems.Any())
            {
                projection = "*";
            }
            else
            {
                var fieldPaths = new List<string>();
                foreach (var item in select.SelectedItems)
                {
                    if (item is PathSelectItem path)
                    {
                        var name = SegmentName(path);
                        if (!string.IsNullOrEmpty(name))
                        {
                            fieldPaths.Add(_fieldNames.TranslateFieldName(name));
                        }
                    }
                }

                projection = fieldPaths.Count == 0 ? "*" : string.Join(", ", fieldPaths);
            }

            var selectKeyword = "SELECT ";
            if (options.Distinct) selectKeyword = "SELECT DISTINCT ";
            if (options.ValueProjection && !projection.Contains(",") && projection != "*")
            {
                selectKeyword = options.Distinct ? "SELECT DISTINCT VALUE " : "SELECT VALUE ";
            }

            return selectKeyword + topPart + projection + " FROM " + options.FromName;
        }

        private static string SegmentName(PathSelectItem item)
        {
            // PathSelectItem.SelectedPath is an ODataSelectPath; we take the last segment's identifier.
            var seg = item.SelectedPath?.LastSegment;
            switch (seg)
            {
                case PropertySegment ps: return ps.Property.Name;
                case DynamicPathSegment dps: return dps.Identifier;
                case NavigationPropertySegment nps: return nps.NavigationProperty.Name;
                default:
                    return seg?.Identifier ?? string.Empty;
            }
        }

        // -------- ORDER BY --------

        private string BuildOrderBy(
            OrderByClause? orderBy,
            ISqlExpressionRenderer renderer,
            Translation.ODataExpressionVisitor visitor,
            IDictionary<string, object?> parameters)
        {
            var parts = new List<string>();
            for (var clause = orderBy; clause != null; clause = clause.ThenBy)
            {
                var expr = visitor.Translate(clause.Expression);
                var rendered = renderer.Render(expr, parameters);
                var dir = clause.Direction == OrderByDirection.Descending ? " DESC" : " ASC";
                parts.Add(rendered + dir);
            }

            return string.Join(", ", parts);
        }

        // -------- pagination --------

        private static string BuildOffsetLimit(long? top, long? skip)
        {
            if ((top == null || top <= 0) && (skip == null || skip <= 0))
            {
                return string.Empty;
            }

            var skipVal = skip.GetValueOrDefault(0);
            var topVal = top.GetValueOrDefault(int.MaxValue);
            return "OFFSET " + skipVal.ToString(CultureInfo.InvariantCulture)
                + " LIMIT " + topVal.ToString(CultureInfo.InvariantCulture);
        }

        // -------- $count=true companion -----

        private string BuildCount(FilterClause? filter, TranslationOptions options, Translation.ODataExpressionVisitor visitor)
        {
            var paramsForCount = new Dictionary<string, object?>();
            var renderer = _rendererFactory(options.Parameterization);
            var sb = new StringBuilder("SELECT VALUE COUNT(1) FROM ").Append(options.FromName);
            if (filter != null)
            {
                var ast = visitor.Translate(filter.Expression);
                var rendered = renderer.Render(ast, paramsForCount);
                sb.Append(" WHERE ").Append(rendered);
            }
            return sb.ToString();
        }

        // -------- $apply (aggregate / groupby) -------------------------------------------------

        private TranslatedQuery TranslateApply(
            ODataQueryClauses clauses,
            TranslationOptions options,
            ISqlExpressionRenderer renderer,
            Translation.ODataExpressionVisitor visitor,
            IDictionary<string, object?> parameters)
        {
            var apply = clauses.Apply!;

            var groupings = new List<GroupByPropertyNode>();
            var aggregations = new List<AggregateExpressionBase>();
            FilterClause? applyFilter = null;

            foreach (var t in apply.Transformations)
            {
                switch (t)
                {
                    case GroupByTransformationNode g:
                        if (g.GroupingProperties != null) groupings.AddRange(g.GroupingProperties);
                        if (g.ChildTransformations is AggregateTransformationNode innerAgg)
                        {
                            aggregations.AddRange(innerAgg.AggregateExpressions);
                        }
                        break;

                    case AggregateTransformationNode a:
                        aggregations.AddRange(a.AggregateExpressions);
                        break;

                    case FilterTransformationNode f:
                        applyFilter = f.FilterClause;
                        break;

                    default:
                        throw new UnsupportedODataFeatureException(
                            $"$apply transformation '{t.GetType().Name}' is not supported.");
                }
            }

            // SELECT projection
            var selectParts = new List<string>();
            var groupByParts = new List<string>();
            foreach (var g in groupings)
            {
                if (g.Expression == null) continue;
                var ast = visitor.Translate(g.Expression);
                var rendered = renderer.Render(ast, parameters);
                var alias = (g.Name ?? "key").Replace('/', '_');
                selectParts.Add(rendered + " AS " + alias);
                groupByParts.Add(rendered);
            }

            foreach (var a in aggregations)
            {
                if (a is AggregateExpression ae)
                {
                    var func = AggregateMethodToSql(ae.Method);
                    var inner = visitor.Translate(ae.Expression);
                    var argSql = renderer.Render(inner, parameters);
                    var name = ae.Alias ?? "agg";
                    if (func == "COUNT" && ae.Expression is CountNode)
                    {
                        // COUNT() over a collection — Cosmos uses ARRAY_LENGTH already from visitor
                        selectParts.Add(argSql + " AS " + name);
                    }
                    else
                    {
                        selectParts.Add(func + "(" + argSql + ") AS " + name);
                    }
                }
                else
                {
                    throw new UnsupportedODataFeatureException(
                        "Only standard aggregate expressions are supported in $apply.");
                }
            }

            if (selectParts.Count == 0)
            {
                throw new UnsupportedODataFeatureException("$apply requires at least one groupby or aggregate.");
            }

            var sb = new StringBuilder("SELECT ");
            sb.Append(string.Join(", ", selectParts));
            sb.Append(" FROM ").Append(options.FromName);

            // WHERE — combine $filter and any filter() inside $apply
            string? where = null;
            if ((options.Clauses & TranslationClauses.Filter) != 0)
            {
                var w1 = clauses.Filter == null ? null : renderer.Render(visitor.Translate(clauses.Filter.Expression), parameters);
                var w2 = applyFilter == null ? null : renderer.Render(visitor.Translate(applyFilter.Expression), parameters);
                where = (w1, w2) switch
                {
                    (null, null) => null,
                    (null, _) => w2,
                    (_, null) => w1,
                    _ => w1 + " AND " + w2,
                };
            }

            if (!string.IsNullOrEmpty(where))
            {
                sb.Append(" WHERE ").Append(where);
            }

            if (groupByParts.Count > 0)
            {
                sb.Append(" GROUP BY ").Append(string.Join(", ", groupByParts));
            }

            return new TranslatedQuery(sb.ToString(), parameters);
        }

        // -------- query complexity validation --------

        private static void ValidateComplexityLimits(ODataQueryClauses clauses, TranslationOptions options)
        {
            // RequireFilter (SEC-09)
            if (options.RequireFilter && clauses.Filter == null)
            {
                throw new ODataTranslationException(
                    "A $filter clause is required but none was provided.",
                    ODataTranslationErrorCode.FilterRequired);
            }

            // MaxTop
            if (options.MaxTop > 0 && clauses.Top.HasValue && clauses.Top.Value > options.MaxTop)
            {
                throw new ODataTranslationException(
                    $"$top value {clauses.Top.Value} exceeds the maximum allowed value of {options.MaxTop}.",
                    ODataTranslationErrorCode.ComplexityLimitExceeded);
            }

            // MaxSkipValue (SEC-07)
            if (options.MaxSkipValue > 0 && clauses.Skip.HasValue && clauses.Skip.Value > options.MaxSkipValue)
            {
                throw new ODataTranslationException(
                    $"$skip value {clauses.Skip.Value} exceeds the maximum allowed value of {options.MaxSkipValue}.",
                    ODataTranslationErrorCode.ComplexityLimitExceeded);
            }

            // MaxOrderByProperties
            if (options.MaxOrderByProperties > 0 && clauses.OrderBy != null)
            {
                int count = 0;
                for (var ob = clauses.OrderBy; ob != null; ob = ob.ThenBy)
                {
                    count++;
                    if (count > options.MaxOrderByProperties)
                    {
                        throw new ODataTranslationException(
                            $"$orderby contains more than {options.MaxOrderByProperties} properties.",
                            ODataTranslationErrorCode.ComplexityLimitExceeded);
                    }
                }
            }

            // MaxSelectProperties
            if (options.MaxSelectProperties > 0 && clauses.Select != null && !clauses.Select.AllSelected)
            {
                int count = clauses.Select.SelectedItems.Count();
                if (count > options.MaxSelectProperties)
                {
                    throw new ODataTranslationException(
                        $"$select contains {count} properties, exceeding the maximum of {options.MaxSelectProperties}.",
                        ODataTranslationErrorCode.ComplexityLimitExceeded);
                }
            }

            // MaxFilterDepth
            if (options.MaxFilterDepth > 0 && clauses.Filter != null)
            {
                int depth = MeasureNodeDepth(clauses.Filter.Expression);
                if (depth > options.MaxFilterDepth)
                {
                    throw new ODataTranslationException(
                        $"$filter expression depth ({depth}) exceeds the maximum allowed depth of {options.MaxFilterDepth}.",
                        ODataTranslationErrorCode.ComplexityLimitExceeded);
                }
            }
        }

        /// <summary>
        /// Iterative depth measurement with hard stack overflow protection (SEC-08).
        /// </summary>
        private static int MeasureNodeDepth(QueryNode node)
        {
            const int HardLimit = 200;
            var stack = new Stack<(QueryNode Node, int Depth)>();
            stack.Push((node, 1));
            int maxDepth = 0;

            while (stack.Count > 0)
            {
                var (current, depth) = stack.Pop();
                if (depth > HardLimit)
                {
                    throw new ODataTranslationException(
                        $"$filter expression depth exceeds hard limit of {HardLimit}.",
                        ODataTranslationErrorCode.ComplexityLimitExceeded);
                }

                if (depth > maxDepth) maxDepth = depth;

                switch (current)
                {
                    case BinaryOperatorNode bin:
                        stack.Push((bin.Left, depth + 1));
                        stack.Push((bin.Right, depth + 1));
                        break;
                    case UnaryOperatorNode un:
                        stack.Push((un.Operand, depth + 1));
                        break;
                    case ConvertNode conv:
                        stack.Push((conv.Source, depth));
                        break;
                }
            }

            return maxDepth;
        }

        private static string AggregateMethodToSql(AggregationMethod method)
        {
            switch (method)
            {
                case AggregationMethod.Sum:           return "SUM";
                case AggregationMethod.Min:           return "MIN";
                case AggregationMethod.Max:           return "MAX";
                case AggregationMethod.Average:       return "AVG";
                case AggregationMethod.CountDistinct: return "COUNT";
                case AggregationMethod.VirtualPropertyCount: return "COUNT";
                default:
                    throw new UnsupportedODataFeatureException($"Aggregation method '{method}' is not supported.");
            }
        }
    }
}
