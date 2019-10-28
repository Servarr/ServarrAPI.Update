using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace LidarrAPI.Release.Azure
{
    public class Manifest
    {
        [JsonPropertyName("items")]
        public List<AzureFile> Files { get; set; }
    }

    public class AzureFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("blob")]
        public Blob Blob { get; set; }

        public string Url { get; set; }
    }

    public class Blob
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class ArtifactHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _uri;

        public ArtifactHttpClient(Uri uri)
        {
            _uri = uri;
            _httpClient = new HttpClient();
        }

        private Uri GetFileUri(string project, int buildId, string artifactName, string fileId, string fileName)
        {
            return new Uri(_uri + $"/{project}/_apis/build/builds/{buildId}/artifacts?artifactName={artifactName}&fileId={fileId}&fileName={fileName}&api-version=5.1");
        }

        public async Task<List<AzureFile>> GetArtifactFiles(string project, int buildId, BuildArtifact artifact)
        {
            var request = GetFileUri(project, buildId, artifact.Name, artifact.Resource.Data, "manifest");
            var data = await _httpClient.GetStringAsync(request);
            var files = JsonSerializer.Deserialize<Manifest>(data).Files;

            foreach (var file in files)
            {
                file.Url = GetFileUri(project, buildId, artifact.Name, file.Blob.Id, Path.GetFileName(file.Path)).AbsoluteUri;
            }

            return files;
        }
    }

    public static class AzureExtensions
    {
        public static ArtifactHttpClient GetClient<T>(this VssConnection connection) where T : ArtifactHttpClient
        {
            return new ArtifactHttpClient(connection.Uri);
        }
    }
}
