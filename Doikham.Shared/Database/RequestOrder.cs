using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
   public class REQUESTDOCUMENT
    {
        public REQUEST_ORDER REQUEST_FORM { get; set; }
    }
    public class REQUEST_ORDER
    {
        public REQUEST_HEADER HEADER { get; set; }
    }
    public class REQUEST_HEADER
    {
        public string POSTYPE { get; set; }
        public string BLDAT { get; set; }
        public string BKTXT { get; set; }
        public string HGTXT { get; set; }
        public List<REQUEST_ITEMS> ITEMS { get; set; }
    }
    public class REQUEST_ITEMS
    {
        public string ZEILE { get; set; }
        public string MATNR { get; set; }
        public string MAKTX { get; set; }
        public string WERKS { get; set; }
        public string SUPPLANT { get; set; }
        public string MENGE { get; set; }
        public string MEINS { get; set; }
        public string DELDATE { get; set; }
        public string SGTXT { get; set; }

    }
}
