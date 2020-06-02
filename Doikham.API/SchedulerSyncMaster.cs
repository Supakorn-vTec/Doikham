using Doikham.API.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Doikham.API
{
    public class SchedulerSyncMaster : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IConfiguration _config;

        public SchedulerSyncMaster(IConfiguration configuration)
        {
            _config = configuration;
        }
        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int intervalTime = Convert.ToInt32(_config.GetSection("SAP")["IntervalTimeSyncMaster"]);
            _timer = new Timer(AutoSendAsync, null, TimeSpan.Zero, TimeSpan.FromHours(intervalTime));
            return Task.CompletedTask;
        }

        private async void AutoSendAsync(object state)
        {
            string baseUrl = _config.GetSection("VTECApi")["BaseUrl"];
            await PostAsync($"{baseUrl}Master/Sync");           
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public async Task<HttpResponseMessage> PostAsync(string action)
        {
            int timeOut = 180;
            HttpClient client = new HttpClient();
            HttpResponseMessage response = new HttpResponseMessage();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(timeOut);
            try
            {
                response = await client.PostAsync(action, null);
            }
            catch (Exception e)
            {
                if (response == null)
                {
                    response = new HttpResponseMessage();
                }
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ReasonPhrase = e.Message;
            }
            return response;
        }
    }
}
