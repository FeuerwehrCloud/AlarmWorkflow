using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlarmWorkflow.Website.Asp
{
    public class csFzList
    {
       public string sFzBez;
       public string sTyp;
       public string sNr;
       public int iAngefordert;
       public int iAAO;

       public csFzList(string FzBez, string Typ, string Nr, int angefordert, int AAO)
        {
            this.sFzBez = FzBez; this.sTyp = Typ; this.sNr = Nr; this.iAngefordert = angefordert; this.iAAO = AAO;
        }

    }
}