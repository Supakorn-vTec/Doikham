using Doikham.Shared.Database;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Doikham.API.Data
{
    public class SapResponse
    {
        public JObject Json { get; set; }
        public string StatusCode { get; set; } = "E";
        public string Message { get; set; }
        public string DocNo { get; set; }
    }

    public interface ISAP
    {
        public Task<HttpResponseMessage> Post(string aciton, string data);
        public Task<DataTable> ConvertResponseToDataTable(HttpResponseMessage response);
        public Task<JObject> ParseResponseJson(HttpResponseMessage response);
        public Task<SapResponse> ParseSapResponse(HttpResponseMessage response);
        public string FormatODataPayload(string data);
    }

    public class SAP : ISAP
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _client;
        private readonly CookieContainer _cookieContainer;
        private readonly string oDataHost;
        private readonly string sapClient;
        private readonly Dictionary<string, string> endpoints;
        private readonly string configuredCsrfToken;
        CultureInfo invC;
        private IDBHelper _dbHelper;
        private string connString = "";

        public SAP(IConfiguration configuration, IDBHelper dBHelper)
        {
            _config = configuration;
            _dbHelper = dBHelper;
            connString = _config.GetSection("Database")["ConnectionString"];
            invC = new CultureInfo("en-US");

            oDataHost = (_config.GetSection("SAP")["ODataHost"] ?? "https://sapdev.deksomboon.com").TrimEnd('/');
            sapClient = _config.GetSection("SAP")["SapClient"] ?? "200";

            var username = _config.GetSection("SAP")["Username"] ?? "";
            var password = _config.GetSection("SAP")["Password"] ?? "";
            var apiKey = _config.GetSection("SAP")["ApiKey"];
            var authValue = !string.IsNullOrEmpty(username)
                ? Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"))
                : apiKey;

            endpoints = _config.GetSection("SAP:Endpoints").Get<Dictionary<string, string>>()
                ?? GetDefaultEndpoints();
            configuredCsrfToken = _config.GetSection("SAP")["CsrfToken"];

            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                // Decompress manually — .NET Core 3.1 handler does not support Brotli (br)
                AutomaticDecompression = DecompressionMethods.None
            };
            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(360)
            };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.TryAddWithoutValidation("X-REQUESTED-WITH", "X");
            // Do not advertise br; SAP may still respond with br — we decompress manually
            _client.DefaultRequestHeaders.AcceptEncoding.Clear();
        }

        private static string GetServiceRootPath(string entityPath)
        {
            var normalized = entityPath.TrimEnd('/');
            var lastSlash = normalized.LastIndexOf('/');
            return lastSlash > 0 ? normalized.Substring(0, lastSlash + 1) : normalized + "/";
        }

        private static Dictionary<string, string> GetDefaultEndpoints()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GOODSISSUE"] = "/sap/opu/odata/sap/zsd_pos_gi_v2/GOODS_ISSUE_IN",
                ["STOCK_ADJUST"] = "/sap/opu/odata/sap/zsd_pos_adjust_v2/STOCK_ADJUST",
                ["GOODSRECEIPT"] = "/sap/opu/odata/sap/zsd_pos_gr_v2/GOODS_RECEIPT",
                ["REQUESTFORM"] = "/sap/opu/odata/sap/zsd_pos_rf_v2/REQUEST_FORM",
                ["RECIPES"] = "/sap/opu/odata/sap/zsd_pos_recipe_v2/RECIPES"
            };
        }

        public async Task<HttpResponseMessage> Post(string action, string data)
        {
            if (!endpoints.TryGetValue(action, out var entityPath))
            {
                throw new Exception($"SAP OData endpoint not configured for action: {action}");
            }

            var postUrl = BuildEntityUrl(entityPath);
            data = FormatODataPayload(data);

            var csrfToken = await FetchCsrfTokenAsync(entityPath);
            var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
            request.Headers.TryAddWithoutValidation("x-csrf-token", csrfToken);
            request.Headers.TryAddWithoutValidation("X-REQUESTED-WITH", "X");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");

            return await _client.SendAsync(request);
        }

        private string BuildEntityUrl(string entityPath)
        {
            return $"{oDataHost}{entityPath}?sap-client={sapClient}";
        }

        private async Task<string> FetchCsrfTokenAsync(string entityPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredCsrfToken))
            {
                return configuredCsrfToken;
            }

            var entityUrl = BuildEntityUrl(entityPath);
            var serviceRootUrl = BuildEntityUrl(GetServiceRootPath(entityPath));

            // Establish SAP session cookie before CSRF fetch (2-step like Postman)
            await SendSessionRequestAsync(entityUrl, fetchCsrf: false);
            await SendSessionRequestAsync(serviceRootUrl, fetchCsrf: false);

            var fetchUrls = new[] { entityUrl, serviceRootUrl };
            Exception lastError = null;

            for (int round = 0; round < 3; round++)
            {
                foreach (var fetchUrl in fetchUrls)
                {
                    foreach (var method in new[] { HttpMethod.Get, new HttpMethod("HEAD") })
                    {
                        try
                        {
                            var response = await SendSessionRequestAsync(fetchUrl, fetchCsrf: true, method: method);
                            if (TryGetCsrfToken(response, out var token, out var required))
                            {
                                return token;
                            }

                            if (required)
                            {
                                continue;
                            }

                            if (method == HttpMethod.Get)
                            {
                                var body = await ReadResponseContentAsync(response);
                                lastError = new Exception($"SAP CSRF token missing (HTTP {(int)response.StatusCode} {fetchUrl}): {body.Substring(0, Math.Min(120, body.Length))}");
                            }
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;
                        }
                    }
                }
            }

            // Some SAP gateways accept Fetch on POST when session cookie already exists
            return "Fetch";
        }

        private async Task<HttpResponseMessage> SendSessionRequestAsync(string url, bool fetchCsrf, HttpMethod method = null)
        {
            method = method ?? HttpMethod.Get;
            var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("X-REQUESTED-WITH", "X");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (fetchCsrf)
            {
                request.Headers.TryAddWithoutValidation("x-csrf-token", "Fetch");
            }

            var response = await _client.SendAsync(request);
            if (method == HttpMethod.Get)
            {
                await ReadResponseContentAsync(response);
            }

            return response;
        }

        private static async Task<string> ReadResponseContentAsync(HttpResponseMessage response)
        {
            var encoding = response.Content.Headers.ContentEncoding
                .FirstOrDefault()?.Trim().ToLowerInvariant();

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var decoded = OpenDecodedStream(stream, encoding))
            using (var reader = new StreamReader(decoded, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static Stream OpenDecodedStream(Stream stream, string encoding)
        {
            switch (encoding)
            {
                case "gzip":
                    return new GZipStream(stream, CompressionMode.Decompress);
                case "deflate":
                    return new DeflateStream(stream, CompressionMode.Decompress);
                case "br":
                    return new BrotliStream(stream, CompressionMode.Decompress);
                default:
                    return stream;
            }
        }

        private static bool TryGetCsrfToken(HttpResponseMessage response, out string token, out bool required)
        {
            token = null;
            required = false;

            foreach (var header in response.Headers)
            {
                if (header.Key.IndexOf("csrf", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                token = header.Value.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (token.Equals("Required", StringComparison.OrdinalIgnoreCase))
                {
                    required = true;
                    return false;
                }

                return true;
            }

            return false;
        }

        public string FormatODataPayload(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return data;
            }

            var trimmed = data.TrimStart();
            if (!trimmed.StartsWith("{"))
            {
                return data;
            }

            var obj = JObject.Parse(data);
            WrapArrayPropertyForOData(obj, "toITEMS");
            WrapArrayPropertyForOData(obj, "ITEMS");
            return obj.ToString(Formatting.None);
        }

        private static void WrapArrayPropertyForOData(JObject obj, string propertyName)
        {
            var token = obj[propertyName];
            if (token != null && token.Type == JTokenType.Array)
            {
                obj[propertyName] = new JObject { ["results"] = token };
            }
        }

        public async Task<JObject> ParseResponseJson(HttpResponseMessage response)
        {
            var resMsg = await ReadResponseContentAsync(response);
            if (string.IsNullOrWhiteSpace(resMsg))
            {
                throw new Exception($"SAP returned empty response (HTTP {(int)response.StatusCode} {response.ReasonPhrase})");
            }

            var trimmed = resMsg.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                throw new Exception($"SAP returned non-JSON response (HTTP {(int)response.StatusCode}): {trimmed.Substring(0, Math.Min(300, trimmed.Length))}");
            }

            return JObject.Parse(trimmed);
        }

        public async Task<SapResponse> ParseSapResponse(HttpResponseMessage response)
        {
            var json = await ParseResponseJson(response);
            var status = ExtractStatus(json, (int)response.StatusCode);
            return new SapResponse
            {
                Json = json,
                StatusCode = status.Type,
                Message = status.Message,
                DocNo = status.DocNo
            };
        }

        private static (string Type, string Message, string DocNo) ExtractStatus(JObject json, int httpStatus)
        {
            JObject statusObj = null;
            if (json["d"] is JObject dObj)
            {
                statusObj = dObj;
            }
            else if (json["RESPONSE"] is JObject responseObj)
            {
                statusObj = responseObj;
            }
            else if (json["TYPE"] != null)
            {
                statusObj = json;
            }

            if (statusObj != null)
            {
                var type = statusObj["TYPE"]?.ToString();
                if (string.IsNullOrEmpty(type) && (httpStatus == 200 || httpStatus == 201))
                {
                    type = "S";
                }

                return (
                    type ?? "E",
                    statusObj["MESSAGE"]?.ToString() ?? "",
                    statusObj["DOC_NO"]?.ToString() ?? ""
                );
            }

            if (httpStatus == 200 || httpStatus == 201)
            {
                return ("S", "", "");
            }

            return ("E", "Unknown SAP response format", "");
        }

        public async Task<DataTable> ConvertResponseToDataTable(HttpResponseMessage response)
        {
            var sapResponse = await ParseSapResponse(response);
            DataTable dt = new DataTable();
            dt.Columns.Add("TYPE");
            dt.Columns.Add("MESSAGE");
            dt.Columns.Add("DOC_NO");
            dt.Rows.Add(sapResponse.StatusCode, sapResponse.Message, sapResponse.DocNo);
            return dt;
        }
    }
}
