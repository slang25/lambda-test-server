﻿// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace MartinCostello.Testing.AwsLambdaTestServer
{
    /// <summary>
    /// Examples for using <c>MartinCostello.Testing.AwsLambdaTestServer</c>.
    /// </summary>
    public static class Examples
    {
        [Fact]
        public static async Task Function_Can_Process_Request()
        {
            // Arrange - Create a test server for the Lambda runtime to use
            using var server = new LambdaTestServer();

            // Create a cancellation token that stops the server listening for new requests.
            // Auto-cancel the server after 2 seconds in case something goes wrong and the request is not handled.
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Start the test server so it is ready to listen for requests from the Lambda runtime
            await server.StartAsync(cancellationTokenSource.Token);

            // Create a test request for the Lambda function being tested
            var value = new MyRequest()
            {
                Values = new[] { 1, 2, 3 }, // The function returns the sum of the specified numbers
            };

            // Queue the request with the server to invoke the Lambda function and
            // store the ChannelReader into a variable to use to read the response.
            ChannelReader<LambdaTestResponse> reader = await server.EnqueueAsync(value);

            // Queue a task to stop the test server from listening as soon as the response is available
            _ = Task.Run(async () =>
            {
                await reader.WaitToReadAsync(cancellationTokenSource.Token);

                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
            });

            // Create an HttpClient for the Lambda to use with LambdaBootstrap
            using var httpClient = server.CreateClient();

            // Act - Start the Lambda runtime and run until the cancellation token is signalled
            await MyFunctionEntrypoint.RunAsync(httpClient, cancellationTokenSource.Token);

            // Assert - The channel reader should have the response available
            reader.TryRead(out LambdaTestResponse response).ShouldBeTrue("No Lambda response is available.");

            response.ShouldNotBeNull("The Lambda response is null.");
            response.IsSuccessful.ShouldBeTrue("The Lambda function failed to handle the request.");
            response.Content.ShouldNotBeNull("The Lambda function did not return any content.");

            string json = Encoding.UTF8.GetString(response.Content);
            var actual = JsonConvert.DeserializeObject<MyResponse>(json);

            actual.Sum.ShouldBe(6, "The Lambda function returned an incorrect response.");
        }

        private static async Task<ChannelReader<LambdaTestResponse>> EnqueueAsync<T>(this LambdaTestServer server, T value)
            where T : class
        {
            string json = JsonConvert.SerializeObject(value);
            return await server.EnqueueAsync(json);
        }
    }
}
