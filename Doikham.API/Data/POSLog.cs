using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Doikham.API.Data
{
   public interface IPOSLog
    {

        public Task<DataTable> GetLog(int documentTypeId);
        public Task<DataTable> GetLog();
        public Task<DataTable> GetAJLog();
        public Task<DataTable> GetPNLog(int docTypeID);
        public Task<DataTable> GetToDCLog();
        public Task<DataTable> GetToSLOCLog();
        public Task<DataTable> GetLog(int documentTypeId, string documentDate);
        public Task<string> SetLog(string tranKey, int shopID, string docDate, int docType, string statusCode, string msgLog);
        public Task<int> SetResponseLog(string uuid, string tranKey, int shopID, int docType, string statusCode, string msgLog);
        public Task<DataTable> GetLogForResend(string uuid);
        public Task<int> SetDocumentRefFromSAP(string tranKey, string refKey);
        public  Task<DataTable> GetInterfaceDocType();
        public  Task<DataTable> GetInterfaceLog(int shopId, int docType, string fromDate, string toDate);
        public Task<DataTable> GetInterfaceShop();
        public Task<int> DeleteLog(string uuid);
        public Task SetApiLog(string apiName, string apiMethod, string queryParams, string bodyParam, string responseData, short apiStatus, DateTime insertDate, DateTime? finishDate, string errorMessage, int shopId = 0, int computerId = 0, string terminalId = null, int staffId = 0);

    }
    public class POSLog : IPOSLog
    {
        private readonly IConfiguration _config;
        private IDBHelper _dbHelper;
        private string connString = "";
        CultureInfo invC;
        public POSLog(IConfiguration configuration, IDBHelper dBHelper)
        {
            _config = configuration;
            _dbHelper = dBHelper;
            connString = _config.GetSection("Database")["ConnectionString"];
            invC = new CultureInfo("en-US");
        }

        #region "Log Interface"
        public async Task<DataTable> GetLog(int documentTypeId)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNoRef,a.DocumentDate,a.documenttypeid from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey and c.DocType={documentTypeId} where a.DocumentStatus=2 and a.DocumentTypeID={documentTypeId} and c.UUID is null;";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetLog(int documentTypeId,string documentDate)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,st.ShopCode,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNoRef,a.DocumentDate,a.documenttypeid from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey and c.DocType={documentTypeId} inner join shop_data st on a.shopid=st.shopid where a.DocumentStatus=2 and a.DocumentTypeID={documentTypeId} and a.DocumentDate>='{documentDate}' and c.UUID is null;";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetLog()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where IsAddReduceDoc=1) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2  and c.UUID is null union select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNo As DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID=3) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2 and a.ShopID<>1 and a.DocumentTypeID=3  and c.UUID is null  union select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNo As DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID=1002) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2 and a.DocumentTypeID=1002  and c.UUID is null";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetToDCLog()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNo As DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID=3) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2 and  a.DocumentTypeID=3  and a.ToInvID=1  and c.UUID is null";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetToSLOCLog()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNo As DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID=3) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2 and a.ShopID<>1 and a.DocumentTypeID=3 and a.ToInvID<>1  and c.UUID is null";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }

        public async Task<DataTable> GetAJLog()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where IsAddReduceDoc=1) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2  and c.UUID is null";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetPNLog(int docTypeID)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNo As DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID={docTypeID}) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2 and a.DocumentTypeID={docTypeID}  and c.UUID is null";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }

        public async Task<DataTable> GetInterfaceDocType()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where IsAddReduceDoc=1) aa union select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID in(2,3,1002,17,25)";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetInterfaceLog(int shopId,int docType, string fromDate, string toDate)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.*,b.DocumentNo,b.DocumentNoRef,b.InvoiceRef from SAPBOne_Interface_Log a inner join document b on a.TranKey=b.DocumentKey  where a.ShopID={shopId} and a.DocType={docType} and a.DocDate between '{fromDate}' and '{toDate}'";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }

        public async Task<DataTable> GetInterfaceShop()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select ShopID,ShopCode,ShopName from shop_data where Deleted=0 and IsInv=1;";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetLogForResend(string uuid)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from SAPBOne_Interface_Log where UUID='{uuid}';";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<int> DeleteLog(string uuid)
        {
            DataTable dt = new DataTable();
            string queryStr = $"delete from SAPBOne_Interface_Log where UUID='{uuid}';";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connString));
        }
        public async Task<string> SetLog(string tranKey, int shopID, string docDate, int docType, string statusCode, string msgLog)
        {
            var uuid = Guid.NewGuid().ToString();
            string queryStr = $"insert into SAPBOne_Interface_Log(UUID,TranKey,ShopID,DocDate,DocType,StatusCode,MsgLog,DateTimeStamp)values('{uuid}','{tranKey}',{shopID},{docDate},{docType},'{statusCode}','{msgLog}',GETDATE());";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connString));
            return uuid;
        }
        public async Task<int> SetResponseLog(string uuid, string tranKey, int shopID, int docType, string statusCode, string msgLog)
        {
            string queryStr = $"update SAPBOne_Interface_Log set ResMsgLog='{msgLog}',ResStatus='{statusCode}',ResDatetimeStamp=GETDATE() where UUID='{uuid}' and TranKey='{tranKey}' and ShopID={shopID} and DocType={docType}";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connString));
        }
        public async Task<int> SetDocumentRefFromSAP( string tranKey, string refKey)
        {
            string queryStr = $"update Document set DocumentNoRef='{refKey}',ReceiveDate=GETDATE() where documentkey='{tranKey}'";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connString));
        }

        public async Task SetApiLog(string apiName, string apiMethod, string queryParams, string bodyParam, string responseData, short apiStatus, DateTime insertDate, DateTime? finishDate, string errorMessage, int shopId = 0, int computerId = 0, string terminalId = null, int staffId = 0)
        {
            string SqlStr(string value) => value == null ? "NULL" : $"'{value.Replace("'", "''")}'";
            string SqlDate(DateTime value) => $"'{value.ToString("yyyy-MM-dd HH:mm:ss.ffffff", invC)}'";

            var uuid = Guid.NewGuid().ToString();
            string finish = finishDate.HasValue ? SqlDate(finishDate.Value) : "NULL";
            string queryStr = $"insert into log_api(batchuuid,apiname,apimethod,params,bodyparam,responsedata,apistatus,insertdate,finishdate,errormessage,shopid,computerid,terminalid,staffid)" +
                $"values('{uuid}',{SqlStr(apiName)},{SqlStr(apiMethod)},{SqlStr(queryParams)},{SqlStr(bodyParam)},{SqlStr(responseData)},{apiStatus},{SqlDate(insertDate)},{finish},{SqlStr(errorMessage)},{shopId},{computerId},{SqlStr(terminalId)},{staffId});";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connString));
        }
        #endregion
    }
}
