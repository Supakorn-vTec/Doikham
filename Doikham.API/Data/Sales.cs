using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doikham.API.Data
{
    public interface ISales
    {
        public  Task<DataTable> GetLog();
        public Task<string> DailySales(int shopId, string saleDate);
        public void WriteFile(string path, string data);
    }
    public class Sales : ISales
    {
        private readonly IConfiguration _config;
        private IDBHelper _dbHelper;
        private string connString = "";
        CultureInfo invC;
        public Sales(IConfiguration configuration, IDBHelper dBHelper)
        {
            _config = configuration;
            _dbHelper = dBHelper;
            connString = _config.GetSection("Database")["ConnectionString"];
            invC = new CultureInfo("en-US");
        }

        public async Task<DataTable> GetLog()
        {
            DataTable dt = new DataTable();
            string queryStr = $"select a.ShopID,a.POS_SHOPID As ShopCode,a.SaleDate,count(*) As total from pos_interface_sales_header a join sessionenddaydetail c on a.ShopID=c.ShopID and a.SaleDate=c.SessionDate  left join SAPBOne_Interface_Log b on  a.ShopID=b.ShopID and a.SaleDate=b.DocDate and b.DocType=8 where c.IsEndDay=1 and b.ShopID is null group by a.ShopID,a.SaleDate,a.POS_SHOPID";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public async Task<string> DailySales(int shopId, string saleDate)
        {
            var sb = new StringBuilder();
            DataTable dtH = new DataTable();
            dtH = await Task.Run(()=> GetHeader(shopId,saleDate));
            if (dtH.Rows.Count > 0)
            {
                for (int i = 0; i < dtH.Rows.Count; i++)
                {
                    string H = $"H|{AppendSpaceToString(dtH.Rows[i]["POS_SHOPID"].ToString(), 20)}|{AppendSpaceToString(dtH.Rows[i]["BSTNK"].ToString(), 35)}|{AppendSpaceToString(dtH.Rows[i]["POS_INVOICE"].ToString(), 16)}|{AppendSpaceToString(dtH.Rows[i]["NAME1"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["NAME2"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["STR_SUPPL1"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["STR_SUPPL2"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["STREET"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["STR_SUPPL3"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["LOCATION"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["CITY2"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["CITY1"].ToString(), 40)}|{AppendSpaceToString(dtH.Rows[i]["POST_CODE1"].ToString(), 10)}|{AppendSpaceToString(dtH.Rows[i]["TAX_ID"].ToString(), 13)}|{AppendSpaceToString(dtH.Rows[i]["BRANCH"].ToString(), 5)}|{AppendSpaceToString(Convert.ToDateTime(dtH.Rows[i]["BSTDK"]).ToString("yyyyMMdd", invC), 8)}|{AppendSpaceToString(Convert.ToDateTime(dtH.Rows[i]["VBAK_ERDAT"]).ToString("yyyyMMdd", invC), 8)}|{AppendSpaceToString(dtH.Rows[i]["PLTYP"].ToString(), 2)}|{AppendSpaceToString(dtH.Rows[i]["EMAIL"].ToString(), 100)}|{AppendSpaceToString(dtH.Rows[i]["POSID"].ToString(), 20)}|{AppendSpaceToString(dtH.Rows[i]["REFNO"].ToString(), 20)}|{AppendSpaceToString(dtH.Rows[i]["SALEMAN"].ToString(), 60)}|{AppendSpaceToString(dtH.Rows[i]["ROUNDING"].ToString(), 11)}|";
                    sb.AppendLine(H);

                    string BSTNK = dtH.Rows[i]["BSTNK"].ToString();
                    string POS_INVOICE = dtH.Rows[i]["POS_INVOICE"].ToString();
                    DataTable dtL = new DataTable();
                    if(POS_INVOICE == "")
                    {
                        dtL = await Task.Run(() => GetDetails(BSTNK));
                    }
                    else
                    {
                        dtL = await Task.Run(() => GetDetails(BSTNK, POS_INVOICE));
                    }
                  
                    for (int j =0; j < dtL.Rows.Count; j++)
                    {
                        string L = $"D|{AppendSpaceToString(dtL.Rows[j]["POSNR_VA"].ToString(),6)}|{AppendSpaceToString(dtL.Rows[j]["MATNR"].ToString(), 18)}|{AppendSpaceToString(dtL.Rows[j]["VBAP_VRKME"].ToString(), 3)}|{AppendSpaceToString(dtL.Rows[j]["ARKTX"].ToString(), 40)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KWMENG"])), 15)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR"])), 11)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR1"])), 11)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR2"])), 11)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR3"])), 11)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR4"])), 11)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR5"])), 11)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR6"])), 11)}|{AppendSpaceToString(DoFormat(Convert.ToDecimal(dtL.Rows[j]["KBETR7"])), 11)}|{AppendSpaceToString(dtL.Rows[j]["FLAG_FREE"].ToString(), 1)}|{AppendSpaceToString(dtL.Rows[j]["VAT_TYPE"].ToString(), 1)}|";
                        sb.AppendLine(L);
                    }
                }
            }
            return sb.ToString();
        }
        private string AppendSpaceToString( string value, int len)
        {
            string padright = value.ToString().PadRight(len, ' ');
            return padright;
        }
        private async Task<DataTable> GetHeader(string tranKey)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from pos_interface_sales_header where TranKey='{tranKey}'";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> GetHeader(int shopId, string saleDate)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from pos_interface_salessummary_header where shopId={shopId} and saleDate={saleDate}";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> GetDetails(string BSTNK)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from pos_interface_salessummary_detail where BSTNK='{BSTNK}' and POS_INVOICE is null";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> GetDetails(string BSTNK, string POS_INVOICE)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from pos_interface_salessummary_detail where BSTNK='{BSTNK}' and POS_INVOICE='{POS_INVOICE}'";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        private async Task<DataTable> GetDetails(int shopId, string saleDate)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from pos_interface_sales_detail where shopId={shopId} and saleDate={saleDate}";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connString));
        }
        public void WriteFile(string path, string data)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(path, true);
                sw.WriteLine(data);
                sw.Flush();
                sw.Close();
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }
        public  string DoFormat(decimal myNumber)
        {
            var s = string.Format("{0:0.00}", myNumber);

            if (s.EndsWith("00"))
            {
                return ((int)myNumber).ToString();
            }
            else
            {
                return s;
            }
        }
    }
}
