using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
   public class GOODRECEIPTDOCUMENT
    {
        public string POSTYPE { get; set; } = "";
        public string BLDAT { get; set; } = "";
        public string BKTXT { get; set; } = "";
        public string LFSNR { get; set; } = "";
        public string BUDAT { get; set; } = "";
        public string USNAM { get; set; } = "";
        public List<GOODRECEIPT_ITEMS> toITEMS { get; set; }
    }
    public class GOODRECEIPT_ITEMS
    {
        public string ZEILE { get; set; } = "";
        public string MATNR { get; set; } = "";
        public string WERKS { get; set; } = "";
        public string LIFNR { get; set; } = "";
        public string SWERKS { get; set; } = "";
        public string MENGE { get; set; } = "";
        public string MEINS { get; set; } = "";
        public string NETPR { get; set; } = "";
        public string NETWR { get; set; } = "";
        public string EBELN { get; set; } = "";
        public string EBELP { get; set; } = "";
        public string SGTXT { get; set; } = "";

    }
}
