using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doikham.API.Data;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;

namespace Doikham.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IDBHelper, SqlServer>();
            services.AddSingleton<IMaster, Master>();
            services.AddSingleton<ISAP, SAP>();
            services.AddSingleton<ISales, Sales>();
            services.AddSingleton<IPOSLog, POSLog>();
            services.AddSingleton<ILib, Lib>();

            bool EnableService = false;
            EnableService = Convert.ToBoolean(Configuration.GetSection("VTECApi")["EnableService"]);
            if (EnableService == true)
            {
                services.AddHostedService<Scheduler>();
                //services.AddHostedService<SchedulerSyncMaster>();
                services.AddHostedService<SchedulerSendSales>();
            }
            services.AddSwaggerGen(c =>{c.SwaggerDoc("v1", new OpenApiInfo { Title = "VTEC API", Version = "v1" });});

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "VTEC API V1");
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
