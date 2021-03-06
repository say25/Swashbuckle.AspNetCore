﻿using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.AspNetCore.Newtonsoft
{
    public class NewtonsoftDictionaryHandler : SchemaGeneratorHandler
    {
        private readonly IContractResolver _contractResolver;
        private readonly ISchemaGenerator _schemaGenerator;

        public NewtonsoftDictionaryHandler(IContractResolver contractResolver, ISchemaGenerator schemaGenerator)
        {
            _contractResolver = contractResolver;
            _schemaGenerator = schemaGenerator;
        }

        public override bool CanCreateSchemaFor(Type type, out bool shouldBeReferenced)
        {
            if (_contractResolver.ResolveContract(type) is JsonDictionaryContract jsonDictionaryContract)
            {
                shouldBeReferenced = (jsonDictionaryContract.DictionaryValueType == type); // to avoid circular references
                return true;
            }

            shouldBeReferenced = false; return false;
        }

        public override OpenApiSchema CreateDefinitionSchema(Type type, SchemaRepository schemaRepository)
        {
            if (!(_contractResolver.ResolveContract(type) is JsonDictionaryContract jsonDictionaryContract))
               throw new InvalidOperationException($"Type {type} does not resolve to a JsonDictionaryContract");

            var keyType = jsonDictionaryContract.DictionaryKeyType ?? typeof(object);
            var valueType = jsonDictionaryContract.DictionaryValueType ?? typeof(object);

            if (keyType.IsEnum)
            {
                // This is a special case where we can include named properties based on the enum values
                return new OpenApiSchema
                {
                    Type = "object",
                    Properties = keyType.GetEnumNames()
                        .ToDictionary(
                            name => name,
                            name => _schemaGenerator.GenerateSchema(valueType, schemaRepository)
                        ),
                    Nullable = true
                };
            }

            return new OpenApiSchema
            {
                Type = "object",
                AdditionalPropertiesAllowed = true,
                AdditionalProperties = _schemaGenerator.GenerateSchema(valueType, schemaRepository),
                Nullable = true
            };
        }
    }
}