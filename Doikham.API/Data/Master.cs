using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Doikham.API.Data
{
    public interface IMaster
    {
        public Task<bool> SyncMaster();
    }


    public class Master : IMaster
    {

        private readonly IConfiguration _config;
        private IDBHelper _dbHelper;
        private string connString = "";
        private string connStringSAP = "";
        CultureInfo invC;
        public Master(IConfiguration configuration, IDBHelper dBHelper)
        {
            _config = configuration;
            _dbHelper = dBHelper;
            connString = _config.GetSection("Database")["ConnectionString"];
            connStringSAP = _config.GetSection("Database")["ConnectionStringSAP"];
            invC = new CultureInfo("en-US");
        }

        public async Task<bool> SyncMaster()
        {
            DataTable dtExTime = new DataTable();
            dtExTime = await Task.Run(() => GetLastSync(connString));
            DateTime dateTime = Convert.ToDateTime(dtExTime.Rows[0]["LastSync"]);
            string lastSyncDate = dateTime.ToString("yyyy-MM-dd", invC);

            /// Get Data From SAP
            string tableName = "SAPBOne_MasterData";
            DataTable dtMaster = new DataTable();
            dtMaster = await Task.Run(() => GetItemMasterFromSAP(connStringSAP));
            dtMaster.TableName = tableName;

            DataTable dtPrice = new DataTable();
            string tablePriceName = "SAPBOne_MasterPriceData";
            dtPrice = await Task.Run(() => GetPriceMasterFromSAP(connStringSAP, lastSyncDate));
            dtPrice.TableName = tablePriceName;

            DataTable dtPeriodDate = new DataTable();
            string tablePeriodDateName = "PeriodDate";
            dtPeriodDate = await Task.Run(() => GetPeriodDateForPriceListFromSAP(connStringSAP));
            dtPeriodDate.TableName = tablePeriodDateName;

            /// Import Master To POS 
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
                    //Create Products
                    await TruncateTable(tableName, connection, transaction);
                    await CreateTempData(dtMaster, connection, transaction);
                    await CreateProductGroup(connection, transaction);
                    await CreateProductDept(connection, transaction);
                    await CreateProduct(connection, transaction);
                    await CreateProductBarcode(connection, transaction);
                    await TruncateTable(tablePriceName, connection, transaction);
                    await CreateTempData(dtPrice, connection, transaction);
                    await CreateProductPrice(dtPeriodDate, connection, transaction);

                    //Create Materials
                    await CreateMaterial(connection, transaction);

                    //Create BOM
                    await CreateProductComponent(connection, transaction);

                    transaction.Commit();
                    connection.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
            return true;
        }

        private async Task<DataTable> GetItemMasterFromSAP(string connection)
        {
            DataTable dt = new DataTable();
            //string queryStr = $"select CONCAT(a.MATNR,'-',b.MEINH) As ProductCode, a.MATNR As ProductName7,a.MAKTX As ProductName,a.MATKL As ProductDeptCode,b.EAN11 As ProductName6,c.UnitRatio As ProductName8,a.MTART As ProductName9 ,b.MEINH As ProductUnitName,0 As ProductTypeID,1 As VATType, case when d.TAXM1 is null then 'V' when d.TAXM1 = 0 then 'N' else 'V' end As VATCode, 7 As VATPercent,case when a.LVORM ='X' then 0 else 1 end As Activate,1 As IsRetail,a.MEINS As ProductName10 from TBM_MM_MATERIAL a inner join TBM_MM_MATERIAL_UOM b on a.MATNR=b.MATNR inner join (select a.MATNR,a.MEINH,CASE WHEN b.NewUMREN is NULL THEN (a.UMREZ/a.UMREN) ELSE (a.UMREZ*b.NewUMREN)/a.UMREN END As UnitRatio,b.NewUMREN from TBM_MM_MATERIAL_UOM a left outer join (select MATNR,MAX(UMREN) As NewUMREN from TBM_MM_MATERIAL_UOM where UMREN>1 group by MATNR) b on a.MATNR=b.MATNR ) c on b.MATNR=c.MATNR and b.MEINH=c.MEINH left join (select MATNR,TAXM1 from TBM_MM_SALES_TAX Group by MATNR,TAXM1) d on a.MATNR=d.MATNR order by a.MATNR,c.UnitRatio";
            string queryStr = $"select CONCAT(a.MATNR,'-',b.MEINH) As ProductCode, a.MATNR As ProductName7,a.MAKTX As ProductName,a.ZMAT_GRP_CODE As ProductGroupCode,a.ZMAT_GRP_NAME As ProductGroupName,a.ZMAT_DEPT_CODE As ProductDeptCode,a.ZMAT_DEPT_NAME As ProductDeptName,b.EAN11 As ProductName6,c.UnitRatio As ProductName8,a.MTART As ProductName9 ,b.MEINH As ProductUnitName,0 As ProductTypeID,1 As VATType, case when d.TAXM1 is null then 'V' when d.TAXM1 = 0 then 'N' else 'V' end As VATCode, 7 As VATPercent,case when a.LVORM ='X' then 0 else 1 end As Activate,1 As IsRetail,a.MEINS As ProductName10 from TBM_MM_MATERIAL a inner join TBM_MM_MATERIAL_UOM b on a.MATNR=b.MATNR inner join (select a.MATNR,a.MEINH,CASE WHEN b.NewUMREN is NULL THEN (a.UMREZ/a.UMREN) ELSE (a.UMREZ*b.NewUMREN)/a.UMREN END As UnitRatio,b.NewUMREN from TBM_MM_MATERIAL_UOM a left outer join (select MATNR,MAX(UMREN) As NewUMREN from TBM_MM_MATERIAL_UOM where UMREN>1 group by MATNR) b on a.MATNR=b.MATNR ) c on b.MATNR=c.MATNR and b.MEINH=c.MEINH left join (select MATNR,TAXM1 from TBM_MM_SALES_TAX Group by MATNR,TAXM1) d on a.MATNR=d.MATNR order by a.MATNR,c.UnitRatio";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection));
        }
        private async Task<DataTable> GetPriceMasterFromSAP(string connection, string lastSync)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select aa.ShopID,aa.ProductID,aa.ProductCode,aa.ProductPrice,aa.FromDate,aa.ToDate from (select 2 As ShopID, 0 As ProductID, MATNR As ProductCode, KBETR As ProductPrice, DATAB As FromDate, DATBI As ToDate, CONCAT(UDATE, ' ', UTIME) as LastUpdate  from TBM_SD_RETAIL_PRICE where INTERFACE_ON >= '{lastSync}' group by MATNR, KBETR, INTERFACE_TIME, DATAB, DATBI, UDATE, UTIME) aa inner join(select MATNR As ProductCode, max(CONCAT(UDATE, ' ', UTIME)) As LastUpdate  from TBM_SD_RETAIL_PRICE where INTERFACE_ON >= '{lastSync}' group by MATNR) bb on aa.ProductCode = bb.ProductCode and aa.LastUpdate = bb.LastUpdate";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection));
        }
        private async Task<DataTable> GetPeriodDateForPriceListFromSAP(string connection)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select DATAB,DATBI from TBM_SD_RETAIL_PRICE group by DATAB,DATBI";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection));
        }
        private async Task<DataTable> GetLastSync(string connection)
        {
            DataTable dt = new DataTable();
            string queryStr = $"select * from SAPBOne_Interface_SyncLog;";
            return dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection));
        }

        private async Task CreateTempData(DataTable dt, SqlConnection connection, SqlTransaction transaction)
        {
            string tableName = dt.TableName.ToString();
            string queryStr = "";
            if (dt.Rows.Count > 0)
            {
                for (int n = 0; n < dt.Rows.Count; n++)
                {
                    queryStr = $"insert into {tableName}(";
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (i != 0)
                            queryStr += $",{dt.Columns[i].ColumnName}";
                        else
                            queryStr += $"{dt.Columns[i].ColumnName}";
                    }
                    queryStr += ")values(";
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        string col = dt.Columns[i].ColumnName;
                        //FromDate,ToDate
                        if (col == "FromDate" || col == "ToDate")
                        {
                            DateTime xDate = Convert.ToDateTime(dt.Rows[n][col]);
                            if (i != 0)
                                queryStr += $",'{xDate.ToString("yyyy-MM-dd", invC)}'";
                            else
                                queryStr += $"'{xDate.ToString("yyyy-MM-dd", invC)}'";
                        }
                        else
                        {
                            if (i != 0)
                                queryStr += $",'{dt.Rows[n][col]}'";
                            else
                                queryStr += $"'{dt.Rows[n][col]}'";
                        }

                    }
                    queryStr += ")";
                    await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
                }
            }
        }
        private async Task<int> TruncateTable(string tableName, SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = $"truncate table {tableName}";
            return await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
        }
        private async Task DropTable(string tableName, SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = $"IF EXISTS(SELECT * FROM   dbo.{tableName}) DROP TABLE dbo.{tableName}";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
        }
        private async Task CreateTable(DataTable dt, SqlConnection connection, SqlTransaction transaction)
        {
            var tablename = dt.TableName.ToString();
            string queryStr = "";
            queryStr = "IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[" + tablename + "]') AND type in (N'U'))";
            queryStr += "BEGIN ";
            queryStr += "create table " + tablename + "";
            queryStr += "(";
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i != dt.Columns.Count - 1)
                    queryStr += dt.Columns[i].ColumnName + " " + "varchar(max)" + ",";
                else
                    queryStr += dt.Columns[i].ColumnName + " " + "varchar(max)";
            }
            queryStr += ") ";
            queryStr += "END;";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
        }
        private async Task CreateProductDept(SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = "";
            queryStr = "insert into productdept(ProductDeptID,ProductGroupID,ProductDeptCode,ProductDeptName,ShopID,ProductDeptActivate) select ((select case when max(ProductDeptID) is null then 0 else max(ProductDeptID) end from productdept) + ROW_NUMBER() OVER(ORDER BY bb.ProductDeptID)) AS ProductDeptID,((select case when max(ProductDeptID) is null then 0 else max(ProductDeptID) end from productdept) + ROW_NUMBER() OVER(ORDER BY bb.ProductDeptID)) AS ProductGroupID,aa.ProductDeptCode, aa.ProductDeptName As ProductDeptName, 2 As ShopID,1 As ProductDeptActivate  from  (select ProductDeptCode,ProductDeptName from SAPBOne_MasterData group by ProductDeptCode,ProductDeptName) aa left join productdept bb on aa.ProductDeptCode=bb.ProductDeptCode where bb.ProductDeptCode is null";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "update bb set bb.ProductDeptName = aa.ProductDeptName from productdept bb join  SAPBOne_MasterData aa on aa.ProductDeptCode=bb.ProductDeptCode";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "update aa set aa.ProductDeptID=bb.ProductDeptID from SAPBOne_MasterData aa join  productdept bb on aa.ProductDeptCode=bb.ProductDeptCode";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "delete from materialdept; insert into materialdept(MaterialDeptID,MaterialGroupID,MaterialDeptCode,MaterialDeptName,Deleted) select ProductDeptID,ProductGroupID,ProductDeptCode,ProductDeptName,0 from productdept;";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task CreateProductGroup(SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = "";
            queryStr = "insert into productgroup(ProductGroupID,ProductGroupCode,ProductGroupName,ShopID,ProductGroupActivate) select((select case when max(ProductGroupID) is null then 0 else max(ProductGroupID) end from productgroup) +ROW_NUMBER() OVER(ORDER BY bb.ProductGroupId)) AS ProductGroupID, aa.ProductGroupName, aa.ProductGroupName, 2 As ShopID,1 As ProductDeptActivate  from(select ProductGroupCode,ProductGroupName from SAPBOne_MasterData group by ProductGroupCode,ProductGroupName) aa left join productgroup bb on aa.ProductGroupCode = bb.ProductGroupCode where bb.ProductGroupCode is null";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "UPDATE bb SET bb.ProductGroupName = aa.ProductGroupName FROM ProductGroup AS bb INNER JOIN SAPBOne_MasterData AS aa ON bb.ProductGroupCode = aa.ProductGroupCode";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "update aa set aa.ProductGroupID=bb.ProductGroupID from SAPBOne_MasterData aa join  productgroup bb on aa.ProductGroupCode=bb.ProductGroupCode";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "delete from materialgroup ; insert into materialgroup(MaterialGroupID,MaterialGroupCode,MaterialGroupName,Deleted) select ProductGroupID,ProductGroupCode,ProductGroupName,Deleted from productgroup;";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task CreateProduct(SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = "";
            queryStr = "insert into products(ProductID,ShopID,InventoryID,ProductGroupID,ProductDeptID,ProductCode,ProductName,ProductName1,ProductName6,ProductName7,ProductName8,SaleMode1,SaleMode2,VATType,VATCode,ProductUnitName,ProductActivate,DiscountAllow,InsertDate,UpdateDate,IsRetail,ProductName9,ProductName10) select((select case when max(ProductId) is null then 0 else max(ProductId) end from products) +ROW_NUMBER() OVER(ORDER BY aa.ProductId)) AS ProductID,2 As ShopID, 2 As InventoryID, aa.ProductGroupID,aa.ProductDeptID,aa.ProductCode,CONCAT(aa.ProductUnitName,'-', aa.ProductName) As ProductName, aa.ProductName As ProductName1, aa.ProductName6,aa.ProductName7,aa.ProductName8,1 As SaleMode1,1 As SaleMode2, aa.VATType,aa.VATCode,aa.ProductUnitName, 1 As ProductActivate, 1 As DiscountAllow, GETDATE() As InsertDate, GETDATE() As UpdateDate,1 As IsRetail,aa.ProductName9,aa.ProductName10 from SAPBOne_MasterData aa left join products bb on aa.ProductCode = bb.ProductCode where bb.ProductID is null  AND aa.ProductGroupID IS NOT NULL AND aa.ProductDeptID IS NOT NULL";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "update aa set aa.ProductID=bb.ProductID from SAPBOne_MasterData aa join  products bb on aa.ProductCode=bb.ProductCode";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "update aa set aa.ProductName=CONCAT(bb.ProductUnitName,'-', bb.ProductName),aa.ProductName1=bb.ProductName,aa.ProductName6=bb.ProductName6,aa.ProductName7=bb.ProductName7,aa.ProductName8=bb.ProductName8,aa.VATCode=bb.VATCode,aa.ProductActivate=bb.Activate,aa.ProductName9=bb.ProductName9,aa.ProductName10=bb.ProductName10 from products aa  join SAPBOne_MasterData bb on aa.productid=bb.productid";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "update Products set ProductActivate=0 where (ProductName6 is null or ProductName6 ='')";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task CreateProductPrice(DataTable dtPeriodDate, SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = "";

            int priceGroupDateID = 1;
            DataTable dtMaxId = new DataTable();
            queryStr = $"select case when max(PriceGroupDateID) is null then 1 else max(PriceGroupDateID)+1 End As PriceGroupDateID from productpricegroupdate";
            dtMaxId = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
            priceGroupDateID = Convert.ToInt32(dtMaxId.Rows[0]["PriceGroupDateID"]);
            if (dtPeriodDate.Rows.Count > 0)
            {
                for (int i = 0; i < dtPeriodDate.Rows.Count; i++)
                {
                    DataTable chkPe = new DataTable();
                    DateTime fDate = Convert.ToDateTime(dtPeriodDate.Rows[i]["DATAB"]);
                    DateTime tDate = Convert.ToDateTime(dtPeriodDate.Rows[i]["DATBI"]);

                    queryStr = $"select * from productpricegroupdate where FromDate='{fDate.ToString("yyyy-MM-dd", invC)}' and ToDate ='{tDate.ToString("yyyy-MM-dd", invC)}' and Deleted=0;";
                    chkPe = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                    if (chkPe.Rows.Count == 0)
                    {
                        queryStr = $"insert into productpricegroupdate(PriceGroupDateID,FromDate,ToDate,Deleted)values({priceGroupDateID},'{fDate.ToString("yyyy-MM-dd", invC)}','{tDate.ToString("yyyy-MM-dd", invC)}',0)";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
                        priceGroupDateID = priceGroupDateID + 1;
                    }
                }
            }

            queryStr = $"delete from productpricegroupshop where ShopID in(select ShopID from shop_data where ShopCatID2=1) and PriceGroupID=1 ; insert into productpricegroupshop(PriceGroupID,ShopID) select 1 As PriceGroupID,ShopID from shop_data where ShopID>2 and ShopID in(select ShopID from shop_data where ShopCatID2=1);";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = $" select * from productpricegroupdate";
            var dtPriceGroup = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
            for(int i =0; i < dtPriceGroup.Rows.Count; i++)
            {
                queryStr = $"delete aa from productpricegroupdata aa inner join (select aa.PriceGroupDateID, 1 As PriceGroupID, CONCAT(aa.PriceGroupDateID, PriceShopId, ProductID, 1) As ProductPriceID, ProductID, ProductPrice,1 As MainPrice,1 As Salemode from (select cc.PriceGroupDateID, bb.ProductID, aa.ShopID As PriceShopId,bb.ProductCode,(aa.ProductPrice * CAST(bb.Productname8 As decimal) ) As ProductPrice,cc.FromDate,cc.ToDate from SAPBOne_MasterPriceData aa join products bb on aa.productcode=bb.ProductName7 join productpricegroupdate cc on aa.fromdate=cc.fromdate and aa.todate=cc.ToDate ) aa where aa.ProductPrice is not null and aa.ProductID is not null and cast(aa.ProductPrice as decimal )> 0 and aa.PriceGroupDateID={dtPriceGroup.Rows[i]["PriceGroupDateID"]} ) bb on aa.PriceGroupID=bb.PriceGroupID and aa.ProductID=bb.ProductID  and aa.PriceGroupID=1";
                await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                queryStr = $"insert into productpricegroupdata(PriceGroupDateID,PriceGroupID,ProductPriceID,ProductID,ProductPrice,MainPrice,Salemode) select aa.PriceGroupDateID, 1 As PriceGroupID, CONCAT(aa.PriceGroupDateID, 2, ProductID, 1) As ProductPriceID, ProductID, ProductPrice,1 As MainPrice,1 As Salemode from (select cc.PriceGroupDateID, bb.ProductID, aa.ShopID As PriceShopId,bb.ProductCode,(aa.ProductPrice * CAST(bb.Productname8 As decimal) ) As ProductPrice,cc.FromDate,cc.ToDate from SAPBOne_MasterPriceData aa join products bb on aa.productcode=bb.ProductName7 join productpricegroupdate cc on aa.fromdate=cc.fromdate and aa.todate=cc.ToDate ) aa where aa.ProductPrice is not null and aa.ProductID is not null and cast(aa.ProductPrice as decimal )> 0 and aa.PriceGroupDateID={dtPriceGroup.Rows[i]["PriceGroupDateID"]} group by PriceGroupDateID,ProductID,productprice";
                await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
            }

            queryStr = $"update aa set aa.ProductActivate=bb.ProductActivated from products aa join (select a.ProductID, case when b.ProductPrice is null then 0 else 1 end As ProductActivated from products a left join productpricegroupdata b on a.ProductID = b.ProductID) bb on aa.ProductID = bb.ProductID";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task CreateMaterial(SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = "";
            queryStr = "insert into materials(MaterialID,MaterialGroupID,MaterialDeptID,MaterialCode,MaterialName,MaterialTypeID,MaterialTaxType,UnitSmallID,InsertDate,UpdateDate,TaxCode,VATPercent) select((select case when max(MaterialId) is null then 0 else max(MaterialId) end from materials) +ROW_NUMBER() OVER(ORDER BY aa.ProductName7)) AS MaterialId, ProductGroupID As MaterialGroupId, ProductDeptID As MaterialDeptId, ProductName7 As MaterialCode, ProductName As MaterialName,1 As MaterialTypeId,1 as MaterialTaxType,0 UnitSmallId,GETDATE() As InsertDate, GETDATE() As UpdateDate,'V' As TaxCode,7 As VATPercent from SAPBOne_MasterData aa left join materials bb on aa.ProductName7 = bb.MaterialCode  where bb.MaterialID is null group by aa.ProductGroupID,aa.ProductDeptID,aa.ProductName7,aa.ProductName";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "update aa set aa.Deleted=bb.Deleted,aa.MaterialName=bb.MaterialName from materials aa join (select aa.MaterialID, aa.MaterialCode,bb.ProductName As MaterialName, case when bb.Activate = 1 then 0 else 1 end As Deleted from  materials aa inner join SAPBOne_MasterData bb on bb.ProductName7 = aa.MaterialCode group by aa.MaterialID,aa.MaterialCode,bb.Activate,bb.ProductName) bb on aa.MaterialID = bb.MaterialID";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            DataTable dt = new DataTable();
            queryStr = $"select  bb.MaterialId,aa.ProductName7 As MaterialCode,bb.UnitSmallID from SAPBOne_MasterData aa inner join materials bb on aa.ProductName7=bb.MaterialCode  group by aa.ProductGroupID,aa.ProductDeptID,aa.ProductName7,aa.ProductName,bb.MaterialId,bb.UnitSmallID order by bb.MaterialID";
            dt = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int unitsmallId = 0;
                    int materialId = 0;
                    int unitlargeId = 0;
                    int unitId = 0;
                    decimal unitsmallRatio = 0;
                    string materialCode = "";
                    string unitName = "";
                    string barcode = "";
                    unitsmallId = Convert.ToInt32(dt.Rows[i]["UnitSmallID"]);
                    materialId = Convert.ToInt32(dt.Rows[i]["MaterialId"]);
                    materialCode = dt.Rows[i]["MaterialCode"].ToString();

                    if (unitsmallId == 0)
                    {
                        DataTable dtSmall = new DataTable();
                        queryStr = $"select ProductName7 As MaterialCode,ProductUnitName As UnitName,ProductName8 As UnitSmallRatio,ProductName6 As Barcode from SAPBOne_MasterData where ProductName7='{materialCode}' and ProductName8=(select MIN(ProductName8) As Ratio from SAPBOne_MasterData where ProductName7 = '{materialCode}')";
                        dtSmall = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                        unitName = dtSmall.Rows[0]["UnitName"].ToString();
                        unitsmallRatio = Convert.ToDecimal(dtSmall.Rows[0]["UnitSmallRatio"]);
                        barcode = dtSmall.Rows[0]["Barcode"].ToString();

                        //Unitsmall
                        DataTable dtMaxId = new DataTable();
                        queryStr = $"select case when max(unitsmallid) is null then 1 else max(unitsmallid)+1 end As UnitSmallID from unitsmall";
                        dtMaxId = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                        unitsmallId = Convert.ToInt32(dtMaxId.Rows[0]["UnitSmallID"]);
                        queryStr = $"insert into unitsmall(UnitSmallID,UnitSmallName)values({unitsmallId},'{unitName}');";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                        //Unitlarge
                        dtMaxId = new DataTable();
                        queryStr = $"select case when max(UnitLargeID) is null then 1 else max(UnitLargeID)+1 end As UnitLargeID from UnitLarge";
                        dtMaxId = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                        unitlargeId = Convert.ToInt32(dtMaxId.Rows[0]["UnitLargeID"]);
                        queryStr = $"insert into UnitLarge(UnitLargeID,UnitLargeName)values({unitlargeId},'{unitName}');";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                        //Unitratio
                        dtMaxId = new DataTable();
                        queryStr = $"select case when max(UnitID) is null then 1 else max(UnitID)+1 end As UnitID from unitratio";
                        dtMaxId = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                        unitId = Convert.ToInt32(dtMaxId.Rows[0]["UnitID"]);
                        queryStr = $"insert into unitratio(UnitID,UnitLargeID,UnitSmallID,UnitSmallRatio,UnitLargeRatio,MaterialUnitRatioCode)values({unitId},{unitlargeId},{unitsmallId},{unitsmallRatio},1,'{barcode}');";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                        queryStr = $"update materials set UnitSmallID={unitsmallId} where materialId={materialId}";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
                    }

                    DataTable dtUnit = new DataTable();
                    queryStr = $"select ProductName7 As MaterialCode,ProductUnitName As UnitName,ProductName8 As UnitSmallRatio,ProductName6 As Barcode from SAPBOne_MasterData where ProductName7='{materialCode}'";
                    dtUnit = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));

                    for (int n = 0; n < dtUnit.Rows.Count; n++)
                    {
                        unitName = dtUnit.Rows[n]["UnitName"].ToString();
                        unitsmallRatio = Convert.ToDecimal(dtUnit.Rows[n]["UnitSmallRatio"]);
                        barcode = dtUnit.Rows[n]["Barcode"].ToString();

                        DataTable chk = new DataTable();
                        queryStr = $"select c.UnitID, b.UnitSmallID,d.UnitLargeID from materials a join unitsmall b on a.UnitSmallID=b.UnitSmallID join unitratio c on b.UnitSmallID=c.UnitSmallID join unitlarge d on c.UnitLargeID=d.UnitLargeID where c.Deleted=0 and d.UnitLargeName='{unitName}' and a.MaterialCode='{materialCode}'";
                        chk = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));

                        if (chk.Rows.Count > 0)
                        {
                            unitlargeId = Convert.ToInt32(chk.Rows[0]["UnitLargeID"]);
                            unitId = Convert.ToInt32(chk.Rows[0]["UnitID"]);
                            unitsmallId = Convert.ToInt32(chk.Rows[0]["UnitSmallID"]);

                            queryStr = $"update unitratio set MaterialUnitRatioCode='{barcode}',UnitSmallRatio={unitsmallRatio} where UnitID={unitId} and UnitLargeID={unitlargeId} and UnitSmallID={unitsmallId}";
                            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
                        }
                        else
                        {
                            //Unitlarge
                            DataTable dtMaxId = new DataTable();
                            queryStr = $"select case when max(UnitLargeID) is null then 1 else max(UnitLargeID)+1 end As UnitLargeID from UnitLarge";
                            dtMaxId = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                            unitlargeId = Convert.ToInt32(dtMaxId.Rows[0]["UnitLargeID"]);
                            queryStr = $"insert into UnitLarge(UnitLargeID,UnitLargeName)values({unitlargeId},'{unitName}');";
                            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                            //Unitratio
                            dtMaxId = new DataTable();
                            queryStr = $"select case when max(UnitID) is null then 1 else max(UnitID)+1 end As UnitID from unitratio";
                            dtMaxId = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                            unitId = Convert.ToInt32(dtMaxId.Rows[0]["UnitID"]);
                            queryStr = $"insert into unitratio(UnitID,UnitLargeID,UnitSmallID,UnitSmallRatio,UnitLargeRatio,MaterialUnitRatioCode)values({unitId},{unitlargeId},{unitsmallId},{unitsmallRatio},1,'{barcode}');";
                            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
                        }

                    }
                }
            }

        }
        private async Task CreateProductBarcode(SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = "";
            queryStr = "delete from products_barcode  where productbarcode in(select productname6 from SAPBOne_MasterData where (productname6 is not null and productname6 <>''))";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

            queryStr = "INSERT INTO products_barcode (ProductID,ProductBarCode) SELECT p.ProductID,aa.ProductName6 FROM SAPBOne_MasterData aa INNER JOIN products p ON aa.ProductCode=p.ProductCode LEFT JOIN products_barcode pb ON pb.ProductID=p.ProductID AND pb.ProductBarCode=aa.ProductName6 WHERE aa.ProductName6 IS NOT NULL AND aa.ProductName6<>'' AND pb.ProductID IS NULL;";
            await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

        }
        private async Task CreateProductComponent(SqlConnection connection, SqlTransaction transaction)
        {
            string queryStr = "";

            queryStr = "SELECT FORMAT(DATEADD(DAY,-1,GETDATE()),'yyyy-MM-dd') AS SaleDateString";
            DataTable chkDate = new DataTable();
            chkDate = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
            string YesterDayDate = Convert.ToString(chkDate.Rows[0]["SaleDateString"]);

            queryStr = "select case when MAX(PGroupID) is null then 1 else MAX(PGroupID)+1 end MaxID from productcomponentgroup";
            DataTable chkMaxId = new DataTable();
            chkMaxId = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
            int PGroupID = Convert.ToInt32(chkMaxId.Rows[0]["MaxID"]);

            queryStr = "select * from pos_interface_product a join pos_interface_material b on a.ProductCode=b.MaterialCodeSAP where SAPGroup<>'ZVS'";
            DataTable dtProd = new DataTable();
            dtProd = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));

            if (dtProd.Rows.Count > 0)
            {
                for (int i = 0; i < dtProd.Rows.Count; i++)
                {
                    int saleModeID = 1;
                    int productId = Convert.ToInt32(dtProd.Rows[i]["ProductID"]);
                    int materialId = Convert.ToInt32(dtProd.Rows[i]["MaterialID"]);
                    decimal ratio = Convert.ToDecimal(dtProd.Rows[i]["Ratio"]);
                    int unitSmallId = Convert.ToInt32(dtProd.Rows[i]["UnitSmallId"]);

                    DataTable chkComp = new DataTable();
                    queryStr = $"select * from productcomponentgroup where EndDate<'9999-01-01' And ProductID={productId} AND SaleMode={saleModeID}";
                    chkComp = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                    for (int j = 0; j < chkComp.Rows.Count; j++)
                    {
                        queryStr = $"update productcomponentgroup set EndDate='{ YesterDayDate}' where ProductID={productId} AND SaleMode={saleModeID }AND PGroupID={ chkComp.Rows[j]["PGroupID"]}";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
                    }

                    chkComp = new DataTable();
                    queryStr = $"select * from productcomponentgroup where EndDate>='9999-01-01' And ProductID={productId} AND SaleMode={saleModeID}";
                    chkComp = await Task.Run(() => _dbHelper.ExecuteReaderAsync(queryStr, connection, transaction));
                    for (int j = 0; j < chkComp.Rows.Count; j++)
                    {
                        queryStr = $"delete from productcomponentgroup where ProductID={productId} AND SaleMode={saleModeID }AND PGroupID={ chkComp.Rows[j]["PGroupID"]}";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                        queryStr = $"delete from productcomponent where ProductID ={ productId} AND SaleMode={saleModeID }AND PGroupID={ chkComp.Rows[j]["PGroupID"]}";
                        await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));
                    }

                    queryStr = $"insert into productcomponentgroup (PGroupID,PGroupTypeID,ProductID,SaleMode,StartDate,EndDate,SetGroupNo,SetGroupName,RequireAddAmountForProduct,MinQty,MaxQty,AddingFromBranch,IsDefault,PackageTypeID,PromotionID,VoucherHeaderID,StaffRoleID,SetGroupText,SetGroupDesp) values ({PGroupID},1,{productId},{ saleModeID},{YesterDayDate},'9999-12-31',0,'',0,0,0,1,1,0,0,0,0,'','')";
                    await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                    queryStr = $"insert into productcomponent (PGroupID,ProductID,SaleMode,MaterialID,MaterialAmount,QtyRatio,UnitSmallID,ShowOnOrder,DataType,FlexibleProductPrice,FlexibleProductIncludePrice,DiscountAmount,DiscountPercent,Ordering,AddingFromBranch)values({PGroupID},{productId},{saleModeID},{materialId},{ratio},1,{unitSmallId},0,1,0,0,0,0,1,1);";
                    await Task.Run(() => _dbHelper.ExecuteNonQuery(queryStr, connection, transaction));

                }
            }

        }

    }
}
