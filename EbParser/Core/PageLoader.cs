﻿using CloudflareSolverRe;
using EbParser.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class PageLoader : ILoader, IDisposable
    {
        #region Fields

        private readonly ClearanceHandler _clearanceHandler;
        private readonly HttpClient _httpClient;

        #endregion

        #region Life

        public PageLoader()
        {
            _clearanceHandler = new ClearanceHandler
            {
                MaxTries = 3,
                ClearanceDelay = 3000
            };

            _httpClient = new HttpClient(_clearanceHandler, true);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #endregion

        #region IPageLoader implentation

        public async Task<string> LoadPageAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException(nameof(url));
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, CancellationToken.None);

            string result;
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new HttpRequestException($"Status Code: { response.StatusCode }");
            }

            return result;
        }

        public async Task<bool> LoadFileAsync(string url, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException(nameof(url));
            }

            var result = false;
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, CancellationToken.None);
            if (response.IsSuccessStatusCode)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fsStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                await contentStream.CopyToAsync(fsStream);
                result = true;
            }
            else
            {
                throw new HttpRequestException(response.StatusCode.ToString());
            }

            return result;
        }

        #endregion
    }
}