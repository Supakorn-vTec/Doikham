using Doikham.Shared.Database;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Doikham.API.Data
{
    public interface IInventory
    {
        public Task<bool> PurchaseOrderAsync(PURCHASEDOCUMENT purchase);
        public Task<GOODRECEIPTDOCUMENT> PurchaseOrderReciptAsync(string documentKey);
        public Task<GOODRECEIPTDOCUMENT> DirectReciptAsync(string documentKey);
        public Task<REQUESTDOCUMENT> RequestOrderAsync(string documentKey);
        public Task<bool> TransferOrderAsync(GOODISSUEOUTDOCUMENT issueData);
        public Task<GOODRECEIPTDOCUMENT> TransferOrderReciptAsync(string documentKey);

        public Task<STOCKADJUST> AdjustStockAsync(string documentKey, string docTypeCode);
        public Task<PREFINISHDOCUMENT_GR> GRPrefinishAsync(string documentKey, string docTypeCode);
        public Task<PREFINISHDOCUMENT> GIPrefinishAsync(string documentKey, string docTypeCode);
        public Task<RETURNTTODC> TransferToDCAsync(string documentKey, string docTypeCode);
        public Task<RETURNTTOSLOC> TransferToSLOCAsync(string documentKey, string docTypeCode);
        public Task<GOODISSUEINDOCUMENT> SalesOrderAsync(string documentKey, string docTypeCode, string shopCoe);
        public Task<DataTable> GetGisDocumentsForDailySale(int shopId, string saleDate);


    }
    public class Inventory : IInventory
    {
        private readonly IConfiguration _config;
        private IDBHelper _dbHelper;
        private string connString = "";
        private IPOSLog posLog;
        CultureInfo invC;
        public Inventory(IConfiguration configuration, IDBHelper dBHelper, IPOSLog pOSLog)
        {
            _config = configuration;
            _dbHelper = dBHelper;
            posLog = pOSLog;
            connString = _config.GetSection("Database")["ConnectionString"];
            invC = new CultureInfo("en-US");
        }

        #region "Sales Order"

        #endregion
        #region "Adjust Order"
        public async Task<GOODISSUEINDOCUMENT> AdjustOrderAsync(string documentKey, string docTypeCode)
        {

            GOODISSUEINDOCUMENT data = new GOODISSUEINDOCUMENT();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));

            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }

            if (dtH.Rows.Count > 0)
            {
                int docTypeId = Convert.ToInt32(dtH.Rows[0]["DocumentTypeId"]);
                data.POSTYPE = docTypeCode;
                data.POSDOCITEM = dtH.Rows[0]["documentno"].ToString();
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                if (docTypeId == 3)
                {
                    data.RESNO = dtH.Rows[0]["documentnoref"].ToString();
                }
                else
                {
                    data.RESNO = "";
                }
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.XBLNR = "";
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODISSUEIN_ITEMS> items = new List<GOODISSUEIN_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODISSUEIN_ITEMS()
                             {
                                 POSGIITEMNO = dr["DocDetailID"].ToString(),
                                 RESITEMNO = "",// dr["RESITEMNO"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 RWERKS = dr["ToShopCode"].ToString(),
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString(PropertyTextValue),
                                 MEINS = dr["UnitName"].ToString(),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }

        public async Task<STOCKADJUST> AdjustStockAsync(string documentKey, string docTypeCode)
        {

            STOCKADJUST data = new STOCKADJUST();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));

            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                int docTypeId = Convert.ToInt32(dtH.Rows[0]["DocumentTypeId"]);
                data.POSTYPE = docTypeCode;
                data.POSDOCITEM = dtH.Rows[0]["documentno"].ToString();
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                if (docTypeId == 3)
                {
                    data.RESNO = dtH.Rows[0]["documentnoref"].ToString();
                }
                else
                {
                    data.RESNO = "";
                }
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.XBLNR = "";
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODISSUEIN_ITEMS> items = new List<GOODISSUEIN_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODISSUEIN_ITEMS()
                             {
                                 POSGIITEMNO = dr["DocDetailID"].ToString(),
                                 RESITEMNO = "",// dr["RESITEMNO"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 RWERKS = dr["ToShopCode"].ToString(),
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString(PropertyTextValue),
                                 MEINS = dr["UnitName"].ToString(),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }

        public async Task<PREFINISHDOCUMENT> GIPrefinishAsync(string documentKey, string docTypeCode)
        {

            PREFINISHDOCUMENT data = new PREFINISHDOCUMENT();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail_Prefinish(documentKey));


            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                int docTypeId = Convert.ToInt32(dtH.Rows[0]["DocumentTypeId"]);
                data.POSTYPE = docTypeCode;
                data.POSDOCITEM = dtH.Rows[0]["documentno"].ToString();
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                if (docTypeId == 3)
                {
                    data.RESNO = dtH.Rows[0]["documentnoref"].ToString();
                }
                else
                {
                    data.RESNO = "";
                }
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.XBLNR = "";
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODISSUEIN_ITEMS> items = new List<GOODISSUEIN_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODISSUEIN_ITEMS()
                             {
                                 POSGIITEMNO = dr["DocDetailID"].ToString(),
                                 RESITEMNO = "",// dr["RESITEMNO"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 RWERKS = dr["ToShopCode"].ToString(),
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString(PropertyTextValue),
                                 MEINS = dr["UnitName"].ToString(),
                               
                                   SGTXT = dr["Parent_MaterialCode"].ToString()

                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }
        public async Task<PREFINISHDOCUMENT_GR> GRPrefinishAsync(string documentKey, string docTypeCode)
        {

            PREFINISHDOCUMENT_GR data = new PREFINISHDOCUMENT_GR();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));


            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                int docTypeId = Convert.ToInt32(dtH.Rows[0]["DocumentTypeId"]);
                data.POSTYPE = docTypeCode;
                data.POSDOCITEM = dtH.Rows[0]["documentno"].ToString();
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                if (docTypeId == 3)
                {
                    data.RESNO = dtH.Rows[0]["documentnoref"].ToString();
                }
                else
                {
                    data.RESNO = "";
                }
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.XBLNR = "";
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODISSUEIN_ITEMS> items = new List<GOODISSUEIN_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODISSUEIN_ITEMS()
                             {
                                 POSGIITEMNO = dr["DocDetailID"].ToString(),
                                 RESITEMNO = "",// dr["RESITEMNO"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 RWERKS = dr["ToShopCode"].ToString(),
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString(PropertyTextValue),
                                 MEINS = dr["UnitName"].ToString(),
                                 SGTXT = string.Empty

                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }
        public async Task<RETURNTTODC> TransferToDCAsync(string documentKey, string docTypeCode)
        {

            RETURNTTODC data = new RETURNTTODC();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));

            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                int docTypeId = Convert.ToInt32(dtH.Rows[0]["DocumentTypeId"]);
                data.POSTYPE = docTypeCode;
                data.POSDOCITEM = dtH.Rows[0]["documentno"].ToString();
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                if (docTypeId == 3)
                {
                    data.RESNO = dtH.Rows[0]["documentnoref"].ToString();
                }
                else
                {
                    data.RESNO = "";
                }
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.XBLNR = "";
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODISSUEIN_ITEMS> items = new List<GOODISSUEIN_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODISSUEIN_ITEMS()
                             {
                                 POSGIITEMNO = dr["DocDetailID"].ToString(),
                                 RESITEMNO = "",// dr["RESITEMNO"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 RWERKS = dr["ToShopCode"].ToString(),
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString(PropertyTextValue),
                                 MEINS = dr["UnitName"].ToString(),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }
        public async Task<RETURNTTOSLOC> TransferToSLOCAsync(string documentKey, string docTypeCode)
        {

            RETURNTTOSLOC data = new RETURNTTOSLOC();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));

            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                int docTypeId = Convert.ToInt32(dtH.Rows[0]["DocumentTypeId"]);
                data.POSTYPE = docTypeCode;
                data.POSDOCITEM = dtH.Rows[0]["documentno"].ToString();
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                if (docTypeId == 3)
                {
                    data.RESNO = dtH.Rows[0]["documentnoref"].ToString();
                }
                else
                {
                    data.RESNO = "";
                }
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.XBLNR = "";
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODISSUEIN_ITEMS> items = new List<GOODISSUEIN_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODISSUEIN_ITEMS()
                             {
                                 POSGIITEMNO = dr["DocDetailID"].ToString(),
                                 RESITEMNO = "",// dr["RESITEMNO"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 RWERKS = dr["ToShopCode"].ToString(),
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString(PropertyTextValue),
                                 MEINS = dr["UnitName"].ToString(),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }


        #endregion

        #region "Request Order From SAP"
        public async Task<REQUESTDOCUMENT> RequestOrderAsync(string documentKey)
        {

            REQUESTDOCUMENT data = new REQUESTDOCUMENT();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();
            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }

            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                data.POSTYPE = dtH.Rows[0]["documenttypeheader"].ToString(); 
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.BKTXT = dtH.Rows[0]["documentno"].ToString();
                data.HGTXT = dtH.Rows[0]["remark"].ToString();
                List<REQUEST_ITEMS> items = new List<REQUEST_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new REQUEST_ITEMS()
                             {
                                 ZEILE = dr["DocDetailID"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 MAKTX = dr["ProductName"].ToString(),
                                 WERKS = dr["ShopCode"].ToString(),
                                 SUPPLANT = dr["ToShopCode"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["SmallQty"]).ToString(PropertyTextValue),
                                 MEINS = dr["SmallUnitName"].ToString(),
                                 DELDATE = Convert.ToDateTime(dr["DueDate"]).ToString("yyyyMMdd", invC),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }
        public async Task<bool> TransferOrderAsync(GOODISSUEOUTDOCUMENT issueData)
        {
            GOODISSUEOUT_ORDER data = new GOODISSUEOUT_ORDER();
            data = issueData.GOODS_ISSUE_OUT;

            bool status = true;
            int shopId = 1;
            int toShopId = 0;
            int fromShopId = 0;
            int documentId = 0;
            int documentTypeId = 3;
            int staffId = 2;
            int keyShopId = 1;
            string documentTypeCode = "";
            string shopCode = "HQ";
            string documentKey = "";
            string documentDate = "";

            if (data.HEADER.ITEMS.Count > 0)
            {
                string ToShopCode = data.HEADER.ITEMS[0].WERKS;
                string ToSLOC = data.HEADER.ITEMS[0].LGORT;
                shopId = 1;
                //toShopId = await Task.Run(() => GetInventoryID(ToShopCode));
                toShopId = await Task.Run(() => GetInventoryIDERP(ToShopCode, ToSLOC));

                DataTable dtT = new DataTable();
                dtT = await Task.Run(() => GetDocumentType(documentTypeId));
                if (dtT.Rows.Count > 0)
                {
                    documentTypeCode = dtT.Rows[0]["DocumentTypeHeader"].ToString();
                }
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;

                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {

                        string documentNo = "";
                        string documentNoRef = "";

                        string invoicePODate = "";
                        string dueDate = "";
                        string timeStamp = "";
                        string invoiceRef = "";
                        int documentYear = 0;
                        int documentMonth = 0;
                        int documentDay = 0;
                        int documentNumber = 0;
                        int docdetailId = 0;
                        int vendorId = 0;
                        int vendorGroupId = 0;
                        int documentStatus = 2;
                        int documentIdRef = 0;
                        int docIdRefShopId = 0;
                        decimal subTotal = 0;
                        decimal totalDiscount = 0;
                        decimal totalVAT = 0;
                        decimal netPrice = 0;
                        decimal grandTotal = 0;
                        int vatPercent = 7;

                        DateTime syncDate = DateTime.Now;
                        string docDate = syncDate.ToString("yyyy-MM-dd", invC);
                        documentYear = syncDate.Year;
                        documentMonth = syncDate.Month;
                        documentDay = syncDate.Day;
                        documentDate = "{ d '" + docDate + "' }";
                        dueDate = "{ d '" + docDate + "' }";
                        invoicePODate = "{ d '" + docDate + "' }";
                        timeStamp = "{ ts '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", invC) + "' }";

                        documentId = await Task.Run(() => GetDocumentID(keyShopId, connection, transaction));
                        documentKey = $"{documentId}:{keyShopId}";

                        documentNo = await Task.Run(() => GetDocumentNumber(shopId, documentTypeId, documentMonth, documentYear, shopCode, documentTypeCode, connection, transaction));
                        documentNoRef = data.HEADER.MBLNR;
                        invoiceRef = data.HEADER.ITEMS[0].EBELN;

                        DataTable dtRQRef = await GetRequestDocumentRef(invoiceRef);
                        if (dtRQRef.Rows.Count > 0)
                        {
                            shopId = Convert.ToInt32(dtRQRef.Rows[0]["ToInvID"]);
                            documentIdRef = Convert.ToInt32(dtRQRef.Rows[0]["documentid"]);
                            docIdRefShopId = Convert.ToInt32(dtRQRef.Rows[0]["keyshopid"]);
                        }
                        DataTable dtValidate = await ValidateStoreSAP(shopId);
                        if (dtValidate.Rows.Count > 0)
                        {
                            await Task.Run(() => InsertDocumentHeader(documentId, keyShopId, documentKey, vendorId, vendorGroupId, documentTypeId, documentYear, documentMonth, documentNumber, documentNo, documentNoRef, invoiceRef, documentDate, shopId, documentStatus, documentIdRef, docIdRefShopId, toShopId, fromShopId, subTotal, totalDiscount, totalVAT, netPrice, grandTotal, data.HEADER.BKTXT, staffId, staffId, staffId, 0, timeStamp, timeStamp, timeStamp, dueDate, invoicePODate, vatPercent, connection, transaction));

                            docdetailId = await Task.Run(() => GetMaxDocdetailID(documentId, keyShopId, connection, transaction));

                            for (int i = 0; i < data.HEADER.ITEMS.Count; i++)
                            {
                                int materialId = 0;
                                string materialCode = "";
                                string materialName = "";
                                string unitName = "";
                                decimal materialQty = 0;
                                int unitsmallId = 0;
                                int unitlargeId = 0;
                                int unitlargeRatio = 1;
                                decimal unitratio = 0;
                                decimal pricePerUnit = 0;
                                int discountType = 0;
                                decimal percentDiscount = 0;
                                decimal amountDiscount = 0;
                                int vatType = 0;
                                string vatCode = "N";
                                decimal totalVat = 0;
                                decimal totalPrice = 0;
                                decimal unitsmallQty = 0;
                                string supplierMaterialCode = "";
                                string supplierMaterialName = "";
                                string remarkLine = "";
                                DataTable dt = new DataTable();

                                dt = await Task.Run(() => CheckMaterial(data.HEADER.ITEMS[i].MATNR, data.HEADER.ITEMS[i].MEINS, connection, transaction));

                                if (dt.Rows.Count > 0)
                                {
                                    materialId = Convert.ToInt32(dt.Rows[0]["materialid"]);
                                    materialCode = data.HEADER.ITEMS[i].MATNR;
                                    materialName = dt.Rows[0]["materialname"].ToString();
                                    supplierMaterialCode = data.HEADER.ITEMS[i].EBELN;
                                    unitName = data.HEADER.ITEMS[i].MEINS;
                                    materialQty = Convert.ToDecimal(data.HEADER.ITEMS[i].MENGE);
                                    unitsmallId = Convert.ToInt32(dt.Rows[0]["unitsmallId"]);
                                    unitlargeId = Convert.ToInt32(dt.Rows[0]["unitlargeId"]);
                                    unitratio = Convert.ToDecimal(dt.Rows[0]["UnitSmallRatio"]);
                                    unitsmallQty = (materialQty * unitlargeRatio);
                                    docdetailId = Convert.ToInt32(data.HEADER.ITEMS[i].EBELP);

                                    DataTable dtCheck = await Task.Run(() => CheckDoDocDetailID(documentId, keyShopId, docdetailId, connection, transaction));

                                    if (dtCheck.Rows.Count > 0)
                                    {
                                        decimal xQty = Convert.ToDecimal(dtCheck.Rows[0]["ProductAmount"]);
                                        materialQty = (materialQty + xQty);
                                        unitsmallQty = (materialQty * unitlargeRatio);
                                        await Task.Run(() => UpdateDocumentDetail(docdetailId, documentId, keyShopId, materialQty, unitsmallQty, connection, transaction));
                                    }
                                    else
                                    {
                                        await Task.Run(() => InsertDocumentDetail(docdetailId, documentId, keyShopId, documentKey, documentDate, shopId, materialId, materialCode, materialName, supplierMaterialCode, supplierMaterialName, materialQty, pricePerUnit, discountType, percentDiscount, amountDiscount, totalDiscount, netPrice, vatType, vatCode, totalVat, totalPrice, unitsmallQty, unitsmallId, unitlargeId, unitratio, unitlargeRatio, unitName, remarkLine, connection, transaction));
                                    }
                                }
                                docdetailId += docdetailId;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        status = false;
                    }
                    transaction.Commit();
                    connection.Close();
                }

                string json = JsonConvert.SerializeObject(data);
                string statusCode = "S";
                await Task.Run(() => posLog.SetLog(data.HEADER.MBLNR, shopId, documentDate, documentTypeId, statusCode, json));
            }
            return status;
        }
        public async Task<GOODRECEIPTDOCUMENT> TransferOrderReciptAsync(string documentKey)
        {

            GOODRECEIPTDOCUMENT data = new GOODRECEIPTDOCUMENT();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();
            DataTable dtR = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentRODetail(documentKey));

            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                data.POSTYPE = "TRO";
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.BKTXT = dtH.Rows[0]["documentno"].ToString();
                data.LFSNR = "";
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODRECEIPT_ITEMS> items = new List<GOODRECEIPT_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODRECEIPT_ITEMS()
                             {
                                 ZEILE = dr["Row_num"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 WERKS = dr["ShopCode"].ToString(),
                                 LIFNR = "",
                                 SWERKS = "",
                                 MENGE = Convert.ToDecimal(dr["SmallQty"]).ToString(PropertyTextValue),
                                 MEINS = dr["SmallUnitName"].ToString(),
                                 NETPR = "0.000",
                                 NETWR = "0.000",
                                 EBELN = dr["EBELN"].ToString(),
                                 EBELP = dr["EBELP"]?.ToString().Trim().PadLeft(5, '0'),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }
        #endregion

        #region "Purchase Order"
        public async Task<bool> PurchaseOrderAsync(PURCHASEDOCUMENT purchase)
        {

            PURCHASE_ORDER data = new PURCHASE_ORDER();
            data = purchase.PURCHASE_ORDER;

            bool status = true;
            int shopId = 1;
            int toShopId = 0;
            int fromShopId = 0;
            int documentId = 0;
            int documentTypeId = 1;
            int staffId = 2;
            int keyShopId = 1;
            int vendorId = 0;
            string documentTypeCode = "";
            string shopCode = "";
            string docDate = "";
            string documentDate = "";
            string documentKey = "";

            if (data.HEADER.ITEMS.Count > 0)
            {

                shopId = await Task.Run(() => GetInventoryID(data.HEADER.ITEMS[0].WERKS));
                shopCode = data.HEADER.ITEMS[0].WERKS;

                DataTable dtT = new DataTable();
                dtT = await Task.Run(() => GetDocumentType(documentTypeId));
                if (dtT.Rows.Count > 0)
                {
                    documentTypeCode = dtT.Rows[0]["DocumentTypeHeader"].ToString();
                }

                vendorId = await Task.Run(() => GetVendorID(data.HEADER.LIFNR));
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;

                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {

                        string documentNo = "";
                        string documentNoRef = "";

                        string invoicePODate = "";
                        string dueDate = "";
                        string timeStamp = "";
                        int documentYear = 0;
                        int documentMonth = 0;
                        int documentDay = 0;
                        int documentNumber = 0;
                        int docdetailId = 0;
                        int vendorGroupId = 0;
                        int documentStatus = 2;
                        int documentIdRef = 0;
                        int docIdRefShopId = 0;
                        decimal subTotal = 0;
                        decimal totalDiscount = 0;
                        decimal totalVAT = 0;
                        decimal netPrice = 0;
                        decimal grandTotal = 0;
                        string remark = "";
                        int vatPercent = 7;

                        DateTime syncDate = DateTime.Now;
                        docDate = syncDate.ToString("yyyy-MM-dd", invC);
                        documentYear = syncDate.Year;
                        documentMonth = syncDate.Month;
                        documentDay = syncDate.Day;
                        documentDate = "{ d '" + docDate + "' }";
                        dueDate = "{ d '" + docDate + "' }";
                        invoicePODate = "{ d '" + docDate + "' }";
                        timeStamp = "{ ts '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", invC) + "' }";

                        documentId = await Task.Run(() => GetDocumentID(keyShopId, connection, transaction));
                        documentKey = $"{documentId}:{keyShopId}";

                        documentNo = await Task.Run(() => GetDocumentNumber(shopId, documentTypeId, documentMonth, documentYear, shopCode, documentTypeCode, connection, transaction));
                        documentNoRef = data.HEADER.EBELN;

                        await Task.Run(() => InsertDocumentHeader(documentId, keyShopId, documentKey, vendorId, vendorGroupId, documentTypeId, documentYear, documentMonth, documentNumber, documentNo, documentNoRef, documentDate, shopId, documentStatus, documentIdRef, docIdRefShopId, toShopId, fromShopId, subTotal, totalDiscount, totalVAT, netPrice, grandTotal, remark, staffId, staffId, staffId, 0, timeStamp, timeStamp, timeStamp, dueDate, invoicePODate, vatPercent, connection, transaction));

                        docdetailId = await Task.Run(() => GetMaxDocdetailID(documentId, keyShopId, connection, transaction));

                        for (int i = 0; i < data.HEADER.ITEMS.Count; i++)
                        {
                            int materialId = 0;
                            string materialCode = "";
                            string materialName = "";
                            string unitName = "";
                            decimal materialQty = 0;
                            int unitsmallId = 0;
                            int unitlargeId = 0;
                            int unitlargeRatio = 1;
                            decimal unitratio = 0;
                            decimal pricePerUnit = 0;
                            int discountType = 0;
                            decimal percentDiscount = 0;
                            decimal amountDiscount = 0;
                            int vatType = 1;
                            string vatCode = "V";
                            decimal totalVat = 0;
                            decimal totalPrice = 0;
                            decimal unitsmallQty = 0;
                            string supplierMaterialCode = "";
                            string supplierMaterialName = "";
                            string remarkLine = "";
                            DataTable dt = new DataTable();

                            dt = await Task.Run(() => CheckMaterial(data.HEADER.ITEMS[i].MATNR, data.HEADER.ITEMS[i].MEINS, connection, transaction));

                            if (dt.Rows.Count > 0)
                            {
                                materialId = Convert.ToInt32(dt.Rows[0]["materialid"]);
                                materialCode = data.HEADER.ITEMS[i].MATNR;
                                materialName = dt.Rows[0]["materialname"].ToString();
                                unitName = data.HEADER.ITEMS[i].MEINS;
                                materialQty = data.HEADER.ITEMS[i].MENGE;
                                pricePerUnit = Convert.ToDecimal(data.HEADER.ITEMS[i].NETPR);
                                totalPrice = Convert.ToDecimal(data.HEADER.ITEMS[i].NETWR);
                                unitsmallId = Convert.ToInt32(dt.Rows[0]["unitsmallId"]);
                                unitlargeId = Convert.ToInt32(dt.Rows[0]["unitlargeId"]);
                                unitratio = Convert.ToDecimal(dt.Rows[0]["UnitSmallRatio"]);
                                unitsmallQty = (materialQty * unitlargeRatio);
                                totalVat = (totalPrice * vatPercent / 107);
                                netPrice = (totalPrice - totalVat);
                                docdetailId = Convert.ToInt32(data.HEADER.ITEMS[i].EBELP);

                                await Task.Run(() => InsertDocumentDetail(docdetailId, documentId, keyShopId, documentKey, documentDate, shopId, materialId, materialCode, materialName, supplierMaterialCode, supplierMaterialName, materialQty, pricePerUnit, discountType, percentDiscount, amountDiscount, totalDiscount, netPrice, vatType, vatCode, totalVat, totalPrice, unitsmallQty, unitsmallId, unitlargeId, unitratio, unitlargeRatio, unitName, remarkLine, connection, transaction));

                            }
                            docdetailId += docdetailId;
                        }

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        status = false;
                    }
                    transaction.Commit();
                    connection.Close();
                }

                string json = JsonConvert.SerializeObject(data);
                string statusCode = "S";
                await Task.Run(() => posLog.SetLog(data.HEADER.EBELN, shopId, documentDate, documentTypeId, statusCode, json));
            }
            return status;
        }
        public async Task<GOODRECEIPTDOCUMENT> PurchaseOrderReciptAsync(string documentKey)
        {

            GOODRECEIPTDOCUMENT data = new GOODRECEIPTDOCUMENT();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));
            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                data.POSTYPE = "RO";
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.BKTXT = dtH.Rows[0]["documentno"].ToString();
                data.LFSNR = "";
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODRECEIPT_ITEMS> items = new List<GOODRECEIPT_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODRECEIPT_ITEMS()
                             {
                                 ZEILE = dr["DocDetailID"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 WERKS = dr["ShopCode"].ToString(),
                                 LIFNR = dr["VendorCode"].ToString(),
                                 SWERKS = $"{dr["ShopCode"].ToString()}-{dr["sloc"].ToString()}",
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString(PropertyTextValue),
                                 MEINS = dr["UnitName"].ToString(),
                                 NETPR = dr["ProductPricePerUnit"].ToString(),
                                 NETWR = dr["ProductTotalPrice"].ToString(),
                                 EBELN = dr["DocumentNoRef"].ToString(),
                                 EBELP = Convert.ToInt32(dr["DocDetailID"]).ToString(),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }
        public async Task<GOODRECEIPTDOCUMENT> DirectReciptAsync(string documentKey)
        {

            GOODRECEIPTDOCUMENT data = new GOODRECEIPTDOCUMENT();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));
            dtL = await Task.Run(() => GetDocumentDetail(documentKey));
            DataTable dtPro = new DataTable();
            string PropertyTextValue = "#,##0.00";
            dtPro = await Task.Run(() => GetProgramPropertyValue(18));
            try
            {
                if (dtPro.Rows[0]["PropertyTextValue"] != null)
                {

                    PropertyTextValue = dtPro.Rows[0]["PropertyTextValue"].ToString();
                }
                else
                {
                    PropertyTextValue = "#,##0.00";
                }
            }
            catch (Exception ex)
            {
                PropertyTextValue = "#,##0.00";
            }
            PropertyTextValue = PropertyTextValue.Replace(",", "");
            if (dtH.Rows.Count > 0)
            {
                data.POSTYPE = "DRO";
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.BKTXT = dtH.Rows[0]["documentno"].ToString();
                data.LFSNR = "";
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODRECEIPT_ITEMS> items = new List<GOODRECEIPT_ITEMS>();
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODRECEIPT_ITEMS()
                             {
                                 ZEILE = dr["DocDetailID"].ToString(),
                                 MATNR = dr["ProductCode"].ToString(),
                                 WERKS = dr["ShopCode"].ToString(),
                                 LIFNR = dr["VendorCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 //MENGE = dr["SmallQty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["SmallQty"]).ToString(PropertyTextValue),
                                 MEINS = dr["SmallUnitName"].ToString(),
                                 NETPR = dr["ProductPricePerUnit"].ToString(),
                                 NETWR = dr["ProductTotalPrice"].ToString(),
                                 EBELN = dr["DocumentNoRef"].ToString(),
                                 EBELP = Convert.ToInt32(dr["DocDetailID"]).ToString(),
                                 SGTXT = "",
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }
        #endregion

        #region "Document Data"
        private async Task<int> GetDocumentID(int shopId, SqlConnection connection, SqlTransaction transaction)
        {
            int documentId = await Task.Run(() => GetMaxDocumentID(shopId, connection, transaction));
            await Task.Run(() => DeleteDocumentID(shopId, connection, transaction));
            await Task.Run(() => CreateDocumentID(shopId, documentId, connection, transaction));

            return documentId;
        }
        private async Task<string> GetDocumentNumber(int shopId, int documentTypeId, int documentMonth, int documentYear, string shopCode, string documentTypeCode, SqlConnection connection, SqlTransaction transaction)
        {
            int documentNumber = await Task.Run(() => GetMaxDocumentNumber(shopId, documentTypeId, documentMonth, documentYear, connection, transaction));
            await Task.Run(() => DeleteDocumentNumber(shopId, documentTypeId, documentMonth, documentYear, connection, transaction));
            await Task.Run(() => CreateDocumentNumber(shopId, documentTypeId, documentMonth, documentYear, documentNumber, connection, transaction));
            string runningNumber = (10000 + documentNumber).ToString();
            string documentNo = $"{shopCode}{documentTypeCode}{documentYear}{documentMonth}/{runningNumber.Substring(runningNumber.Length - 4, 4)}";
            return documentNo;
        }
        private async Task<int> GetMaxDocumentID(int shopId, SqlConnection connection, SqlTransaction transaction)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select case when  max(DocumentID) is null then 1 else max(DocumentID)+1 end DocumentID from DocumentMaxID where KeyShopID={shopId}";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
            int id = 1;
            if (dt.Rows.Count > 0)
            {
                id = Convert.ToInt32(dt.Rows[0]["documentid"]);
            }
            return id;
        }
        private async Task<int> CreateDocumentID(int shopId, int documentId, SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = $"insert into DocumentMaxID(KeyShopID,DocumentID)values({shopId},{documentId});";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task<int> DeleteDocumentID(int shopId, SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = $"delete from DocumentMaxID where keyshopId={shopId}";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task<int> GetMaxDocumentNumber(int shopId, int documentTypeId, int documentMonth, int documentYear, SqlConnection connection, SqlTransaction transaction)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select case when  max(DocumentNumber) is null then 1 else max(DocumentNumber)+1 end MaxID from DocumentMaxNumber where ShopID={shopId} and documenttypeid={documentTypeId} and DocumentMonth={documentMonth} and DocumentYear={documentYear}";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
            int id = 1;
            if (dt.Rows.Count > 0)
            {
                id = Convert.ToInt32(dt.Rows[0]["MaxID"]);
            }
            return id;
        }
        private async Task<int> CreateDocumentNumber(int shopId, int documentTypeId, int documentMonth, int documentYear, int documentNumber, SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = $"insert into DocumentMaxNumber(ShopID,DocumentTypeID,DocumentMonth,DocumentYear,DocumentNumber)values({shopId},{documentTypeId},{documentMonth},{documentYear},{documentNumber})";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task<int> DeleteDocumentNumber(int shopId, int documentTypeId, int documentMonth, int documentYear, SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = $"delete from DocumentMaxNumber where shopid={shopId} and documenttypeid={documentTypeId} and documentmonth={documentMonth} and documentyear={documentYear}";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task<int> GetMaxDocdetailID(int documentId, int keyShopId, SqlConnection connection, SqlTransaction transaction)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select case when  max(docdetailid) is null then 1 else max(docdetailid)+1 end docdetailid from docdetail where documentid={documentId} and keyshopid={keyShopId} ";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            int id = 1;
            if (dt.Rows.Count > 0)
            {
                id = Convert.ToInt32(dt.Rows[0]["docdetailid"]);
            }
            return id;
        }
        private async Task<int> GetInventoryID(string shopCode)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from shop_data where ShopCode='{shopCode}'";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            int id = 0;
            if (dt.Rows.Count > 0)
            {
                id = Convert.ToInt32(dt.Rows[0]["shopid"]);
            }
            return id;
        }
        private async Task<int> GetInventoryIDERP(string shopCode, string SLOC)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from shop_data where PTTShopCode='{shopCode}' and SLOC='{SLOC}'";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            int id = 0;
            if (dt.Rows.Count > 0)
            {
                id = Convert.ToInt32(dt.Rows[0]["shopid"]);
            }
            return id;
        }
        private async Task<DataTable> GetDocumentType(int documentTypeId)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from documenttype where DocumentTypeID={documentTypeId}";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> GetRequestDocumentRef(string invoiceRef)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from document  where DocumentTypeID=17 and DocumentNoRef='{invoiceRef}'";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> ValidateStoreSAP(int shopId)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select* from shop_data where ShopCatID1=3 and ShopID={shopId}";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> GetDocumentHeader(string documentKey)
        {
            DataTable dtH = new DataTable();
            string queryStr = $"select dt.documenttypeheader,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,a.DocumentNumber,case when a.DocumentTypeID=3 and a.ShopID<>1 then a.DocumentNoRef else b.DocumentNoRef end As DocumentNoRef,a.DocumentTypeId,a.DocumentDate,c.StaffCode,a.remark,a.DueDate,a.ShopID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join staffs c on a.ApproveBy=c.StaffID  inner join documenttype dt on a.documenttypeid=dt.DocumentTypeID where a.DocumentStatus=2 and a.DocumentKey='{documentKey}'";
            dtH = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            dtH.TableName = "Header";
            return dtH;
        }

        private async Task<DataTable> GetDocumentDetail(string documentKey)
        {
            DataTable dtL = new DataTable();

            string queryStr = $"select d.ShopCode,d.sloc,e.VendorCode, a.DocumentID,a.KeyShopID,a.DocumentKey,b.DocumentKey As POKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo, b.DocumentNoRef,case when a.DocumentTypeID=25 and (po.SupplierMaterialCode is null or po.SupplierMaterialCode='') then b.DocumentNoRef else po.SupplierMaterialCode end As SupplierMaterialCode,a.DocumentDate,c.DocDetailID,case when a.DocumentTypeID=3 then c.DocDetailID else '' end As RESITEMNO, c.ProductCode,c.ProductName,c.ProductAmount As Qty,c.UnitSmallAmount As SmallQty,c.UnitName, 'EA' As SmallUnitName,c.ProductPricePerUnit,c.ProductNetPrice,c.ProductTotalPrice,LineNumber,s1.ShopCode As ToShopCode,s2.ShopCode As FromShopCode,a.DueDate from document a left join document b on a.DocumentIDRef = b.DocumentID and a.DocIDRefShopID = b.KeyShopID join docdetail c on a.DocumentID = c.DocumentID and a.KeyShopID = c.KeyShopID join shop_data d on a.ShopID = d.ShopID left join vendors e on a.VendorID = e.VendorID left join interface_document_fromsap po on a.DocumentIDRef=po.DocumentID and a.DocIDRefShopID=po.KeyShopID and c.ProductID=po.ProductID left join shop_data s1 on a.ToInvID = s1.ShopID left join shop_data s2 on a.FromInvID = s2.ShopID  where a.DocumentStatus = 2  and a.DocumentKey='{documentKey}' order by DocDetailID";
            dtL = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            dtL.TableName = "Detail";
            return dtL;
        }
        private async Task<DataTable> GetDocumentDetail_Prefinish(string documentKey)
        {
            DataTable dtL = new DataTable();

            string queryStr = $"select d.ShopCode,d.sloc,e.VendorCode, a.DocumentID,a.KeyShopID,a.DocumentKey,b.DocumentKey As POKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo, b.DocumentNoRef,case when a.DocumentTypeID=25 and (po.SupplierMaterialCode is null or po.SupplierMaterialCode='') then b.DocumentNoRef else po.SupplierMaterialCode end As SupplierMaterialCode,a.DocumentDate,c.DocDetailID,case when a.DocumentTypeID=3 then c.DocDetailID else '' end As RESITEMNO, c.ProductCode,c.ProductName,c.ProductAmount As Qty,c.UnitSmallAmount As SmallQty,c.UnitName, 'EA' As SmallUnitName,c.ProductPricePerUnit,c.ProductNetPrice,c.ProductTotalPrice,LineNumber,s1.ShopCode As ToShopCode,s2.ShopCode As FromShopCode,a.DueDate,mc.Parent_MaterialID,mc.Parent_MaterialCode,mc.Parent_MaterialName from document a left join document b on a.DocumentIDRef = b.DocumentID and a.DocIDRefShopID = b.KeyShopID join docdetail c on a.DocumentID = c.DocumentID and a.KeyShopID = c.KeyShopID join shop_data d on a.ShopID = d.ShopID left join vendors e on a.VendorID = e.VendorID left join interface_document_fromsap po on a.DocumentIDRef=po.DocumentID and a.DocIDRefShopID=po.KeyShopID and c.ProductID=po.ProductID left join shop_data s1 on a.ToInvID = s1.ShopID left join shop_data s2 on a.FromInvID = s2.ShopID inner join pos_interface_materialcomponent mc on c.DocumentID=mc.child_DocID and c.KeyShopID=mc.child_keyshopid  and c.ProductID=mc.child_MaterialID  and c.DocDetailID=mc.Child_DocDetailID where a.DocumentStatus = 2  and a.DocumentKey='{documentKey}' order by DocDetailID";
            dtL = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            dtL.TableName = "Detail";
            return dtL;
        }

        private async Task<DataTable> GetDocumentDetail_SaleOrder(string documentKey, int shopId, string docDate)
        {
            DataTable dtL = new DataTable();

            //string queryStr = $"select d.ShopCode,e.VendorCode, a.DocumentID,a.KeyShopID,a.DocumentKey,b.DocumentKey As POKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo, b.DocumentNoRef,case when a.DocumentTypeID=25 and (po.SupplierMaterialCode is null or po.SupplierMaterialCode='') then b.DocumentNoRef else po.SupplierMaterialCode end As SupplierMaterialCode,a.DocumentDate,c.DocDetailID,case when a.DocumentTypeID=3 then c.DocDetailID else '' end As RESITEMNO, c.ProductCode,c.ProductName,c.ProductAmount As Qty,c.UnitSmallAmount As SmallQty,c.UnitName, c.UnitName As SmallUnitName,c.ProductPricePerUnit,c.ProductNetPrice,c.ProductTotalPrice,LineNumber,s1.ShopCode As ToShopCode,s2.ShopCode As FromShopCode,a.DueDate from document a left join document b on a.DocumentIDRef = b.DocumentID and a.DocIDRefShopID = b.KeyShopID join docdetail c on a.DocumentID = c.DocumentID and a.KeyShopID = c.KeyShopID  join shop_data d on a.ShopID = d.ShopID left join vendors e on a.VendorID = e.VendorID left join interface_document_fromsap po on a.DocumentIDRef=po.DocumentID and a.DocIDRefShopID=po.KeyShopID and c.ProductID=po.ProductID left join shop_data s1 on a.ToInvID = s1.ShopID left join shop_data s2 on a.FromInvID = s2.ShopID  where a.DocumentStatus = 2  and a.DocumentKey='{documentKey}' and c.ProductID in(select Materialid from MaterialMaster_GIS) order by DocDetailID";
            //string queryStr = $"select distinct d.ShopCode,e.VendorCode, a.DocumentID,a.KeyShopID,a.DocumentKey,b.DocumentKey As POKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo, b.DocumentNoRef,case when a.DocumentTypeID=25 and (po.SupplierMaterialCode is null or po.SupplierMaterialCode='') then b.DocumentNoRef else po.SupplierMaterialCode end As SupplierMaterialCode,a.DocumentDate,c.DocDetailID,case when a.DocumentTypeID=3 then c.DocDetailID else '' end As RESITEMNO, c.ProductCode,c.ProductName,c.ProductAmount As Qty,c.UnitSmallAmount As SmallQty,c.UnitName, c.UnitName As SmallUnitName,c.ProductPricePerUnit,c.ProductNetPrice,c.ProductTotalPrice,LineNumber,s1.ShopCode As ToShopCode,s2.ShopCode As FromShopCode,a.DueDate from document a left join document b on a.DocumentIDRef = b.DocumentID and a.DocIDRefShopID = b.KeyShopID join docdetail c on a.DocumentID = c.DocumentID and a.KeyShopID = c.KeyShopID right join (select mg.* from orderdetail od right join MaterialMaster_GIS mg on od.productid=mg.productid where saledate='{docDate}' and shopid={shopId}) sale on c.productid=sale.MaterialID join shop_data d on a.ShopID = d.ShopID left join vendors e on a.VendorID = e.VendorID left join interface_document_fromsap po on a.DocumentIDRef=po.DocumentID and a.DocIDRefShopID=po.KeyShopID and c.ProductID=po.ProductID left join shop_data s1 on a.ToInvID = s1.ShopID left join shop_data s2 on a.FromInvID = s2.ShopID  where a.DocumentStatus = 2  and a.DocumentKey='{documentKey}' order by DocDetailID";
            //string queryStr = $"select ShopCode,VendorCode, DocumentID,KeyShopID,DocumentKey,POKey,DocumentYear,DocumentMonth,DocumentNo, DocumentNoRef,SupplierMaterialCode,DocumentDate,DocDetailID, RESITEMNO, ProductCode,ProductName,sum(Qty) as Qty ,sum(SmallQty) as SmallQty,UnitName, SmallUnitName,ProductPricePerUnit,ProductNetPrice,ProductTotalPrice,LineNumber,ToShopCode, FromShopCode,DueDate \r\nfrom (\r\n select distinct d.ShopCode,e.VendorCode, a.DocumentID,a.KeyShopID,a.DocumentKey,b.DocumentKey As POKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo, b.DocumentNoRef,case when a.DocumentTypeID=25 and (po.SupplierMaterialCode is null or po.SupplierMaterialCode='') then b.DocumentNoRef else po.SupplierMaterialCode end As SupplierMaterialCode,a.DocumentDate,c.DocDetailID,case when a.DocumentTypeID=3 then c.DocDetailID else '' end As RESITEMNO, c.ProductCode,c.ProductName,(sale.totalqty*sale.materialamount) As Qty,(sale.totalqty*sale.materialamount) As SmallQty,c.UnitName, c.UnitName As SmallUnitName,c.ProductPricePerUnit,c.ProductNetPrice,c.ProductTotalPrice,LineNumber,s1.ShopCode As ToShopCode,s2.ShopCode As FromShopCode,a.DueDate,sale.TranKey\r\n from document a left join document b on a.DocumentIDRef = b.DocumentID and a.DocIDRefShopID = b.KeyShopID join docdetail c on a.DocumentID = c.DocumentID and a.KeyShopID = c.KeyShopID inner join (select mg.*,od.TotalQty as totalQty,trankey from orderdetail od inner join MaterialMaster_GIS mg on od.productid=mg.productid where saledate='{docDate}' and shopid={shopId} and OrderStatusID=2) sale on c.productid=sale.MaterialID join shop_data d on a.ShopID = d.ShopID left join vendors e on a.VendorID = e.VendorID left join interface_document_fromsap po on a.DocumentIDRef=po.DocumentID and a.DocIDRefShopID=po.KeyShopID and c.ProductID=po.ProductID left join shop_data s1 on a.ToInvID = s1.ShopID left join shop_data s2 on a.FromInvID = s2.ShopID  where a.DocumentStatus = 2  and a.DocumentKey='{documentKey}' \r\n ) Int_GIS\r\n group by ShopCode,VendorCode, DocumentID,KeyShopID,DocumentKey,POKey,DocumentYear,DocumentMonth,DocumentNo, DocumentNoRef,SupplierMaterialCode,DocumentDate,DocDetailID, RESITEMNO, ProductCode,ProductName,UnitName, SmallUnitName,ProductPricePerUnit,ProductNetPrice,ProductTotalPrice,LineNumber,ToShopCode, FromShopCode,DueDate\r\n order by DocDetailID";
            string queryStr = $"select ShopCode,VendorCode, DocumentID,KeyShopID,DocumentKey,POKey,DocumentYear,DocumentMonth,DocumentNo, DocumentNoRef,SupplierMaterialCode,DocumentDate,DocDetailID, RESITEMNO, ProductCode,MaterialCode,FGProductCode,ProductName,sum(Qty) as Qty ,sum(SmallQty) as SmallQty,UnitName, SmallUnitName,ProductPricePerUnit,ProductNetPrice,ProductTotalPrice,LineNumber,ToShopCode, FromShopCode,DueDate \r\nfrom (\r\n select distinct d.ShopCode,e.VendorCode, a.DocumentID,a.KeyShopID,a.DocumentKey,b.DocumentKey As POKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo, b.DocumentNoRef,case when a.DocumentTypeID=25 and (po.SupplierMaterialCode is null or po.SupplierMaterialCode='') then b.DocumentNoRef else po.SupplierMaterialCode end As SupplierMaterialCode,a.DocumentDate,c.DocDetailID,case when a.DocumentTypeID=3 then c.DocDetailID else '' end As RESITEMNO, c.ProductCode,sale.MaterialCode,sale.productcode As FGProductCode,c.ProductName,(sale.totalqty*sale.materialamount) As Qty,(sale.totalqty*sale.materialamount) As SmallQty,c.UnitName, c.UnitName As SmallUnitName,c.ProductPricePerUnit,c.ProductNetPrice,c.ProductTotalPrice,LineNumber,s1.ShopCode As ToShopCode,s2.ShopCode As FromShopCode,a.DueDate,sale.TranKey\r\n from document a left join document b on a.DocumentIDRef = b.DocumentID and a.DocIDRefShopID = b.KeyShopID join docdetail c on a.DocumentID = c.DocumentID and a.KeyShopID = c.KeyShopID inner join (select mg.*,od.TotalQty as totalQty,od.trankey from orderdetail od inner join MaterialMaster_GIS mg on od.productid=mg.productid inner join ordertransaction tr on od.TranKey=tr.TranKey where od.saledate='{docDate}' and od.shopid={shopId} and OrderStatusID=2 and tr.TransactionStatusID=2) sale on c.productid=sale.MaterialID join shop_data d on a.ShopID = d.ShopID left join vendors e on a.VendorID = e.VendorID left join interface_document_fromsap po on a.DocumentIDRef=po.DocumentID and a.DocIDRefShopID=po.KeyShopID and c.ProductID=po.ProductID left join shop_data s1 on a.ToInvID = s1.ShopID left join shop_data s2 on a.FromInvID = s2.ShopID  where a.DocumentStatus = 2  and a.DocumentKey='{documentKey}' \r\n ) Int_GIS\r\n group by ShopCode,VendorCode, DocumentID,KeyShopID,DocumentKey,POKey,DocumentYear,DocumentMonth,DocumentNo, DocumentNoRef,SupplierMaterialCode,DocumentDate,DocDetailID, RESITEMNO, ProductCode,MaterialCode,FGProductCode,ProductName,UnitName, SmallUnitName,ProductPricePerUnit,ProductNetPrice,ProductTotalPrice,LineNumber,ToShopCode, FromShopCode,DueDate\r\n order by DocDetailID";
            dtL = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            dtL.TableName = "Detail";
            return dtL;
        }
        private async Task<DataTable> GetDocumentRODetail(string documentKey)
        {
            DataTable dtL = new DataTable();

            string queryStr = $"select ROW_NUMBER() OVER (ORDER BY ro.DocDetailID) Row_num,ro.*,wt.InvoiceRef  As EBELN,RIGHT(REPLICATE('0',5) + CAST(rq.DocDetailID * 10 AS VARCHAR(20)), 5) AS EBELP,s.ShopID,s.ShopCode from (select a.DocumentID, a.KeyShopID, a.DocumentKey, a.DocIDRefShopID, a.DocumentIDRef, a.DocumentNo, a.DocumentNoRef, a.InvoiceRef, b.DocDetailID, b.ProductAmount, b.UnitSmallAmount as Qty,b.UnitSmallAmount as SmallQty, b.UnitName, 'EA' As SmallUnitName, b.ProductID, b.ProductCode, b.ProductName, a.DocumentDate, a.ShopID from document a inner join docdetail b on a.DocumentKey= b.DocumentKey where DocumentTypeID = 25  and DocumentStatus = 2) as ro join shop_data s on ro.ShopID = s.ShopID left join(select a.DocumentID, a.KeyShopID, a.DocumentKey, a.DocIDRefShopID, a.DocumentIDRef, a.DocumentNo, a.DocumentNoRef, a.InvoiceRef, b.DocDetailID, b.ProductAmount, b.UnitSmallAmount,b.UnitSmallAmount as SmallQty, b.UnitName, 'EA' As SmallUnitName, b.ProductID, b.ProductCode, b.ProductName, a.DocumentDate from document a inner join docdetail b on a.DocumentKey= b.DocumentKey where DocumentTypeID = 3  and DocumentStatus = 2) as wt on ro.DocumentIDRef = wt.documentid and ro.DocIDRefShopID = wt.KeyShopID and ro.ProductCode = wt.ProductCode left join (select a.DocumentID, a.KeyShopID, a.DocumentKey, a.DocIDRefShopID, a.DocumentIDRef, a.DocumentNo, a.DocumentNoRef, a.InvoiceRef, b.DocDetailID, b.ProductAmount, b.UnitSmallAmount,b.UnitSmallAmount  As SmallQty, b.UnitName, 'EA' As SmallUnitName, b.ProductID, b.ProductCode, b.ProductName, a.DocumentDate from document a inner join docdetail b on a.DocumentKey= b.DocumentKey where DocumentTypeID = 17 and DocumentStatus = 2) as rq on wt.InvoiceRef = rq.DocumentNoRef and wt.ProductCode = rq.ProductCode where ro.DocumentKey = '{documentKey}' order by ro.DocDetailID";
            dtL = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            dtL.TableName = "Detail";
            return dtL;
        }
        private async Task<DataTable> GetRequestDocument(string DocumentNoRef)
        {
            DataTable dtL = new DataTable();

            string queryStr = $"select DocumentKey,DocumentNo,DocumentNoRef,InvoiceRef,DocumentDate from document where DocumentTypeID=17 and documentstatus=2  and DocumentNoRef in(select InvoiceRef from document where DocumentTypeID=3 and DocumentNoRef='{DocumentNoRef}')";
            dtL = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            dtL.TableName = "Detail";
            return dtL;
        }
        private async Task<int> GetVendorID(string vendorCode)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from vendors where VendorCode='{vendorCode}'";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            int id = 0;
            if (dt.Rows.Count > 0)
            {
                id = Convert.ToInt32(dt.Rows[0]["vendorid"]);
            }
            return id;
        }
        private async Task<DataTable> CheckMaterial(string materialCode, string unitName, SqlConnection connection, SqlTransaction transaction)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.MaterialID,a.MaterialCode,a.MaterialName,b.UnitSmallID,d.UnitLargeID,d.UnitLargeName,c.UnitSmallRatio from materials a join unitsmall b on a.UnitSmallID=b.UnitSmallID join unitratio c on b.UnitSmallID=c.UnitSmallID join unitlarge d on c.UnitLargeID=d.UnitLargeID where a.MaterialCode='{materialCode}' and d.UnitLargeName='{unitName}'";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));

            return dt;
        }
        private async Task<DataTable> CheckDoDocDetailID(int DocumentId, int KeyShopId, int DocDetailID, SqlConnection connection, SqlTransaction transaction)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from docdetail where DocumentID={DocumentId} and keyshopId={KeyShopId} and DocDetailID={DocDetailID}";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));

            return dt;
        }
        private async Task<int> InsertDocumentHeader(int documentID, int keyShopID, string documentKey, int vendorID, int vendorGroupID, int documentTypeID, int documentYear, int documentMonth, int documentNumber, string documentNo, string documentNoRef, string documentDate, int shopID, int documentStatus, int documentIDRef, int docIDRefShopID, int toInvID, int fromInvID, decimal subTotal, decimal totalDiscount, decimal totalVAT, decimal netPrice, decimal grandTotal, string remark, int inputBy, int updateBy, int approveBy, int receiveBy, string insertDate, string updateDate, string approveDate, string dueDate, string invoicePODate, int vatPercent, SqlConnection connection, SqlTransaction transaction)
        {
            int id = 0;
            string queryStr = $"insert into document (DocumentID,KeyShopID,DocumentKey,VendorID,VendorGroupID,DocumentTypeID,DocumentYear,DocumentMonth,DocumentNumber,DocumentNo,DocumentNoRef,DocumentDate,ShopID,DocumentStatus,DocumentIDRef,DocIDRefShopID,ToInvID,FromInvID,SubTotal,TotalDiscount,TotalVAT,NetPrice,GrandTotal,Remark,InputBy,UpdateBy,ApproveBy,ReceiveBy,InsertDate,UpdateDate,ApproveDate,DueDate,InvoicePODate,VATPercent) values({documentID},{keyShopID},'{documentKey}',{vendorID},{vendorGroupID},{documentTypeID},{documentYear},{documentMonth},{documentNumber},'{documentNo}','{documentNoRef}',{documentDate},{shopID},{documentStatus},{documentIDRef},{docIDRefShopID},{toInvID},{fromInvID},{subTotal},{totalDiscount},{totalVAT},{netPrice},{grandTotal},'{remark}',{inputBy},{updateBy},{approveBy},{receiveBy},{insertDate},{updateDate},{approveDate},{dueDate},{invoicePODate},{vatPercent});";
            id = await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
            return id;
        }
        private async Task<int> InsertDocumentHeader(int documentID, int keyShopID, string documentKey, int vendorID, int vendorGroupID, int documentTypeID, int documentYear, int documentMonth, int documentNumber, string documentNo, string documentNoRef, string InvoiceRef, string documentDate, int shopID, int documentStatus, int documentIDRef, int docIDRefShopID, int toInvID, int fromInvID, decimal subTotal, decimal totalDiscount, decimal totalVAT, decimal netPrice, decimal grandTotal, string remark, int inputBy, int updateBy, int approveBy, int receiveBy, string insertDate, string updateDate, string approveDate, string dueDate, string invoicePODate, int vatPercent, SqlConnection connection, SqlTransaction transaction)
        {
            int id = 0;
            string queryStr = $"insert into document (DocumentID,KeyShopID,DocumentKey,VendorID,VendorGroupID,DocumentTypeID,DocumentYear,DocumentMonth,DocumentNumber,DocumentNo,DocumentNoRef,InvoiceRef,DocumentDate,ShopID,DocumentStatus,DocumentIDRef,DocIDRefShopID,ToInvID,FromInvID,SubTotal,TotalDiscount,TotalVAT,NetPrice,GrandTotal,Remark,InputBy,UpdateBy,ApproveBy,ReceiveBy,InsertDate,UpdateDate,ApproveDate,DueDate,InvoicePODate,VATPercent) values({documentID},{keyShopID},'{documentKey}',{vendorID},{vendorGroupID},{documentTypeID},{documentYear},{documentMonth},{documentNumber},'{documentNo}','{documentNoRef}','{InvoiceRef}',{documentDate},{shopID},{documentStatus},{documentIDRef},{docIDRefShopID},{toInvID},{fromInvID},{subTotal},{totalDiscount},{totalVAT},{netPrice},{grandTotal},'{remark}',{inputBy},{updateBy},{approveBy},{receiveBy},{insertDate},{updateDate},{approveDate},{dueDate},{invoicePODate},{vatPercent});";
            id = await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
            return id;
        }
        private async Task<int> InsertDocumentDetail(int docDetailID, int documentID, int keyShopID, string documentKey, string documentDate, int shopID, int productID, string productCode, string productName, string supplierMaterialCode, string supplierMaterialName, decimal productAmount, decimal productPricePerUnit, int discountType, decimal productDiscount, decimal discountAmount, decimal productDiscountAmount, decimal productNetPrice, int vatType, string vatCode, decimal productTax, decimal productTotalPrice, decimal unitSmallAmount, int unitSmallID, int unitLargeID, decimal unitRatio, int unitLargeRatio, string unitName, string remark, SqlConnection connection, SqlTransaction transaction)
        {
            int id = 0;
            string queryStr = $"insert into docdetail (DocDetailID,DocumentID,KeyShopID,DocumentKey,DocumentDate,ShopID,ProductID,ProductCode,ProductName,SupplierMaterialCode,SupplierMaterialName,ProductAmount,ProductPricePerUnit ,DiscountType,ProductDiscount,DiscountAmount,ProductDiscountAmount,ProductNetPrice,VATType,VATCode,ProductTax,ProductTotalPrice,UnitSmallAmount,UnitSmallID,UnitLargeID,UnitRatio,UnitLargeRatio,UnitName,POAmount,POSmallAmount,IsDefault,DiscLevelDesc,Remark) values({docDetailID},{documentID},{keyShopID},'{documentKey}',{documentDate},{shopID},{productID},'{productCode}','{productName}','{supplierMaterialCode}','{supplierMaterialName}',{productAmount},{productPricePerUnit} ,{discountType},{productDiscount},{discountAmount},{productDiscountAmount},{productNetPrice},{vatType},'{vatCode}',{productTax},{productTotalPrice},{unitSmallAmount},{unitSmallID},{unitLargeID},{unitRatio},{unitLargeRatio},'{unitName}',{productAmount},{unitSmallAmount},1,0,'{remark}');";
            id = await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
            return id;
        }

        private async Task<int> UpdateDocumentDetail(int docDetailID, int documentID, int keyShopID, decimal productAmount, decimal unitSmallAmount, SqlConnection connection, SqlTransaction transaction)
        {
            int id = 0;
            string queryStr = $"update docdetail set ProductAmount={productAmount}, UnitSmallAmount={unitSmallAmount} where docdetailid={docDetailID} and documentid={documentID} and keyshopid={keyShopID}; ";
            id = await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
            return id;
        }

        public async Task<GOODISSUEINDOCUMENT> SalesOrderAsync(string documentKey, string docTypeCode, string shopCoe)
        {

            GOODISSUEINDOCUMENT data = new GOODISSUEINDOCUMENT();
            DataTable dtH = new DataTable();
            DataTable dtL = new DataTable();

            dtH = await Task.Run(() => GetDocumentHeader(documentKey));


            if (dtH.Rows.Count > 0)
            {

                int docTypeId = Convert.ToInt32(dtH.Rows[0]["DocumentTypeId"]);
                int docNumber = Convert.ToInt32(dtH.Rows[0]["DocumentNumber"]);
                int docShopId = Convert.ToInt32(dtH.Rows[0]["ShopID"]);
                DateTime docDate = Convert.ToDateTime(dtH.Rows[0]["documentdate"]);
                // BSTNK format e.g. 00001GIS20260429
                string docNo = $"{docNumber.ToString().PadLeft(5, '0')}{docTypeCode}{docDate.ToString("yyyyMMdd", invC)}";

                data.POSTYPE = docTypeCode;
                data.POSDOCITEM = docNo;
                data.BLDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                if (docTypeId == 3)
                {
                    data.RESNO = dtH.Rows[0]["documentnoref"].ToString();
                }
                else
                {
                    data.RESNO = "";
                }
                data.BUDAT = Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyyMMdd", invC);
                data.XBLNR = "";
                data.USNAM = dtH.Rows[0]["staffcode"].ToString();
                List<GOODISSUEIN_ITEMS> items = new List<GOODISSUEIN_ITEMS>();
                dtL = await Task.Run(() => GetDocumentDetail_SaleOrder(documentKey, docShopId, Convert.ToDateTime(dtH.Rows[0]["documentdate"]).ToString("yyyy-MM-dd", invC)));
                if (dtL.Rows.Count > 0)
                {
                    items = (from DataRow dr in dtL.Rows
                             select new GOODISSUEIN_ITEMS()
                             {
                                 POSGIITEMNO = dr["DocDetailID"].ToString(),
                                 RESITEMNO = dr["RESITEMNO"].ToString(),
                                 MATNR = dr["MaterialCode"].ToString(),
                                 SWERKS = dr["ShopCode"].ToString(),
                                 RWERKS = dr["ToShopCode"].ToString(),
                                 //MENGE = dr["Qty"].ToString(),
                                 MENGE = Convert.ToDecimal(dr["Qty"]).ToString("0.000"),
                                 MEINS = dr["UnitName"].ToString(),
                                 SGTXT = dr["FGProductCode"].ToString(),
                             }).ToList();
                }
                data.toITEMS = items;
            }
            return data;
        }

        public async Task<DataTable> GetGisDocumentsForDailySale(int shopId, string saleDate)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,st.ShopCode,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,a.DocumentNumber,a.DocumentDate,a.documenttypeid from document a inner join shop_data st on a.shopid=st.shopid where a.DocumentStatus=2 and a.DocumentTypeID=20 and a.ShopID={shopId} and a.DocumentDate={saleDate};";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> GetProgramPropertyValue(int properID)
        {
            DataTable dtH = new DataTable();
            string queryStr = $"select PropertyID,PropertyValue,PropertyTextValue from programpropertyvalue where PropertyID={properID}";
            dtH = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
            dtH.TableName = "programpropertyvalue";
            return dtH;
        }

        #endregion
    }

}
