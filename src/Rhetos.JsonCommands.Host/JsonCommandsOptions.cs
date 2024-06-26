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

namespace Rhetos.JsonCommands.Host
{
    /// <summary>
    /// Options class for Rhetos JsonCommands API.
    /// </summary>
    /// <remarks>
    /// It is intended by be configured in Program.cs or Startup.cs, by a delegate parameter of <see cref="RhetosJsonCommandsServiceCollectionExtensions.AddJsonCommands"/> method call.
    /// </remarks>
    public class JsonCommandsOptions
    {
        /// <summary>
        /// Flag for switching between new error response and legacy error response.
        /// <para>
        /// New error response format:
        /// { "Error": { "Message": "...", "Metadata": { "DataStructure": "Bookstore.Book", "Property": "Title", "ErrorCode": ..., ... } } }
        /// </para>
        /// <para>
        /// Legacy error response:
        /// { "UserMessage": "...", "SystemMessage": "..." }
        /// </para>
        /// </summary>
        public bool UseLegacyErrorResponse { get; set; } = false;
    }
}
