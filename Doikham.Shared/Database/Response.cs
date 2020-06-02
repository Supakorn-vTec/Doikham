using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
    public class RESPONSEDATA
    {
        public RESPONSE RESPONSE { get; set; }
    }
    public class RESPONSE
    {
        public string TYPE { get; set; }
        public string DOC_NO { get; set; }
        public string MESSAGE { get; set; }
        public string PIMSGID { get; set; }
    }
}
