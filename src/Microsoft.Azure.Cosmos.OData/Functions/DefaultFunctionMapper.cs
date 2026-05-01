using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Functions
{
    /// <summary>
    /// Maps OData v4 standard built-in functions and Cosmos DB extension functions
    /// to their Cosmos DB SQL equivalents.
    /// </summary>
    /// <remarks>
    /// Coverage:
    /// <list type="bullet">
    ///   <item><description>String: contains, startswith, endswith, length, indexof, substring, tolower, toupper, trim, concat, matchesPattern, left, right, replace, replicate, reverse, stringequals, tostring.</description></item>
    ///   <item><description>Math: round, floor, ceiling, abs, sign, power, sqrt, log, log10, exp, sin, cos, tan, atn, atn2, pi, degrees, radians, rand, numberbin.</description></item>
    ///   <item><description>Date/Time: year, month, day, hour, minute, second, datetimeadd, datetimediff, getcurrentdatetime, getcurrentticks, datetimebin, datetimetoticks, tickstodatetime, datetimetotimestamp, timestamptodatetime.</description></item>
    ///   <item><description>Type checking: isdefined, isnull, isbool, isnumber, isstring, isarray, isobject, isprimitive, isinteger, isfinitenumber.</description></item>
    ///   <item><description>Array: arraycontains, arraylength, arrayslice, arrayconcat.</description></item>
    /// </list>
    /// Custom function families (geospatial, vector search, full-text search) live in dedicated mappers
    /// composed via <see cref="CompositeFunctionMapper"/>.
    /// </remarks>
    public sealed class DefaultFunctionMapper : ISqlFunctionMapper
    {
        private static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // String functions
            "contains", "startswith", "endswith", "length", "indexof", "substring",
            "tolower", "toupper", "trim", "concat", "matchespattern",
            "left", "right", "replace", "replicate", "reverse", "stringequals", "tostring",

            // Math functions
            "round", "floor", "ceiling", "abs", "sign", "power", "sqrt",
            "log", "log10", "exp", "sin", "cos", "tan", "atn", "atn2",
            "pi", "degrees", "radians", "rand", "numberbin",

            // Date/Time functions
            "year", "month", "day", "hour", "minute", "second",
            "datetimeadd", "datetimediff", "getcurrentdatetime", "getcurrentticks",
            "datetimebin", "datetimetoticks", "tickstodatetime",
            "datetimetotimestamp", "timestamptodatetime",

            // Type checking functions
            "isdefined", "isnull", "isbool", "isnumber", "isstring",
            "isarray", "isobject", "isprimitive", "isinteger", "isfinitenumber",

            // Array functions
            "arraycontains", "arraylength", "arrayslice", "arrayconcat",
        };

        /// <inheritdoc />
        public bool CanMap(string odataFunctionName)
        {
            if (odataFunctionName == null) return false;
            return Names.Contains(odataFunctionName);
        }

        /// <inheritdoc />
        public SqlExpression Map(string odataFunctionName, IReadOnlyList<SqlExpression> arguments)
        {
            if (odataFunctionName == null) throw new ArgumentNullException(nameof(odataFunctionName));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));

            switch (odataFunctionName.ToLowerInvariant())
            {
                // -------- String functions --------
                case "contains":       return new SqlFunctionCall("CONTAINS", arguments);
                case "startswith":     return new SqlFunctionCall("STARTSWITH", arguments);
                case "endswith":       return new SqlFunctionCall("ENDSWITH", arguments);
                case "length":         return new SqlFunctionCall("LENGTH", arguments);
                case "indexof":        return new SqlFunctionCall("INDEX_OF", arguments);
                case "substring":      return new SqlFunctionCall("SUBSTRING", arguments);
                case "tolower":        return new SqlFunctionCall("LOWER", arguments);
                case "toupper":        return new SqlFunctionCall("UPPER", arguments);
                case "concat":         return new SqlFunctionCall("CONCAT", arguments);
                case "matchespattern": return new SqlFunctionCall("RegexMatch", arguments);
                case "left":           return new SqlFunctionCall("LEFT", arguments);
                case "right":          return new SqlFunctionCall("RIGHT", arguments);
                case "replace":        return new SqlFunctionCall("REPLACE", arguments);
                case "replicate":      return new SqlFunctionCall("REPLICATE", arguments);
                case "reverse":        return new SqlFunctionCall("REVERSE", arguments);
                case "stringequals":   return new SqlFunctionCall("StringEquals", arguments);
                case "tostring":       return new SqlFunctionCall("ToString", arguments);

                // trim() => LTRIM(RTRIM(x))
                case "trim":
                    return new SqlFunctionCall("LTRIM", new SqlExpression[]
                    {
                        new SqlFunctionCall("RTRIM", arguments),
                    });

                // -------- Math functions --------
                case "round":          return new SqlFunctionCall("ROUND", arguments);
                case "floor":          return new SqlFunctionCall("FLOOR", arguments);
                case "ceiling":        return new SqlFunctionCall("CEILING", arguments);
                case "abs":            return new SqlFunctionCall("ABS", arguments);
                case "sign":           return new SqlFunctionCall("SIGN", arguments);
                case "power":          return new SqlFunctionCall("POWER", arguments);
                case "sqrt":           return new SqlFunctionCall("SQRT", arguments);
                case "log":            return new SqlFunctionCall("LOG", arguments);
                case "log10":          return new SqlFunctionCall("LOG10", arguments);
                case "exp":            return new SqlFunctionCall("EXP", arguments);
                case "sin":            return new SqlFunctionCall("SIN", arguments);
                case "cos":            return new SqlFunctionCall("COS", arguments);
                case "tan":            return new SqlFunctionCall("TAN", arguments);
                case "atn":            return new SqlFunctionCall("ATN", arguments);
                case "atn2":           return new SqlFunctionCall("ATN2", arguments);
                case "pi":             return new SqlFunctionCall("PI", arguments);
                case "degrees":        return new SqlFunctionCall("DEGREES", arguments);
                case "radians":        return new SqlFunctionCall("RADIANS", arguments);
                case "rand":           return new SqlFunctionCall("RAND", arguments);
                case "numberbin":      return new SqlFunctionCall("NumberBin", arguments);

                // -------- Date/Time functions --------
                case "year":           return new SqlFunctionCall("DateTimePart", PrependLiteral("yyyy", arguments));
                case "month":          return new SqlFunctionCall("DateTimePart", PrependLiteral("mm", arguments));
                case "day":            return new SqlFunctionCall("DateTimePart", PrependLiteral("dd", arguments));
                case "hour":           return new SqlFunctionCall("DateTimePart", PrependLiteral("hh", arguments));
                case "minute":         return new SqlFunctionCall("DateTimePart", PrependLiteral("mi", arguments));
                case "second":         return new SqlFunctionCall("DateTimePart", PrependLiteral("ss", arguments));
                case "datetimeadd":    return new SqlFunctionCall("DateTimeAdd", arguments);
                case "datetimediff":   return new SqlFunctionCall("DateTimeDiff", arguments);
                case "getcurrentdatetime": return new SqlFunctionCall("GetCurrentDateTime", arguments);
                case "getcurrentticks":    return new SqlFunctionCall("GetCurrentTicks", arguments);
                case "datetimebin":    return new SqlFunctionCall("DateTimeBin", arguments);
                case "datetimetoticks":    return new SqlFunctionCall("DateTimeToTicks", arguments);
                case "tickstodatetime":    return new SqlFunctionCall("TicksToDateTime", arguments);
                case "datetimetotimestamp": return new SqlFunctionCall("DateTimeToTimestamp", arguments);
                case "timestamptodatetime": return new SqlFunctionCall("TimestampToDateTime", arguments);

                // -------- Type checking functions --------
                case "isdefined":      return new SqlFunctionCall("IS_DEFINED", arguments);
                case "isnull":         return new SqlFunctionCall("IS_NULL", arguments);
                case "isbool":         return new SqlFunctionCall("IS_BOOL", arguments);
                case "isnumber":       return new SqlFunctionCall("IS_NUMBER", arguments);
                case "isstring":       return new SqlFunctionCall("IS_STRING", arguments);
                case "isarray":        return new SqlFunctionCall("IS_ARRAY", arguments);
                case "isobject":       return new SqlFunctionCall("IS_OBJECT", arguments);
                case "isprimitive":    return new SqlFunctionCall("IS_PRIMITIVE", arguments);
                case "isinteger":      return new SqlFunctionCall("IS_INTEGER", arguments);
                case "isfinitenumber": return new SqlFunctionCall("IS_FINITE_NUMBER", arguments);

                // -------- Array functions --------
                case "arraycontains":  return new SqlFunctionCall("ARRAY_CONTAINS", arguments);
                case "arraylength":    return new SqlFunctionCall("ARRAY_LENGTH", arguments);
                case "arrayslice":     return new SqlFunctionCall("ARRAY_SLICE", arguments);
                case "arrayconcat":    return new SqlFunctionCall("ARRAY_CONCAT", arguments);

                default:
                    throw new UnsupportedODataFeatureException($"Function '{odataFunctionName}' is not supported.");
            }
        }

        private static IReadOnlyList<SqlExpression> PrependLiteral(string literal, IReadOnlyList<SqlExpression> tail)
        {
            var list = new List<SqlExpression>(tail.Count + 1) { new SqlLiteral(literal) };
            for (int i = 0; i < tail.Count; i++) list.Add(tail[i]);
            return list;
        }
    }
}
