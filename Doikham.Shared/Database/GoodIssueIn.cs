using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
    public class RETURNTTOSLOC
    {
        public GOODISSUEIN_ORDER TRANSFER_SLOC { get; set; }
    }
    public class RETURNTTODC
    {
        public GOODISSUEIN_ORDER RETURNT_TO_DC { get; set; }
    }
    public class PREFINISHDOCUMENT
    {
        public GOODISSUEIN_ORDER GI_PREFINISH { get; set; }
    }
    public class STOCKADJUST
    {
        public GOODISSUEIN_ORDER STOCK_ADJUST { get; set; }
    }
    public class GOODISSUEINDOCUMENT
    {
        public GOODISSUEIN_ORDER GOODS_ISSUE_IN { get; set; }
    }
    public class GOODISSUEIN_ORDER
    {
        public GOODISSUEIN_HEADER HEADER { get; set; }
    }
    public class GOODISSUEIN_HEADER
    {
        public string POSTYPE { get; set; }
        public string POSDOCITEM { get; set; }
        public string RESNO { get; set; }
        public string BLDAT { get; set; }
        public string BUDAT { get; set; }
        public string XBLNR { get; set; }
        public string USNAM { get; set; }
        public List<GOODISSUEIN_ITEMS> ITEMS { get; set; }
    }
    public class GOODISSUEIN_ITEMS
    {
        public string POSGIITEMNO { get; set; }
        public string RESITEMNO { get; set; }
        public string MATNR { get; set; }
        public string SWERKS { get; set; }
        public string RWERKS { get; set; }
        public string MENGE { get; set; }
        public string MEINS { get; set; }      
        public string SGTXT { get; set; }

    }
}
