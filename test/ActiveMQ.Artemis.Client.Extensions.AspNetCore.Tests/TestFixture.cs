using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.Tests
{
    public class TestFixture : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly CancellationTokenSource _cts;

        private TestFixture(IHost host, IConnection connection, CancellationTokenSource cts)
        {
            _host = host;
            Connection = connection;
            _cts = cts;
        }

        public static async Task<TestFixture> CreateAsync(Action<IActiveMqBuilder> configureActiveMq = null, Action<IServiceCollection> configureServices = null)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var endpoints = new[] { Endpoint.Create(host: "localhost", port: 5672, "guest", "guest") };
            var host = new HostBuilder()
                       .ConfigureWebHost(webBuilder =>
                       {
                           webBuilder
                               .ConfigureServices(services =>
                               {
                                   services.AddSingleton<IServer>(serviceProvider => new TestServer(serviceProvider));
                                   configureServices?.Invoke(services);
                                   configureActiveMq?.Invoke(services.AddActiveMq("my-test-artemis", endpoints));
                               })
                               .Configure(app => { });
                       })
                       .Build();
            await host.StartAsync(cts.Token);

            var connectionFactory = new ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(endpoints, cts.Token);

            return new TestFixture(host, connection, cts);
        }

        public IServiceProvider Services => _host.Services;
        public IConnection Connection { get; }
        public CancellationToken CancellationToken => _cts.Token;

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            await _host.StopAsync(_cts.Token);
            _cts.Dispose();
            _host.Dispose();
        }
    }
}