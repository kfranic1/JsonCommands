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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rhetos.JsonCommands.Host.Utilities;
using System;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Filters
{
    /// <summary>
    /// Standard Rhetos JsonCommands API error response format:
    /// In case of an exception, the controller will return a JSON response body with the "Error" property.
    /// The property will contain text "Message" and optionally the "Metadata" dictionary with additional details on the error.
    /// For example, on some errors, the metadata will contain the "SystemMessage" text.
    /// If option <see cref="JsonCommandsOptions.UseLegacyErrorResponse"/> is enabled, the error response will be
    /// a JSON object with "UserMessage" and "SystemMessage" properties instead.
    /// </summary>
    /// <remarks>
    /// It also writes the exception to the application's log, based on severity:
    /// <see cref="UserException"/> is logged as Trace level (not logged by default), because it is expected during the standard app usage
    /// (for example, user forgot to enter a required field).
    /// <see cref="ClientException"/> is logged as Information level (logged by default), because it indicates that
    /// the client application needs to be corrected.
    /// Other exceptions are logged as Error level, because they represent internal error in the server application
    /// that needs to be fixed.
    /// </remarks>
    public class ApiExceptionFilter : IActionFilter, IOrderedFilter
    {
        private readonly IOptions<JsonCommandsOptions> options;
        private readonly ErrorReporting jsonErrorHandler;
        private readonly ILogger logger;

        public int Order { get; } = int.MaxValue - 10;

        public ApiExceptionFilter(IOptions<JsonCommandsOptions> options, ErrorReporting jsonErrorHandler, ILogger<ApiExceptionFilter> logger)
        {
            this.options = options;
            this.jsonErrorHandler = jsonErrorHandler;
            this.logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var invalidModelEntries = context.ModelState
                .Where(a => a.Value.ValidationState == ModelValidationState.Invalid)
                .ToList();

            if (!invalidModelEntries.Any())
                return;

            var invalidModelEntry = invalidModelEntries.First();
            var errors = string.Join("\n", invalidModelEntry.Value.Errors.Select(a => a.ErrorMessage));

            string systemMessage;

            // If no key is present, it means there is an error deserializing body.
            if (string.IsNullOrEmpty(invalidModelEntry.Key))
            {
                systemMessage = "Serialization error: Please check if the request body has a valid JSON format.\n" + errors;
            }
            else
            {
                systemMessage = $"Parameter error: Supplied value for parameter '{invalidModelEntry.Key}' couldn't be parsed.\n" + errors;
            }

            object responseMessage = ErrorReporting.CreateErrorResponseMessage(null, systemMessage, options.Value.UseLegacyErrorResponse);
            
            context.Result = new JsonResult(responseMessage) { StatusCode = StatusCodes.Status400BadRequest };
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                var error = jsonErrorHandler.CreateResponseFromException(context.Exception, options.Value.UseLegacyErrorResponse);

                context.Result = new JsonResult(error.Response) { StatusCode = error.HttpStatusCode };
                context.ExceptionHandled = true;

                logger.Log(error.Severity, error.LogMessage);
            }
        }
    }
}
