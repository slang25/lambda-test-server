﻿// Copyright (c) Martin Costello, 2019. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace MartinCostello.Testing.AwsLambdaTestServer
{
    internal class MyHandler
    {
        public virtual Task<bool> InitializeAsync()
        {
            return Task.FromResult(true);
        }

        public Task<MyResponse> SumAsync(MyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine($"Handling AWS request Id {context.AwsRequestId}.");

            var response = new MyResponse()
            {
                Sum = request.Values.Sum(),
            };

            context.Logger.LogLine($"The sum of the {request.Values?.Count} values is {response.Sum}.");

            return Task.FromResult(response);
        }
    }
}
