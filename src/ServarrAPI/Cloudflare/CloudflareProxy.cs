using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServarrAPI.Cloudflare
{
    public interface ICloudflareProxy
    {
        Task InvalidateBranches(IEnumerable<string> branches);
    }

    public class CloudflareProxy : ICloudflareProxy
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly ILogger _logger;
        private readonly CloudflareOptions _config;

        private readonly HttpClient _httpClient;

        public CloudflareProxy(IConfiguration config,
                               ILogger<CloudflareProxy> logger)
        {
            _logger = logger;
            _config = new CloudflareOptions();
            config.GetSection("Cloudflare").Bind(_config);

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                SslProtocols = SslProtocols.Tls12
            };

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Email", _config.Email);
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Key", _config.Key);
        }

        public async Task InvalidateBranches(IEnumerable<string> branches)
        {
            var page = 0;
            var paged = GetPage(branches, page);

            while (paged.Any())
            {
                _logger.LogTrace($"Invalidating page {page} with {paged.Count()} ids");
                var files = paged
                    .SelectMany(x => new[]
                                {
                                    $"{_config.BaseUrl}/update/{x}",
                                    $"{_config.BaseUrl}/update/{x}/changes",
                                    $"{_config.BaseUrl}/update/{x}/updatefile"
                                })
                    .ToList();

                var payload = new CloudflareInvalidationRequest
                {
                    Files = files
                };

                _logger.LogTrace($"Sending to cloudflare:\n{JsonSerializer.Serialize(payload, JsonOptions)}");

                var message = GetInvalidationMessage(payload);
                var retries = 2;

                while (retries > 0)
                {
                    try
                    {
                        // Can't send same message twice
                        message = GetInvalidationMessage(payload);
                        var result = await _httpClient.SendAsync(message).ConfigureAwait(false);
                        var content = await result.Content.ReadAsStringAsync();
                        var resource = JsonSerializer.Deserialize<CloudflareResponse>(content, JsonOptions);

                        if (resource.Success)
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Invalidation failed");
                    }

                    _logger.LogTrace($"Invalidation failed, retrying");

                    retries--;
                }

                paged = GetPage(branches, ++page);
            }
        }

        private IEnumerable<string> GetPage(IEnumerable<string> items, int page)
        {
            return items.Skip(page * 10).Take(10);
        }

        private HttpRequestMessage GetInvalidationMessage(CloudflareInvalidationRequest payload)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            return new HttpRequestMessage(HttpMethod.Post, $"https://api.cloudflare.com/client/v4/zones/{_config.ZoneID}/purge_cache")
            {
                Content = body
            };
        }
    }

    public class CloudflareInvalidationRequest
    {
        public IEnumerable<string> Files { get; set; }
    }

    public class CloudflareResponse
    {
        public bool Success { get; set; }
    }
}
