using Doikham.Shared.Database;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Doikham.API.Data
{
    public interface ISAP
    {
        public Task<HttpResponseMessage> Post(string aciton, string data);
        public Task<DataTable> ConvertResponseToDataTable(HttpResponseMessage response);
    }

    public class SAP : ISAP
    {
        private readonly IConfiguration _config;
        CultureInfo invC;
        HttpClient client;
        private string baseUrl = "";
        private string apiKey = "";
        private IDBHelper _dbHelper;
        private string connString = "";

        public SAP(IConfiguration configuration, IDBHelper dBHelper)
        {
            _config = configuration;
            baseUrl = _config.GetSection("SAP")["BaseUrl"];
            apiKey = _config.GetSection("SAP")["ApiKey"];
            _dbHelper = dBHelper;
            connString = _config.GetSection("Database")["ConnectionString"];
            invC = new CultureInfo("en-US");
        }

        public async Task<HttpResponseMessage> Post(string aciton, string data)
        {
            client = new HttpClient();

            HttpResponseMessage response = new HttpResponseMessage();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", apiKey);
            client.Timeout = TimeSpan.FromSeconds(360);
            try
            {
                response = await client.PostAsync(baseUrl + $"/{aciton}", new StringContent(data, Encoding.UTF8, "application/json"));
            }
            catch (TaskCanceledException e)
            {
                data = e.ToString();
            }
            return response;
        }
        public async Task<DataTable> ConvertResponseToDataTable(HttpResponseMessage response)
        {
            DataTable dt = new DataTable();
            try
            {
                var resMsg = await response.Content.ReadAsStringAsync();
                var jsonLinq = JObject.Parse(resMsg);

                // Find the first array using Linq
                var linqArray = jsonLinq.Descendants().Where(x => x is JArray).First();
                var jsonArray = new JArray();
                foreach (JObject row in linqArray.Children<JObject>())
                {
                    var createRow = new JObject();
                    foreach (JProperty column in row.Properties())
                    {
                        // Only include JValue types
                        if (column.Value is JValue)
                        {
                            createRow.Add(column.Name, column.Value);
                        }
                    }
                    jsonArray.Add(createRow);
                }
               dt = JsonConvert.DeserializeObject<DataTable>(jsonArray.ToString());
              
            }
            catch (Exception e)
            {

            }

            return dt;
        }
    }

}
