using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Doikham.API.Data;
using Doikham.Shared.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Doikham.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        private ISales repo { get; set; }
        private ISAP repoSAP { get; set; }
        private IPOSLog repoLog { get; set; }
        CultureInfo invC;
        private readonly IConfiguration _config;
        private bool enableSendDataToSAP = true;
        public SalesController(ISales sales, ISAP sap, IPOSLog poslog, IConfiguration configuration)
        {
            repo = sales;
            repoSAP = sap;
            repoLog = poslog;
            invC = new CultureInfo("en-US");
            _config = configuration;
            enableSendDataToSAP = Convert.ToBoolean(_config.GetSection("SAP")["Enable"]);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> DailySales()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            try
            {
                int docType = 8;
                DataTable dt = new DataTable();
                dt = await Task.Run(() => repo.GetLog());
                for (int i = 0; i < dt.Rows.Count; i++)
                {

                    int shopId = Convert.ToInt32(dt.Rows[i]["shopid"]);
                    string shopCode = dt.Rows[i]["shopcode"].ToString();
                    string tranKey = "";
                    string statusCode = "S";
                    string saleDate = "{ d'" + Convert.ToDateTime(dt.Rows[i]["saledate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                    string data = await Task.Run(() => repo.DailySales(shopId, saleDate));
                    string uuid = await Task.Run(() => repoLog.SetLog(tranKey, shopId, saleDate, docType, statusCode, data));

                    string timeSp = DateTime.Now.ToString("HHmmss", invC);
                    string fileName = $"FPOS{shopCode}_{Convert.ToDateTime(dt.Rows[i]["saledate"]).ToString("yyMMdd", invC) }_{timeSp}.csv";

                    string sourceDir = $"{AppDomain.CurrentDomain.BaseDirectory}{_config.GetSection("SAP")["SorceDir"].ToString()}";
                    Directory.CreateDirectory(sourceDir);
                    string sourceDirName = Path.Combine(sourceDir, fileName);
                    repo.WriteFile(sourceDirName, data);

                    string username = _config.GetSection("FTP")["Username"].ToString();
                    string password = _config.GetSection("FTP")["Password"].ToString();
                    string ftpServer = _config.GetSection("FTP")["Server"].ToString();
                    string destDir = _config.GetSection("FTP")["DestDir"].ToString();
                    string ftpServerPath = $"ftp://{ftpServer}/{destDir}/{fileName}";

                    using (var client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential(username, password);
                        client.UploadFile(ftpServerPath,  sourceDirName);
                    }

                }

                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                return Ok(resData);
            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }


    }
}