﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExamplesCommonCode.DemoSetupCode
{
    public class SetupAspNetCoreUsers : IHostedService
    {
        private const string SeedDataDir = "SeedData";
        private const string UsersFilename = "Users.json";

        private readonly IServiceProvider _serviceProvider;

        public SetupAspNetCoreUsers(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a new scope to retrieve scoped services
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var env = services.GetRequiredService<IWebHostEnvironment>();

            //Get the demo data from files in the WebRootPath
            var pathUserJson = Path.GetFullPath(Path.Combine(env.WebRootPath, SeedDataDir, UsersFilename));
            var userJson = File.ReadAllText(pathUserJson);
            var seedUsers = JsonSerializer.Deserialize<List<UserJson>>(userJson);

            var demoData = services.GetRequiredService<IOptions<DemoSetup>>();
            var aspNetUsers = await services.AddDemoUsersFromJson(seedUsers, demoData.Value.AddRolesToAspNetUser);
        }

        // noop
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}