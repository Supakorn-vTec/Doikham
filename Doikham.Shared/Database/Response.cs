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

    public class ActionResultData
    {
        public string ReponseCode { get; set; } = "";
        public string ResponseText { get; set; } = "";
    }

    public class ResultShopData : ActionResultData
    {
        public List<ShopData> Data { get; set; }
    }

    public class ResultDoctypeData : ActionResultData
    {
        public List<DocTypeData> Data { get; set; }
    }
    public class ResultInterfaceData : ActionResultData
    {
        public List<InterfaceLog> Data { get; set; }
    }
}
