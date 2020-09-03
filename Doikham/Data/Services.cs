using Doikham.Shared.Database;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Doikham.Data
{
    public class Services
    {
        public HttpClient Client { get; }
        public IConfiguration Config { get; }

        //string strJ = JsonConvert.SerializeObject(data);
        public Services(HttpClient client, IConfiguration config)
        {
            Config = config;

            var baseAddress = Config.GetSection("VTECAPI")["BaseAddress"].ToString();

            client.BaseAddress = new Uri(baseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(1000);

            Client = client;
        }
        public async Task<T> Get<T>(string method, string param)
        {
            T results = default;
            try
            {
                HttpResponseMessage response = Client.GetAsync($"{method}{param}").Result;
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    results = JsonConvert.DeserializeObject<T>(jsonString);
                }
               
            }
            catch (HttpRequestException e)
            {

            }

            return results;
        }

        public async Task<T> Post<T>(string method, string param, object data)
        {
            T results = default;
            try
            {

                StringContent content = new StringContent("");
                try
                {
                    content = new StringContent(JsonConvert.SerializeObject(data)
                  , Encoding.UTF8, "application/json");
                }
                catch
                {

                }

                HttpResponseMessage response = await Client.PostAsync($"{method}{param}", content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    results = JsonConvert.DeserializeObject<T>(jsonString);

                }
            }
            catch (HttpRequestException e)
            {

            }

            return results;
        }

        public async Task<ResultShopData> Shops()
        {
            ResultShopData data = await Get<ResultShopData>("api/Log/Shops","");
            return data;
        }

        public async Task<ResultDoctypeData> DocTypes()
        {
            ResultDoctypeData data = await Get<ResultDoctypeData>("api/Log/DocTypes", "");
            return data;
        }

        public async Task<ResultInterfaceData> InfaceData(object param)
        {
            ResultInterfaceData data = await Post<ResultInterfaceData>("api/Log/GetLog", "", param);
            return data;
        }
        public async Task<ResultInterfaceData> ResendData(string param)
        {
            ResultInterfaceData data = await Post<ResultInterfaceData>("api/Log/ResendData", $"?uuid={param}" , "");
            return data;
        }

    }
}
