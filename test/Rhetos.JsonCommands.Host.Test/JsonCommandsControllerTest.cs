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

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.JsonCommands.Host.Test.Tools;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestApp;
using Xunit;

namespace Rhetos.JsonCommands.Host.Test
{
    public class JsonCommandsControllerTests : IDisposable, IClassFixture<JsonCommandsTestCleanup>
    {
        private readonly WebApplicationFactory<Startup> _factory;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly JsonCommandsTestCleanup _cleanup; // The 'cleanup' instance is used for its constructor and Dispose method for setup and teardown logic. It is injected by xUnit and not directly used in the test methods.
#pragma warning restore IDE0052 // Remove unread private members

        public JsonCommandsControllerTests(JsonCommandsTestCleanup cleanup)
        {
            _factory = new CustomWebApplicationFactory<Startup>();
            _cleanup = cleanup;
        }

        public void Dispose()
        {
            _factory.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task BatchJsonCommands()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IRhetosComponent<Common.DomRepository>>().Value;

                var client = SetupClient();

                Guid guid = Guid.NewGuid();
                Guid guid2 = Guid.NewGuid();
                Guid guid3 = Guid.NewGuid();

                string insertJson = $@"
                [
                  {{
                    ""Bookstore.Book"": {{
                      ""Insert"": [
                        {{ ""ID"": ""{guid}"", ""Name"": ""__Test__This is a test book"" }},
                        {{ ""ID"": ""{guid2}"", ""Name"": ""__Test__This is the second test book"" }}
                      ]
                    }}
                  }}
                ]";

                string updateJson = $@"
                [
                  {{
                    ""Bookstore.Book"": {{
                      ""Delete"": [
                        {{ ""ID"": ""{guid}"" }}
                      ],
                      ""Update"": [
                        {{ ""ID"": ""{guid2}"", ""Name"": ""__Test__Updated name"" }}
                      ],
                      ""Insert"": [
                        {{ ""ID"": ""{guid3}"", ""Name"": ""__Test__This is a another book"" }}
                      ]
                    }}
                  }}
                ]";

                string deleteJson = $@"
                [
                  {{
                    ""Bookstore.Book"": {{
                      ""Delete"": [
                        {{ ""ID"": ""{guid2}"" }},
                        {{ ""ID"": ""{guid3}"" }}
                      ],
                    }}
                  }}
                ]";

                Guid[] guids = new[] { guid, guid2, guid3 };

                var response = await Execute(insertJson, client);
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(2, repository.Bookstore.Book.Query(guids).Count());

                await Execute(updateJson, client);
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(2, repository.Bookstore.Book.Query(guids).Count());

                await Execute(deleteJson, client);
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(0, repository.Bookstore.Book.Query(guids).Count());
            }
        }

        [Fact]
        public async Task FailDoubleInsert()
        {
            var client = SetupClient();

            Guid guid = Guid.NewGuid();

            string insertJson = $@"
            [
              {{
                ""Bookstore.Book"": {{
                  ""Insert"": [
                    {{ ""ID"": ""{guid}"", ""Name"": ""__Test__This is a test book"" }},
                    {{ ""ID"": ""{guid}"", ""Name"": ""__Test__This is the second test book"" }}
                  ]
                }}
              }}
            ]";

            var response = await Execute(insertJson, client);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.False(response.IsSuccessStatusCode);
            Assert.StartsWith("{\"UserMessage\":\"Operation could not be completed because the request sent to the server was not valid or not properly formatted.\""
                    + ",\"SystemMessage\":\"Inserting a record that already exists in database.", responseContent);
        }

        private HttpClient SetupClient(bool legacyErrorResponse = true)
        {
            var logEntries = new LogEntries();
            return _factory
                .WithWebHostBuilder(builder => builder.MonitorLogging(logEntries).UseLegacyErrorResponse(legacyErrorResponse))
                .CreateClient();
        }

        private async Task<HttpResponseMessage> Execute(string json, HttpClient client)
        {
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return await client.PostAsync("jc/write", content);
        }
    }
}
