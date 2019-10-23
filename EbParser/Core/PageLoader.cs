using EbParser.Interfaces;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EbParser.Core
{
    class PageLoader : IPageLoader
    {
        public async Task<string> LoadPageAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException(nameof(url));
            }

            string result = null;
            try
            {
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await client.SendAsync(request, CancellationToken.None);
                //if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                }
                //else
                //{
                //    HandleException(new Exception($"Status Code: {response.StatusCode}"));
                //}
            }
            catch (HttpRequestException httpex)
            {
                HandleException(httpex);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return result;
        }

        private void HandleException(Exception ex, [CallerMemberName]string src = "N/A")
        {
            Debug.WriteLine($"{src}: {ex.Message}");
            throw ex;
        }
    }
}