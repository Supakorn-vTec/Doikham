using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
    public class InterfaceLog
    {
        public string UUID { get; set; }
        public string TranKey { get; set; }
        public int ShopID { get; set; }
        public DateTime? DocDate { get; set; }
        public int DocType { get; set; }
        public int DocTypeGroup { get; set; }
        public string StatusCode { get; set; }
        public string MsgLog { get; set; }
        public DateTime? DateTimeStamp { get; set; }
        public string ResMsgLog { get; set; }
        public DateTime? ResDatetimeStamp { get; set; }
        public string ResStatus { get; set; }
        public string DocumentNo { get; set; }
        public string DocumentNoRef { get; set; }
        public string InvoiceRef { get; set; }
    }
}
