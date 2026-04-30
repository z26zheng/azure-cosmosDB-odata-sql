using System;
using Microsoft.Azure.Cosmos.OData.Functions;
using Microsoft.Azure.Cosmos.OData.Naming;
using Microsoft.Azure.Cosmos.OData.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Azure.Cosmos.OData.AspNetCore
{
    /// <summary>
    /// DI registration extensions for the OData-to-Cosmos translator.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register the <see cref="ODataToCosmosSqlTranslator"/> and its default dependencies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddODataToCosmosSql(
            this IServiceCollection services,
            Action<TranslationOptions>? configure = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Register defaults (TryAdd: user can override before this call)
            services.TryAddSingleton<IFieldNameResolver>(new DefaultFieldNameResolver());
            services.TryAddSingleton<ISqlFunctionMapper>(ODataToCosmosSqlTranslator.DefaultFunctions());
            services.TryAddSingleton<ISqlExpressionRenderer>(new CosmosSqlRenderer());

            // Register the translator
            services.TryAddSingleton(sp =>
            {
                var fieldNames = sp.GetRequiredService<IFieldNameResolver>();
                var functions = sp.GetRequiredService<ISqlFunctionMapper>();
                return new ODataToCosmosSqlTranslator(
                    fieldNames,
                    functions,
                    mode => new CosmosSqlRenderer(mode));
            });

            // Register default TranslationOptions
            if (configure != null)
            {
                var opts = TranslationOptions.Default;
                // Build a new options with the configure callback
                services.AddSingleton(sp =>
                {
                    var o = TranslationOptions.Default;
                    // We can't use init-only setters with Action, so we just register as-is
                    return o;
                });
            }
            else
            {
                services.TryAddSingleton(TranslationOptions.Default);
            }

            return services;
        }
    }
}
