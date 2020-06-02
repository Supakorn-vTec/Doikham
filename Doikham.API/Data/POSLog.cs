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
        public Task<string> SetLog(string tranKey, int shopID, string docDate, int docType, string statusCode, string msgLog);
        public Task<int> SetResponseLog(string uuid, string tranKey, int shopID, int docType, string statusCode, string msgLog);
        public Task<DataTable> GetLogForResend(string uuid);
        public Task<int> SetDocumentRefFromSAP(string tranKey, string refKey);
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
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNoRef,a.DocumentDate from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey and c.DocType={documentTypeId} where a.DocumentStatus=2 and a.DocumentTypeID={documentTypeId} and c.UUID is null;";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetLog()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where IsAddReduceDoc=1) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2  and c.UUID is null union select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNo As DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID=3) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2 and a.ShopID<>1 and a.DocumentTypeID=3  and c.UUID is null  union select a.ShopId,a.DocumentID,a.KeyShopID,a.DocumentKey,a.DocumentYear,a.DocumentMonth,a.DocumentNo,b.DocumentNo As DocumentNoRef,a.DocumentDate,d.DocumentTypeHeader,d.MovementInStock As DocTypeCode,a.DocumentTypeID from document a left join document b on a.DocumentIDRef=b.DocumentID and a.DocIDRefShopID=b.KeyShopID left join SAPBOne_Interface_Log c on a.ShopID=c.ShopID and a.DocumentKey=c.TranKey join (select DocumentTypeID,DocumentTypeHeader,DocumentTypeName,MovementInStock from documenttype where DocumentTypeID=1002) d on a.DocumentTypeID=d.DocumentTypeID where a.DocumentStatus=2 and a.DocumentTypeID=1002  and c.UUID is null";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<DataTable> GetLogForResend(string uuid)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from SAPBOne_Interface_Log where UUID='{uuid}';";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
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
            string queryStr = $"update Document set DocumentNoRef='{refKey}' where documentkey='{tranKey}'";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connString));
        }
        #endregion
    }
}
