using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Doikham.API.Data;
using Doikham.Shared.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Doikham.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private IPOSLog repoLog { get; set; }
        private ISAP repoSAP { get; set; }
        private ILib Lib { get; set; }
        CultureInfo invC;
        private readonly IConfiguration _config;

        public LogController(IPOSLog poslog, ISAP sap, IConfiguration configuration, ILib libb)
        {
            invC = new CultureInfo("en-US");
            _config = configuration;
            repoLog = poslog;
            repoSAP = sap;
            Lib = libb;
        }

        //Resend Data To SAP
        [HttpPost("[action]")]
        public async Task<ActionResult> ResendData(string uuid)
        {
            ActionResultData actionResultData = new ActionResultData();
            try
            {
                await Task.Run(() => repoLog.DeleteLog(uuid));
            }
            catch (Exception ex)
            {
                actionResultData.ReponseCode = "99";
                actionResultData.ResponseText = ex.Message;
            }
            return Ok(actionResultData);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> GetLog([FromBody] CriteriaData criteriaData)
        {
            List<InterfaceLog> interfaceLogs = new List<InterfaceLog>();
            DataTable dt = new DataTable();
            ResultInterfaceData resultData = new ResultInterfaceData();
            try
            {

                dt = await Task.Run(() => repoLog.GetInterfaceLog(criteriaData.ShopID, criteriaData.DocType, criteriaData.FromDate, criteriaData.ToDate));
                interfaceLogs = Lib.ConvertDataTable<InterfaceLog>(dt);

                resultData.Data = interfaceLogs;
            }
            catch (Exception ex)
            {
                resultData.ReponseCode = "99";
                resultData.ResponseText = ex.Message;
            }
            return Ok(resultData);
        }

        [HttpGet("[action]")]
        public async Task<ActionResult> Shops()
        {
            List<ShopData> shops = new List<ShopData>();
            DataTable dt = new DataTable();
            ResultShopData resultData = new ResultShopData();
            try
            {
                dt = await Task.Run(() => repoLog.GetInterfaceShop());
                shops = Lib.ConvertDataTable<ShopData>(dt);

                resultData.Data = shops;
            }
            catch (Exception ex)
            {
                resultData.ReponseCode = "99";
                resultData.ResponseText = ex.Message;
            }
            return Ok(resultData);
        }

        [HttpGet("[action]")]
        public async Task<ActionResult> DocTypes()
        {
            List<DocTypeData> doctypes = new List<DocTypeData>();
            DataTable dt = new DataTable();
            ResultDoctypeData resultData = new ResultDoctypeData();
            try
            {
                dt = await Task.Run(() => repoLog.GetInterfaceDocType());
                doctypes = Lib.ConvertDataTable<DocTypeData>(dt);
                resultData.Data = doctypes;
            }
            catch (Exception ex)
            {
                resultData.ReponseCode = "99";
                resultData.ResponseText = ex.Message;
            }
            return Ok(resultData);
        }
    }
}
