using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
   public class GOODISSUEOUTDOCUMENT
    {
        public GOODISSUEOUT_ORDER GOODS_ISSUE_OUT { get; set; }
    }
    public class GOODISSUEOUT_ORDER
    {
        public GOODISSUEOUT_HEADER HEADER { get; set; }
        public string USERNAME { get; set; }
        public string SENTDATE { get; set; }
        public string SENTTIME { get; set; }
        public string PIMSGID { get; set; }
    }
    public class GOODISSUEOUT_HEADER
    {
        public string MBLNR { get; set; }
        public string MJAHR { get; set; }
        public string BLDAT { get; set; }
        public string BUDAT { get; set; }
        public string BKTXT { get; set; }
        public string XBLNR { get; set; }
        public string USNAM { get; set; }
        public List<GOODISSUEOUT_ITEMS> ITEMS { get; set; }
    }
    public class GOODISSUEOUT_ITEMS
    {
        public string ZEILE { get; set; }
        public string BWART { get; set; }
        public string MATNR { get; set; }
        public string WERKS { get; set; }
        public string LGORT { get; set; }
        public string CHARG { get; set; }
        public string MENGE { get; set; }
        public string MEINS { get; set; }
        public string EBELN { get; set; }
        public string EBELP { get; set; }
        public string RSNUM { get; set; }
        public string RSPOS { get; set; }
        public string LFBNR { get; set; }
        public string LFPOS { get; set; }
        public string SJAHR { get; set; }
        public string SGTXT { get; set; }

    }
}
