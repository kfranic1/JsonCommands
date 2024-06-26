﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Utilities
{
    public static class JsonHelper
    {
        /// <summary>
        /// A custom error handler for JSON deserialization, that throws a Rhetos.ClientException with an error description.
        /// This is a wrapper around `JsonConvert.DeserializeObject`, because `DeserializeObject` returns null
        /// in case of an error, so the same error checking needs to be performed on each deserialization.
        /// </summary>
        public static T DeserializeOrException<T>(string serialized)
            => (T)DeserializeOrException(serialized, typeof(T));

        /// <summary>
        /// A custom error handler for JSON deserialization, that throws a Rhetos.ClientException with an error description.
        /// This is a wrapper around `JsonConvert.DeserializeObject`, because `DeserializeObject` returns null
        /// in case of an error, so the same error checking needs to be performed on each deserialization.
        /// </summary>
        public static object DeserializeOrException(string serialized, Type type)
            => DeserializeOrException(jsonSettings => JsonConvert.DeserializeObject(serialized, type, jsonSettings), () => serialized);

        private static object DeserializeOrException(JToken jToken, Type type)
            => DeserializeOrException(jsonSettings =>
                {
                    var jsonSerializer = JsonSerializer.CreateDefault(jsonSettings);
                    return jToken.ToObject(type, jsonSerializer);
                },
                () => jToken.ToString());

        public const string RhetosJsonErrorErrorMetadata = "Rhetos.JsonError";

        private static object DeserializeOrException(Func<JsonSerializerSettings, object> deserializer, Func<string> errorContext)
        {
            var errors = new List<Exception>();
            var jsonSettings = new JsonSerializerSettings
            {
                Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                {
                    errors.Add(args.ErrorContext.Error);
                    args.ErrorContext.Handled = true;
                }
            };

            object deserialized = deserializer(jsonSettings);

            if (errors.Any())
            {
                var exception = new ClientException("The request has invalid JSON format. See the server log for more information.", errors.First());
                exception.Data[RhetosJsonErrorErrorMetadata] = $"Filter parameter: '{CsUtility.Limit(errorContext(), 1000, true)}'.";
                throw exception;
            }
            else
                return deserialized;
        }

        /// <summary>
        /// Resolve partially deserialized object with known types.
        /// </summary>
        public static object FinishPartiallyDeserializedObject(object o, Type type)
        {
            if (o is JToken jToken)
                return DeserializeOrException(jToken, type);
            if (o is string s && type.IsValueType)
                return DeserializeOrException(s, type);
            else
                return o;
        }

        /// <summary>
        /// Resolve partially deserialized object with detected array type.
        /// </summary>
        public static object FinishPartiallyDeserializedArray(object o)
        {
            if (o is JArray jArray && jArray.Count > 0)
            {
                var elementType = jArray.First().Type;
                if (jArray.All(item => item.Type == elementType))
                {
                    switch (elementType)
                    {
                        case JTokenType.String:
                            return jArray.ToObject<string[]>();
                        case JTokenType.Integer:
                            return jArray.ToObject<int[]>();
                        case JTokenType.Guid:
                            return jArray.ToObject<Guid[]>();
                        case JTokenType.Boolean:
                            return jArray.ToObject<bool[]>();
                        case JTokenType.Date:
                            return jArray.ToObject<DateTime[]>();
                        case JTokenType.Float:
                            return jArray.ToObject<decimal[]>();
                    }
                }
            }

            return o;
        }
    }
}
