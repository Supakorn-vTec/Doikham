using System;
using System.Collections.Generic;
using System.Text;

namespace Doikham.Shared.Database
{
    public class RETURNTTOSLOC : GOODISSUEIN_ORDER
    {
    }
    public class RETURNTTODC : GOODISSUEIN_ORDER
    {
    }
    public class PREFINISHDOCUMENT : GOODISSUEIN_ORDER
    {
    }
    public class PREFINISHDOCUMENT_GR : GOODISSUEIN_ORDER
    {
    }
    public class STOCKADJUST : GOODISSUEIN_ORDER
    {
    }
    public class GOODISSUEINDOCUMENT : GOODISSUEIN_ORDER
    {
    }
    public class GOODISSUEIN_ORDER
    {
        public string POSTYPE { get; set; }
        public string POSDOCITEM { get; set; }
        public string RESNO { get; set; }
        public string BLDAT { get; set; }
        public string BUDAT { get; set; }
        public string XBLNR { get; set; }
        public string USNAM { get; set; }
        public List<GOODISSUEIN_ITEMS> toITEMS { get; set; }
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
