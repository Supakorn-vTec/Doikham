using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
    public enum SAPMETHOD
    {
        REQUESTFORM,
        GOODSRECEIPT,
        GOODSISSUE,
        STOCK_ADJUST,
        RECIPES
    }
    public class RESENDDATA
    {
        public string UUID { get; set; } = "";
        public string JSONDATA { get; set; } = "";
        public SAPMETHOD ACTION { get; set; }
    }
}
