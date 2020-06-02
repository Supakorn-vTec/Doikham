using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
    public class PURCHASEDOCUMENT
    {
        public PURCHASE_ORDER PURCHASE_ORDER { get; set; }
    }
    public class PURCHASE_ORDER
    {
        public PURCHASE_HEADER HEADER { get; set; }
        public string USERNAME { get; set; }
        public string SENTDATE { get; set; }
        public string SENTTIME { get; set; }
        public string PIMSGID { get; set; }
    }
    public class PURCHASE_HEADER
    {
        public string EBELN { get; set; }
        public string LIFNR { get; set; }
        public List<PURCHASE_ITEMS> ITEMS { get; set; }
    }
    public class PURCHASE_ITEMS
    {
        public string EBELP { get; set; }
        public string MATNR { get; set; }
        public string WERKS { get; set; }
        public decimal MENGE { get; set; }
        public string MEINS { get; set; }
        public string NETPR { get; set; }
        public string NETWR { get; set; }
        public string ELIKZ { get; set; }
        public List<SCHEDULE_LINE> SCHEDULE_LINE { get; set; }
    }
}
