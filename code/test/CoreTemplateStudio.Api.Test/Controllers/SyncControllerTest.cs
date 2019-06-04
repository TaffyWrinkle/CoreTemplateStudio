﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CoreTemplateStudio.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Templates.Api.Test.ResponseModels;
using Microsoft.Templates.Api.Test.Utilities;
using Microsoft.Templates.Core.Locations;
using Newtonsoft.Json;

using Xunit;

namespace Microsoft.Templates.Api.Test.Controllers
{
    [Trait("ExecutionSet", "Minimum")]
    public class SyncControllerTest : IDisposable
    {
        private TestServer testServer;

        [Fact]
        public async void TestSync_ShouldBeSuccessResponse()
        {
            const string ValidPath = ".";
            var message = SyncStatus.None;
            var progress = 0;

            var connection = CreateConnection();

            connection.On<SyncStatus, int>("syncMessage", (msg, prg) =>
            {
                message = msg;
                progress = prg;
            });

            await connection.StartAsync();
            await connection.InvokeAsync("SyncTemplates", ValidPath);
            await connection.StopAsync();

            message.Should().Be(SyncStatus.Updated, "Sync has finished with success");
        }

        [Fact]
        public async void TestSync_InvalidPath()
        {
            const string InValidPath = @"..\wrongpath\";
            var errrorMessage = string.Empty;

            var connection = CreateConnection();

            await connection.StartAsync();
            try
            {
                await connection.InvokeAsync("SyncTemplates", InValidPath);
            }
            catch (Exception ex)
            {
                errrorMessage = ex.Message;
            }

            await connection.StopAsync();

            errrorMessage.Should().Be("An unexpected error occurred invoking 'SyncTemplates' on the server. HubException: invalid path", "Sync has finished with success");
        }

        private HubConnection CreateConnection()
        {
            testServer = new TestServer(new WebHostBuilder().UseStartup<Startup>());

            return new HubConnectionBuilder()
                .WithUrl(
                    "http://localhost:5000/corehub",
                    o => o.HttpMessageHandlerFactory = _ => testServer.CreateHandler())
                .Build();
        }

        private async Task<ApiResponse> GetResponseFromUrlPost(HttpClient client, string url)
        {
            HttpResponseMessage httpResponse = await client.PostAsync(url, null);
            string content = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ApiResponse>(content);
        }

        public void Dispose()
        {
            testServer?.Dispose();
        }
    }
}
