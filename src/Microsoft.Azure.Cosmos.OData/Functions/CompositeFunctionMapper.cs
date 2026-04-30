using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Functions
{
    /// <summary>
    /// Composes several <see cref="ISqlFunctionMapper"/>s.  The first mapper that returns
    /// <see cref="ISqlFunctionMapper.CanMap"/> wins.
    /// </summary>
    public sealed class CompositeFunctionMapper : ISqlFunctionMapper
    {
        private readonly IReadOnlyList<ISqlFunctionMapper> _mappers;

        /// <summary>Compose the given mappers in priority order.</summary>
        public CompositeFunctionMapper(params ISqlFunctionMapper[] mappers)
            : this((IReadOnlyList<ISqlFunctionMapper>)(mappers ?? Array.Empty<ISqlFunctionMapper>()))
        {
        }

        /// <summary>Compose the given mappers in priority order.</summary>
        public CompositeFunctionMapper(IReadOnlyList<ISqlFunctionMapper> mappers)
        {
            _mappers = mappers ?? throw new ArgumentNullException(nameof(mappers));
        }

        /// <inheritdoc />
        public bool CanMap(string odataFunctionName)
        {
            for (int i = 0; i < _mappers.Count; i++)
            {
                if (_mappers[i].CanMap(odataFunctionName)) return true;
            }

            return false;
        }

        /// <inheritdoc />
        public SqlExpression Map(string odataFunctionName, IReadOnlyList<SqlExpression> arguments)
        {
            for (int i = 0; i < _mappers.Count; i++)
            {
                if (_mappers[i].CanMap(odataFunctionName))
                {
                    return _mappers[i].Map(odataFunctionName, arguments);
                }
            }

            throw new UnsupportedODataFeatureException($"Function '{odataFunctionName}' is not supported.");
        }
    }
}
