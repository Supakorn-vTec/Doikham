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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Doikham.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private IInventory repo { get; set; }
        private ISAP repoSAP { get; set; }
        private IPOSLog repoLog { get; set; }
        CultureInfo invC;
        private readonly IConfiguration _config;
        private bool enableSendDataToSAP = true;
        private string startSaleDate = "2026-01-01";
        public InventoryController(IInventory inventory, ISAP sap, IPOSLog poslog, IConfiguration configuration)
        {
            repo = inventory;
            repoSAP = sap;
            invC = new CultureInfo("en-US");
            _config = configuration;
            repoLog = poslog;
            enableSendDataToSAP = Convert.ToBoolean(_config.GetSection("SAP")["Enable"]);
            startSaleDate =  _config.GetSection("SAP")["StartSaleDate"];
        }

        private async Task WriteApiLog(string apiName, object body, RESPONSEDATA resData, DateTime insertDate, short apiStatus, string errorMessage = null)
        {
            try
            {
                string bodyParam = body != null ? JsonConvert.SerializeObject(body) : null;
                string responseJson = resData != null ? JsonConvert.SerializeObject(resData) : null;
                string queryParams = Request?.QueryString.HasValue == true ? Request.QueryString.Value : null;
                await repoLog.SetApiLog(apiName, Request?.Method ?? "POST", queryParams, bodyParam, responseJson, apiStatus, insertDate, DateTime.Now, errorMessage);
            }
            catch
            {
                // logging must not break API response
            }
        }

        private async Task<SapResponse> PostSapAndLogResponse(string action, string json, string uuid, string documentKey, int shopID, int docType)
        {
            var insertDate = DateTime.Now;
            try
            {
                HttpResponseMessage resFromSAP = await Task.Run(() => repoSAP.Post(action, json));
                var sapRes = await Task.Run(() => repoSAP.ParseSapResponse(resFromSAP));
                var responseJson = sapRes.Json?.ToString() ?? "";
                await Task.Run(() => repoLog.SetResponseLog(uuid, documentKey, shopID, docType, sapRes.StatusCode, responseJson));
                await repoLog.SetApiLog(action, "POST", null, json, responseJson, (short)(sapRes.StatusCode == "S" ? 1 : 0), insertDate, DateTime.Now, sapRes.StatusCode == "S" ? null : sapRes.Message, shopID);
                return sapRes;
            }
            catch (Exception ex)
            {
                var errorResponse = new RESPONSEDATA
                {
                    RESPONSE = new RESPONSE
                    {
                        TYPE = "E",
                        PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss"),
                        MESSAGE = ex.Message
                    }
                };
                string jsonError = JsonConvert.SerializeObject(errorResponse);
                await Task.Run(() => repoLog.SetResponseLog(uuid, documentKey, shopID, docType, "E", jsonError));
                await repoLog.SetApiLog(action, "POST", null, json, jsonError, 0, insertDate, DateTime.Now, ex.Message, shopID);
                return new SapResponse { StatusCode = "E", Message = ex.Message };
            }
        }

        //Purchase Order
        [HttpPost("[action]")]
        public async Task<ActionResult> PurchaseOrder([FromBody]PURCHASEDOCUMENT data)
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            DateTime insertDate = DateTime.Now;
            try
            {
                await Task.Run(() => repo.PurchaseOrderAsync(data));
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("PurchaseOrder", data, resData, insertDate, 1);
                return Ok(resData);
            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("PurchaseOrder", data, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }
        [HttpPost("[action]")]
        public async Task<ActionResult> PurchaseOrderRecipts()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 2;
            DateTime insertDate = DateTime.Now;
            try
            {
                GOODRECEIPTDOCUMENT data = new GOODRECEIPTDOCUMENT();
                string action = "GOODSRECEIPT";
                string documentKey = "";
                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetLog(docType));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        data = await Task.Run(() => repo.PurchaseOrderReciptAsync(documentKey));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }

                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("PurchaseOrderRecipts", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("PurchaseOrderRecipts", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }
        [HttpPost("[action]")]
        public async Task<ActionResult> DirectRecipts()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 39;
            DateTime insertDate = DateTime.Now;
            try
            {
                GOODRECEIPTDOCUMENT data = new GOODRECEIPTDOCUMENT();
                string action = "GOODSRECEIPT";
                string documentKey = "";
                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetLog(docType));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        data = await Task.Run(() => repo.DirectReciptAsync(documentKey));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("DirectRecipts", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("DirectRecipts", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        //Sales Order 

        [HttpPost("[action]")]
        public async Task<ActionResult> SalesOrder()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 20;
            string docTypeCode = "GIS";
            DateTime insertDate = DateTime.Now;
            try
            {
                GOODISSUEINDOCUMENT data = new GOODISSUEINDOCUMENT();
                string action = "GOODSISSUE";
                string documentKey = "GIS";

                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetLog(docType, startSaleDate));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        docType = Convert.ToInt32(dtLog.Rows[i]["documenttypeid"]);
                        string shopCoe = dtLog.Rows[i]["shopCode"].ToString();
                        docTypeCode = "GIS"; 

                        data = await Task.Run(() => repo.SalesOrderAsync(documentKey, docTypeCode,shopCoe));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("SalesOrder", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("SalesOrder", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        //Transfer Order
        [HttpPost("[action]")]
        public async Task<ActionResult> RequestOrder()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 17;
            DateTime insertDate = DateTime.Now;
            try
            {
                REQUESTDOCUMENT data = new REQUESTDOCUMENT();
                string action = "REQUESTFORM";
                string documentKey = "";
                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetLog(docType));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        data = await Task.Run(() => repo.RequestOrderAsync(documentKey));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            var sapRes = await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                            if (sapRes.StatusCode == "S" && !string.IsNullOrEmpty(sapRes.DocNo))
                            {
                                await Task.Run(() => repoLog.SetDocumentRefFromSAP(documentKey, sapRes.DocNo));
                            }
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("RequestOrder", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("RequestOrder", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }
        
        [HttpPost("[action]")]
        public async Task<ActionResult> TransferOrder([FromBody]GOODISSUEOUTDOCUMENT data)
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            DateTime insertDate = DateTime.Now;
            try
            {
                await Task.Run(() => repo.TransferOrderAsync(data));
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("TransferOrder", data, resData, insertDate, 1);
                return Ok(resData);
            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("TransferOrder", data, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }
        
        [HttpPost("[action]")]
        public async Task<ActionResult> TransOrderRecipts()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 25;
            DateTime insertDate = DateTime.Now;
            try
            {
                GOODRECEIPTDOCUMENT data = new GOODRECEIPTDOCUMENT();
                string action = "GOODSRECEIPT";
                string documentKey = "";
                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetLog(docType));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey =  dtLog.Rows[i]["documentkey"].ToString();
                        data = await Task.Run(() => repo.TransferOrderReciptAsync(documentKey));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("TransOrderRecipts", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("TransOrderRecipts", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> AdjustOrder()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 0;
            string docTypeCode = "";
            DateTime insertDate = DateTime.Now;
            try
            {
                STOCKADJUST data = new STOCKADJUST();
                string action = "STOCK_ADJUST";
                string documentKey = "";

                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetAJLog());
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        docType = Convert.ToInt32(dtLog.Rows[i]["documenttypeid"]);
                        docTypeCode = dtLog.Rows[i]["DocumentTypeHeader"].ToString();

                        data = await Task.Run(() => repo.AdjustStockAsync(documentKey, docTypeCode));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("AdjustOrder", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("AdjustOrder", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> PrefinishOrder_BatchOUT()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 1002;
            string docTypeCode = "";
            DateTime insertDate = DateTime.Now;
            try
            {
                PREFINISHDOCUMENT data = new PREFINISHDOCUMENT();
                string action = "GOODSISSUE";
                string documentKey = "";
            
                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetPNLog(docType));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        docType = Convert.ToInt32(dtLog.Rows[i]["documenttypeid"]);
                        docTypeCode = dtLog.Rows[i]["DocumentTypeHeader"].ToString();

                        data = await Task.Run(() => repo.GIPrefinishAsync(documentKey, docTypeCode));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("PrefinishOrder_BatchOUT", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("PrefinishOrder_BatchOUT", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> PrefinishOrder_BatchIN()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 1001;
            string docTypeCode = "";
            DateTime insertDate = DateTime.Now;
            try
            {
                PREFINISHDOCUMENT_GR data = new PREFINISHDOCUMENT_GR();
                string action = "GOODSISSUE";
                string documentKey = "";

                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetPNLog(docType));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        docType = Convert.ToInt32(dtLog.Rows[i]["documenttypeid"]);
                        docTypeCode = dtLog.Rows[i]["DocumentTypeHeader"].ToString();

                        data = await Task.Run(() => repo.GRPrefinishAsync(documentKey, docTypeCode));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("PrefinishOrder_BatchIN", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("PrefinishOrder_BatchIN", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> RETURNT_TO_DC()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 0;
            string docTypeCode = "RDC";
            DateTime insertDate = DateTime.Now;
            try
            {
                RETURNTTODC data = new RETURNTTODC();
                string action = "GOODSISSUE";
                string documentKey = "";

                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetToDCLog());
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        docType = Convert.ToInt32(dtLog.Rows[i]["documenttypeid"]);
                        //docTypeCode = dtLog.Rows[i]["DocumentTypeHeader"].ToString();

                        data = await Task.Run(() => repo.TransferToDCAsync(documentKey, docTypeCode));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("RETURNT_TO_DC", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("RETURNT_TO_DC", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> RETURNT_TO_SLOC()
        {

            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 0;
            string docTypeCode = "TRF";
            DateTime insertDate = DateTime.Now;
            try
            {
                RETURNTTOSLOC data = new RETURNTTOSLOC();
                string action = "GOODSISSUE";
                string documentKey = "";

                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetToSLOCLog());
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["documentkey"].ToString();
                        docType = Convert.ToInt32(dtLog.Rows[i]["documenttypeid"]);
                        //docTypeCode = dtLog.Rows[i]["DocumentTypeHeader"].ToString();

                        data = await Task.Run(() => repo.TransferToSLOCAsync(documentKey, docTypeCode));
                        string json = JsonConvert.SerializeObject(data);

                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string statusCode = "S";
                        string docDate = "{ d'" + Convert.ToDateTime(dtLog.Rows[i]["documentDate"]).ToString("yyyy-MM-dd", invC) + "'} ";
                        string uuid = await Task.Run(() => repoLog.SetLog(documentKey, shopID, docDate, docType, statusCode, json));

                        if (enableSendDataToSAP == true)
                        {
                            await PostSapAndLogResponse(action, json, uuid, documentKey, shopID, docType);
                        }
                        else
                        {
                            await repoLog.SetApiLog(action, "POST", null, json, null, 0, DateTime.Now, DateTime.Now, "SAP:Enable is false", shopID);
                        }
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("RETURNT_TO_SLOC", null, resData, insertDate, 1);
                return Ok(resData);

            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("RETURNT_TO_SLOC", null, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }


        //Resend Data To SAP
        [HttpPost("[action]")]
        public async Task<ActionResult> ResendData([FromBody]RESENDDATA data)
        {
            RESPONSEDATA resData = new RESPONSEDATA();
            RESPONSE response = new RESPONSE();
            int docType = 0;
            string action = "";
            string documentKey = "";
            string uuid = "";
            DateTime insertDate = DateTime.Now;
            try
            {
                switch (data.ACTION)
                {
                    case SAPMETHOD.GOODSISSUE:
                        action = "GOODSISSUE";
                        break;
                    case SAPMETHOD.STOCK_ADJUST:
                        action = "STOCK_ADJUST";
                        break;
                    case SAPMETHOD.GOODSRECEIPT:
                        action = "GOODSRECEIPT";
                        break;
                    case SAPMETHOD.REQUESTFORM:
                        action = "REQUESTFORM";
                        break;
                    case SAPMETHOD.RECIPES:
                        action = "RECIPES";
                        break;
                }
                action = data.ACTION.ToString();
                uuid = data.UUID.ToString();
                DataTable dtLog = new DataTable();
                dtLog = await Task.Run(() => repoLog.GetLogForResend(uuid));
                if (dtLog.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLog.Rows.Count; i++)
                    {
                        documentKey = dtLog.Rows[i]["TranKey"].ToString();
                        docType = Convert.ToInt32(dtLog.Rows[i]["doctype"]);
                        int shopID = Convert.ToInt32(dtLog.Rows[i]["shopid"]);
                        string json = dtLog.Rows[i]["msglog"].ToString();
                        var jsonData = JObject.Parse(json);

                        await PostSapAndLogResponse(action, jsonData.ToString(), uuid, documentKey, shopID, docType);
                    }
                }
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                resData.RESPONSE = response;
                await WriteApiLog("ResendData", data, resData, insertDate, 1);
                return Ok(resData);
            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                resData.RESPONSE = response;
                await WriteApiLog("ResendData", data, resData, insertDate, 0, e.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, resData);
            }
        }

        public T ToObject<T>(string node, string json)
        {
            T data;
            JToken results = JObject.Parse(json);
            if (node.Equals(string.Empty))
            {
                data = results.ToObject<T>();
            }
            else
            {
                data = results[node].ToObject<T>();
            }
            return (T)Convert.ChangeType(data, typeof(T));
        }
        public List<T> ToList<T>(string node, string json)
        {
            List<T> resultData;
            JToken results = JArray.Parse(json);

            if (node.Equals(string.Empty))
            {
                resultData = results.ToObject<List<T>>();
            }
            else
            {
                resultData = results[node].ToObject<List<T>>();
            }
            return resultData;
        }

    }
}