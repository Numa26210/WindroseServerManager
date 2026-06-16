using System;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WindroseServerManager.Core.Models;
using WindroseServerManager.Core.Services;
using Xunit;

namespace WindroseServerManager.Core.Tests.Phase17;

public class WindrosePlusApiServiceHostTests
{
    private sealed class FakeSettings : IAppSettingsService
    {
        public AppSettings Current { get; } = new();
        public string ActiveServerDir => Current.ServerInstallDir;
        public event Action<AppSettings>? Changed;
        public Task SelectServerAsync(string id) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Action<AppSettings> mutate, CancellationToken ct = default)
        {
            mutate(Current);
            Changed?.Invoke(Current);
            return Task.CompletedTask;
        }
    }

    private sealed class NoopHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private static WindrosePlusApiService CreateService(IAppSettingsService settings)
    {
        var factory = new WindroseServerManager.Core.Tests.TestDoubles.FakeHttpClientFactory(new NoopHandler());
        return new WindrosePlusApiService(factory, settings, NullLogger<WindrosePlusApiService>.Instance);
    }

    [Fact]
    public void GetHost_NoHostConfigured_ReturnsLocalhost()
    {
        var settings = new FakeSettings();
        settings.Current.WindrosePlusDashboardPortByServer["C:\\server"] = 8780;
        var svc = CreateService(settings);

        var baseUrl = GetBaseUrl(svc, "C:\\server");

        Assert.Contains("localhost", baseUrl);
    }

    [Fact]
    public void GetHost_CustomHostConfigured_ReturnsCustomHost()
    {
        var settings = new FakeSettings();
        settings.Current.WindrosePlusHostByServer["C:\\server"] = "192.168.1.50";
        settings.Current.WindrosePlusDashboardPortByServer["C:\\server"] = 8780;
        var svc = CreateService(settings);

        var baseUrl = GetBaseUrl(svc, "C:\\server");

        Assert.Contains("192.168.1.50", baseUrl);
    }

    [Fact]
    public void GetBaseUrl_PortSet_IncludesPort()
    {
        var settings = new FakeSettings();
        settings.Current.WindrosePlusDashboardPortByServer["C:\\server"] = 9000;
        var svc = CreateService(settings);

        var baseUrl = GetBaseUrl(svc, "C:\\server");

        Assert.Equal("http://localhost:9000", baseUrl);
    }

    [Fact]
    public void GetBaseUrl_PortZero_OmitsPort()
    {
        var settings = new FakeSettings();
        settings.Current.WindrosePlusDashboardPortByServer["C:\\server"] = 0;
        var svc = CreateService(settings);

        var baseUrl = GetBaseUrl(svc, "C:\\server");

        Assert.Equal("http://localhost", baseUrl);
    }

    [Fact]
    public void GetBaseUrl_CustomHostAndPort_ReturnsFullUrl()
    {
        var settings = new FakeSettings();
        settings.Current.WindrosePlusHostByServer["C:\\server"] = "my-host.example.com";
        settings.Current.WindrosePlusDashboardPortByServer["C:\\server"] = 8780;
        var svc = CreateService(settings);

        var baseUrl = GetBaseUrl(svc, "C:\\server");

        Assert.Equal("http://my-host.example.com:8780", baseUrl);
    }

    [Fact]
    public void GetHost_NormalizedPath_Matches()
    {
        var settings = new FakeSettings();
        settings.Current.WindrosePlusHostByServer["C:\\servers\\my-server"] = "10.0.0.1";
        var svc = CreateService(settings);

        var baseUrl = GetBaseUrl(svc, "C:\\servers\\my-server\\");

        Assert.Contains("10.0.0.1", baseUrl);
    }

    private static string GetBaseUrl(WindrosePlusApiService svc, string serverDir)
    {
        return svc.GetType()
            .GetMethod("GetBaseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(svc, [serverDir]) as string ?? "";
    }
}
