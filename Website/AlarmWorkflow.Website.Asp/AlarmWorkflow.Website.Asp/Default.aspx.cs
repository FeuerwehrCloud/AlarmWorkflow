// This file is part of AlarmWorkflow.
// 
// AlarmWorkflow is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// AlarmWorkflow is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AlarmWorkflow.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Xml.XPath;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Diagnostics;
using AlarmWorkflow.Windows.ServiceContracts;
using AlarmWorkflow.Windows.UI.Models;

namespace AlarmWorkflow.Website.Asp
{
    /// <summary>
    /// Logic of the Default-page.
    /// </summary>
    public partial class Default : Page
    {
        #region Fields

        private readonly WebsiteConfiguration _configuration;
        protected String JSScripts;
        protected String OSMCode;

        #endregion

        #region Constructors

        public Default()
        {
            _configuration = WebsiteConfiguration.Instance;
            if (_UpdateTimer != null)
            {
                _UpdateTimer.Interval = WebsiteConfiguration.Instance.UpdateIntervall;
            }
        }

        #endregion

        #region Constants


        private const string OSMHead = "var map;" +
                                       "var layer_mapnik;" +
                                       "var layer_tah;" +
                                       "var layer_markers;" +
                                       "function jumpTo(lon, lat, zoom) {" +
                                       "    var x = Lon2Merc(lon);" +
                                       "    var y = Lat2Merc(lat);" +
                                       "    map.setCenter(new OpenLayers.LonLat(x, y), zoom);" +
                                       "    return false;" +
                                       "}" +
                                       " " +
                                       "function Lon2Merc(lon) {" +
                                       "    return 20037508.34 * lon / 180;" +
                                       "}" +
                                       " " +
                                       "function Lat2Merc(lat) {" +
                                       "    var PI = 3.14159265358979323846;" +
                                       "    lat = Math.log(Math.tan( (90 + lat) * PI / 360)) / (PI / 180);" +
                                       "    return 20037508.34 * lat / 180;" +
                                       "}" +
                                       " " +
                                       "function addMarker(layer, lon, lat) {" +
                                       " " +
                                       "    var ll = new OpenLayers.LonLat(Lon2Merc(lon), Lat2Merc(lat));    " +
                                       " " +
                                       "    var marker = new OpenLayers.Marker(ll); " +
                                       "    layer.addMarker(marker);" +
                                       "}" +
                                       " " +
                                       "function getCycleTileURL(bounds) {" +
                                       "   var res = this.map.getResolution();" +
                                       "   var x = Math.round((bounds.left - this.maxExtent.left) / (res * this.tileSize.w));" +
                                       "   var y = Math.round((this.maxExtent.top - bounds.top) / (res * this.tileSize.h));" +
                                       "   var z = this.map.getZoom();" +
                                       "   var limit = Math.pow(2, z);" +
                                       " " +
                                       "   if (y < 0 || y >= limit)" +
                                       "   {" +
                                       "     return null;" +
                                       "   }" +
                                       "   else" +
                                       "   {" +
                                       "     x = ((x % limit) + limit) % limit;" +
                                       " " +
                                       "     return this.url + z + \"/\" + x + \"/\" + y + \".\" + this.type;" +
                                       "   }" +
                                       "}";

        private const string Tilt = "map.setTilt(45);";

        private const string Traffic = "var trafficLayer = new google.maps.TrafficLayer();" +
                                       "trafficLayer.setMap(map);";

        private const string Showroute = "directionsDisplay.setMap(map);" +
                                         "calcRoute(Home, address);";

        private const string RouteFunc = "function calcRoute(start, end) {" +
                                         "var request = {" +
                                         "origin:start," +
                                         "destination:end," +
                                         "travelMode: google.maps.TravelMode.DRIVING" +
                                         "};" +
                                         "directionsService.route(request, function(result, status) {" +
                                         "if (status == google.maps.DirectionsStatus.OK) {" +
                                         "directionsDisplay.setDirections(result);" +
                                         "}" +
                                         "});" +
                                         "}";

        private const string CenterCoord = "var beachMarker = new google.maps.Marker({" +
                                           "position: dest," +
                                           "map: map" +
                                           "});" +
                                           "map.setCenter(dest);" +
                                           "maxZoomService.getMaxZoomAtLatLng(dest, function(response) {" +
                                           "if (response.status == google.maps.MaxZoomStatus.OK) {" +
                                           "var zoom = Math.round(response.zoom * ZoomLevel);" +
                                           "map.setZoom(zoom);" +
                                           "}" +
                                           "});";

        private const string BeginnHead = "var directionsService = new google.maps.DirectionsService();" +
                                          "var directionsDisplay = new google.maps.DirectionsRenderer();" +
                                          "var map;" +
                                          "var maxZoomService = new google.maps.MaxZoomService();" +
                                          "var geocoder = new google.maps.Geocoder();" +
                                          "function initialize() {";

        private string FFName = "";
        private string PLZ = ""; // zur ermittlung was überörtlich ist
        private string colorFzFax = "Red";
        private string colorFzAAO = "Orange";
        private string colorFzFaxAAO = "Magenta";
        private string showMessenger = "nein";
        private int commentLengh = 160;
        private string shortKeyword = "nein";
        // ArrayList für Fahrzeuge erzeugen
        System.Collections.ArrayList arrFz = new System.Collections.ArrayList();
        System.Collections.ArrayList arrStichwort = new System.Collections.ArrayList();

        #endregion Constants

        #region Methods

        private void SetAlarmDisplay()
        {
            Operation operation;
            GetOperation(Request["id"], out operation);
            SetAlarmContent(operation);
            Dictionary<string, string> result = GetGeocodes(operation.Einsatzort.Street + " " + operation.Einsatzort.StreetNumber + " " +
                                                            operation.Einsatzort.ZipCode + " " + operation.Einsatzort.City);
            if (result == null || result.Count != 2)
            {
                trMap.Visible = false;
            }
            else
            {
                JSScripts = GoogleMaps(operation, result);
                JSScripts += OSM(result);
            }
        }

        ///<summary>
        /// Liefert den Inhalt der Datei zurück.
        ///</summary>
        ///<param name="sFilename">Dateipfad</param>
        public string ReadFile(String sFilename)
        {
            string sContent = "";

            if (File.Exists(sFilename))
            {
                StreamReader myFile = new StreamReader(sFilename, System.Text.Encoding.Default);
                sContent = myFile.ReadToEnd();
                myFile.Close();
            }
            return sContent;
        }

        public void getReferenzParameter()
        {
            string sParamText = ReadFile(@"C:\inetpub\wwwroot\RefParam.txt");
            string[] Param = sParamText.Split('#');
            int l = Param.Length;
            for (int i = 1; i < l; i++)
            {

                //Fahrzeugblock decodieren
                if (Param[i].StartsWith("HomeParameter"))
                {
                    string[] sHomeParam = Param[i].Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int anzHomeParam = sHomeParam.Length;
                    for (int h = 1; h < anzHomeParam; h++)
                    {
                        string[] ParamValue;
                        ParamValue = sHomeParam[h].Split(':');

                        if (ParamValue[0].Trim() == "FFName")
                        { FFName = ParamValue[1].Trim(); }
                        if (ParamValue[0].Trim() == "PLZ")
                        { PLZ = ParamValue[1].Trim(); }
                        if (ParamValue[0].Trim() == "FarbeFzFax")
                        {
                            colorFzFax = ParamValue[1].Trim();
                            colorFzAAO = ParamValue[1].Trim();
                            colorFzFaxAAO = ParamValue[1].Trim();
                        }
                        if (ParamValue[0].Trim() == "FarbeFzAAO")
                        { colorFzAAO = ParamValue[1].Trim(); }
                        if (ParamValue[0].Trim() == "FarbeFzFaxAAO")
                        { colorFzFaxAAO = ParamValue[1].Trim(); }
                        if (ParamValue[0].Trim() == "Mitteiler_anzeigen")
                        { showMessenger = ParamValue[1].Trim(); }
                        if (ParamValue[0].Trim() == "KommentarLaenge")
                        { commentLengh =  Convert.ToInt32(ParamValue[1].Trim()); }
                        if (ParamValue[0].Trim() == "Stichwort_komprimieren")
                        { shortKeyword  = ParamValue[1].Trim(); }

                        //FFName = sHomeParam[1].Trim();
                        //PLZ = sHomeParam[2].Trim();
                        //if (sHomeParam.Length > 3)
                        //{ colorFzFax = sHomeParam[3].Trim(); }
                        //if (sHomeParam.Length > 4)
                        //{ colorFzAAO = sHomeParam[4].Trim(); }
                        //else
                        //{ colorFzAAO = colorFzFax; }
                        //if (sHomeParam.Length > 5)
                        //{ colorFzFaxAAO = sHomeParam[5].Trim(); }
                        //else
                        //{ colorFzFaxAAO = colorFzFax; }

                    }

                }
                //Fahrzeugblock decodieren
                if (Param[i].StartsWith("Fahrzeuge"))
                {
                    arrFz.Clear();
                    string[] sTextfzList = Param[i].Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int anzFz = sTextfzList.Length;
                    if (anzFz == 1)
                    {
                        //FZ_Info.Visible = false;
                                         }
                    else
                    {
                        for (int f = 1; f < anzFz; f++)
                        {
                            string[] sfz;
                            sfz = sTextfzList[f].Split(',');
                            if (sfz[0].Trim().Length > 0)
                            {
                                arrFz.Add(new csFzList(sfz[0].Trim(), sfz[2].Trim(), sfz[1].Trim(), 0, 0));
                            }
                        }
                    }
                          
                }

                //Schlagworte für AAo decodieren
                if (Param[i].StartsWith("Stichworte"))
                {
                    arrStichwort.Clear();
                    string[] sTextStichwList = Param[i].Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    int anzStichw = sTextStichwList.Length;
                    for (int s = 1; s < anzStichw; s++)
                    {
                        string[] sStichwortLine;
                        sStichwortLine = sTextStichwList[s].Split(';');
                        if (sStichwortLine[0].Trim().Length > 0) //Stichwort
                        {
                            arrStichwort.Add(new csStichwort(sStichwortLine[0].Trim(), sStichwortLine[2].Trim(), sStichwortLine[1].Trim()));
                        }
                    }
                }
            }
        }

        private void setFzFields()
        {
            tcFZ1.Visible = false; tcFZ2.Visible = false; tcFZ3.Visible = false; tcFZ4.Visible = false;
            tcFZ5.Visible = false; tcFZ6.Visible = false; tcFZ7.Visible = false; tcFZ8.Visible = false;

            int anzFz = arrFz.Count;
            for (int f = 1; f <= anzFz; f++)
            {
                if (f == 1)
                {
                    tcFZ1.BackColor = Color.LightGray;
                    lbFZ1.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ1.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ1.Visible = true;
                }
                if (f == 2)
                {
                    tcFZ2.BackColor = Color.LightGray;
                    lbFZ2.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ2.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ2.Visible = true;
                }
                if (f == 3)
                {
                    tcFZ3.BackColor = Color.LightGray;
                    lbFZ3.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ3.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ3.Visible = true;
                }
                if (f == 4)
                {
                    tcFZ4.BackColor = Color.LightGray;
                    lbFZ4.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ4.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ4.Visible = true;
                }
                if (f == 5)
                {
                    tcFZ5.BackColor = Color.LightGray;
                    lbFZ5.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ5.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ5.Visible = true;
                }
                if (f == 6)
                {
                    tcFZ6.BackColor = Color.LightGray;
                    lbFZ6.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ6.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ6.Visible = true;
                }
                if (f == 7)
                {
                    tcFZ7.BackColor = Color.LightGray;
                    lbFZ7.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ7.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ7.Visible = true;
                }
                if (f == 8)
                {
                    tcFZ8.BackColor = Color.LightGray;
                    lbFZ8.ForeColor = Color.FromArgb(186, 186, 186);
                    lbFZ8.Text = (arrFz[f - 1] as csFzList).sTyp.ToString() + " \r\n " + (arrFz[f - 1] as csFzList).sNr.ToString();
                    tcFZ8.Visible = true;
                }
            }
        }

        private void SetAlarmContent(Operation operation)
        {
            try
            {
                string sImSchutzbereich = "a";
                getReferenzParameter();
                DebugLabel.ForeColor = Color.Red;
                DebugLabel.Text = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                DebugLabel.Text += " - " + operation.OperationGuid.ToString();
                DebugLabel.Text += " - " + operation.Id;
                DebugLabel.Text += " - " + operation.IsAcknowledged;
                
                lbSchlagwort.Text = operation.Keywords.ToString();                
                if (shortKeyword == "ja")
                {
                    lbSchlagwort.Text = lbSchlagwort.Text.Replace(", Stichwort:", ",");
                    lbSchlagwort.Text = lbSchlagwort.Text.Replace("Stichwort:","");                    
                }

                lbSchlagwort.ForeColor = Color.Black;
                tcSchlagwort.BackColor = Color.LightGreen;
                if (operation.Priority != null)
                {
                    tclbPrio.Visible = true;
                    tcPrio.Visible = true;
                    lbPrio.Text = operation.Priority.ToString();
                    lbPrio.ForeColor = Color.Black;
                    tcPrio.BackColor = Color.White;
                    lblPrio.ForeColor = Color.Black;
                    tclbPrio.BackColor = Color.White;
                    //Wenn Prio = 1 dann Farbig hervorheben
                    if (lbPrio.Text.Contains("1"))
                    {
                        tcPrio.BackColor = Color.Blue;
                        lbPrio.ForeColor = Color.Red;
                        lblPrio.ForeColor = Color.Red;
                        tclbPrio.BackColor = Color.Blue;
                    }
                }

                else
                {
                    lbPrio.Text = "0";
                    tcPrio.Visible = false;
                    tclbPrio.Visible = false;
                }
               
                
                // Wenn Schlüsselwort Brand dann roter Hintergrund
                //if (lbSchlagwort.Text.Contains("B:"))
                if (operation.OperationNumber.ToString().StartsWith("B"))
                {
                    tcSchlagwort.BackColor = Color.FromArgb(255, 080, 048);
                }
                // Wenn Schlüsselwort THL dann blauer Hintergurnd
                //if (lbSchlagwort.Text.Contains("T:"))
                if (operation.OperationNumber.ToString().StartsWith("T"))
                {
                    tcSchlagwort.BackColor = Color.FromArgb(000, 000, 128);
                    lbSchlagwort.ForeColor = Color.White;
                }

                lbSchlagwort.Font.Bold = true;
                lbObjekt.Text = operation.Einsatzort.Property;
                if (showMessenger == "ja")
                { lbObjekt.Text = lbObjekt.Text + " Mitteiler: " + operation.Messenger.ToString(); }
                lbObjekt.Font.Size = 20;
                lbObjekt.Font.Bold = false;
                tcObjekt.BackColor = Color.LightBlue;
                lbBemerkung.Text = operation.Comment;
                lbBemerkung.Font.Size = 24;
                if (lbBemerkung.Text.Length > 85)
                {
                    lbBemerkung.Font.Size = 18;
                    if (lbBemerkung.Text.Length > commentLengh+1)
                    { lbBemerkung.Text = lbBemerkung.Text.Substring(0, commentLengh) + " ..."; }
                }
                lbBemerkung.Font.Bold = false;
                lbBemerkung.ForeColor = Color.Black;
                tcBemerkung.BackColor = Color.LightGreen;
                tcAddress.BackColor = Color.LightBlue;
                lbAddress.Text = operation.Einsatzort.Street + " " + operation.Einsatzort.StreetNumber;
                lbAddress.Font.Bold = true;
                tcAddress.BackColor = Color.LightYellow;

                //Adressdaten aufbereiten; ggf. aufeinander folgende, gleich Ortsnamen unterdrücken
                lbOrt.Text = operation.Einsatzort.ZipCode + " " + operation.Einsatzort.City;
                //ggf führende Leerzeichen entfernen
                lbOrt.Text = lbOrt.Text.Trim();
                string[] words = lbOrt.Text.Split(' ');
                int l = words.Length;
                lbOrt.Text = words[0].ToString();
                for (int i = 1; i < l; i++)
                {
                    if (!words[i].ToString().Contains(words[i - 1].ToString()))
                    {
                        lbOrt.Text = lbOrt.Text + " " + words[i].ToString();
                    }
                }

                lbOrt.Font.Bold = true;
                tcOrt.BackColor = Color.LightYellow;

                //innerorts-außerorts KNZ setzen
                if (lbOrt.Text.Contains(PLZ))
                { sImSchutzbereich = "i"; }

               
                //Fahrzeuge+Ausrüstung nach den eigenen filtern.
                string MyResourses = "";
                lbResources.Text = operation.Resources.ToString("{FullName} & {RequestedEquipment} ;", null).Trim();
                lbResources.Text = lbResources.Text.TrimEnd(';');
                string[] Resources = lbResources.Text.Split(';');
                lbResources.Text = "";
                int k = Resources.Length;
                for (int i = 0; i < k; i++)
                {
                    if (Resources[i].ToString().Contains(FFName))
                    {
                        string[] Geraet = Resources[i].ToString().Trim().Split('&');
                        if (Geraet[1].Trim().Length > 0)
                        {
                            MyResourses = MyResourses + " " + Resources[i].ToString().TrimEnd().Replace("&","mit") + ";";
                        }
                    }
                }
                MyResourses = MyResourses.Trim();
                lbResources.Text = MyResourses.Replace(FFName, "");

                lbResources.Font.Bold = true;
                lbResources.Font.Size = 20;
                tcResources.BackColor = Color.LightBlue;

                //Fahrzeuge Initialisieren: 
                
                setFzFields();

                tcTimeLeft.BackColor = Color.White;
                //Alarmzeit
                //TimeSpan ts = Convert.ToDateTime(DateTime.Now) - Convert.ToDateTime(operation.TimestampIncome);

                lbTimeLeft.ForeColor = Color.Black;
                lbTimeLeft.Text = operation.Timestamp.ToString(@"HH\:mm");
                //lbTimeLeft.Text = ts.ToString(@"mm\:ss");
                lbTimeLeft.Font.Size = 22;
                lbTimeLeft.Font.Bold = true;

                
                //angeforderte Fahrzeuge einblenden

                int anzFz = arrFz.Count;
                for (int f = 0; f < anzFz; f++)
                {
                    if (operation.Resources.ToString("{FullName} {RequestedEquipment} ;", null).Contains((arrFz[f] as csFzList).sFzBez.ToString()))
                    {
                        (arrFz[f] as csFzList).iAngefordert = 1;
                    }
                }

                //Fz laut AAO setzen

                for (int a = 0; a < arrStichwort.Count; a++)//Die liste der Stichworte durchgehen
                {
                    if (operation.Keywords.ToString().Contains((arrStichwort[a] as csStichwort).sStichwort))
                        if (sImSchutzbereich == (arrStichwort[a] as csStichwort).sInerorts.ToString())
                        {
                            { SetAAOFz((arrStichwort[a] as csStichwort).sFzList); }
                        }
                }


                //Fahrzeue anzeigen
                SetAktivFZ();


            }
            catch 
            {
                Page page = this;
                ServiceConnection.Instance.RedirectToErrorPage(ref page);
            }
        }
        

        private void SetAAOFz(string fzList)
        {
             for (int f = 0; f < arrFz.Count; f++)// für jedes Stichwort prüfen ob übereinstimmende FZ vorhanden und setzen
                {
                    if(fzList.Contains((arrFz[f] as csFzList ).sFzBez ))
                    { (arrFz[f] as csFzList).iAAO = 1; }
                }
           
        }

        

        private void SetAktivFZ()
        {
            Color ColAngefordert = Color.FromName(colorFzFax);
            Color ColAAO = Color.FromName(colorFzAAO);
            Color ColAngefordertAAO = Color.FromName(colorFzFaxAAO);
            int anzFz = arrFz.Count;
            for (int f = 1; f <= anzFz; f++)
            {
                if (f == 1)
                {
                    lbFZ1.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ1.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ1.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ1.BackColor = ColAAO; }
                    }
                }
                if (f == 2)
                {
                    lbFZ2.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ2.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ2.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ2.BackColor = ColAAO; }
                    }
                }
                if (f == 3)
                {
                    lbFZ3.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ3.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ3.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ3.BackColor = ColAAO; }
                    }
                }
                if (f == 4)
                {
                    lbFZ4.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ4.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ4.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ4.BackColor = ColAAO; }
                    }
                }
                if (f == 5)
                {
                    lbFZ5.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ5.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ5.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ5.BackColor = ColAAO; }
                    }
                }
                if (f == 6)
                {
                    lbFZ6.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ6.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ6.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ6.BackColor = ColAAO; }
                    }
                }
                if (f == 7)
                {
                    lbFZ7.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ7.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ7.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ7.BackColor = ColAAO; }
                    }
                }
                if (f == 8)
                {
                    lbFZ8.ForeColor = Color.Black;
                    if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                    { tcFZ8.BackColor = ColAngefordert; }
                    if ((arrFz[f - 1] as csFzList).iAAO == 1)
                    {
                        if ((arrFz[f - 1] as csFzList).iAngefordert == 1)
                        { tcFZ8.BackColor = ColAngefordertAAO; }
                        else
                        { tcFZ8.BackColor = ColAAO; }
                    }
                }

            }
        }

        private void GetOperation(string id, out Operation operation)
        {
            operation = null;
            try
            {
                using (WrappedService<IAlarmWorkflowServiceInternal> service = InternalServiceProxy.GetServiceInstance())
                {
                    OperationItem operationItem = service.Instance.GetOperationById(int.Parse(id));
                    operation = operationItem.ToOperation();
                }
            }
            catch (EndpointNotFoundException)
            {
                Page page = this;
                ServiceConnection.Instance.RedirectToErrorPage(ref page);
            }
        }

        private Dictionary<string, string> GetGeocodes(string address)
        {
            Dictionary<string, string> geocodes = new Dictionary<string, string>();
            string urladdress = HttpUtility.UrlEncode(address);
            string url = "http://maps.googleapis.com/maps/api/geocode/xml?address=" + urladdress + "&sensor=false";
            WebResponse response = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                response = request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        XPathDocument document = new XPathDocument(stream);
                        XPathNavigator navigator = document.CreateNavigator();

                        // get response status
                        XPathNodeIterator statusIterator = navigator.Select("/GeocodeResponse/status");
                        while (statusIterator.MoveNext())
                        {
                            if (statusIterator.Current.Value != "OK")
                            {
                                return null;
                            }
                        }

                        // gets first restult
                        XPathNodeIterator resultIterator = navigator.Select("/GeocodeResponse/result");
                        resultIterator.MoveNext();
                        XPathNodeIterator geometryIterator = resultIterator.Current.Select("geometry");
                        geometryIterator.MoveNext();
                        XPathNodeIterator locationIterator = geometryIterator.Current.Select("location");
                        while (locationIterator.MoveNext())
                        {
                            XPathNodeIterator latIterator = locationIterator.Current.Select("lat");
                            while (latIterator.MoveNext())
                            {
                                geocodes.Add("lat", latIterator.Current.Value);
                            }
                            XPathNodeIterator lngIterator = locationIterator.Current.Select("lng");
                            while (lngIterator.MoveNext())
                            {
                                geocodes.Add("long", lngIterator.Current.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogFormat(LogType.Error, typeof(Default), "Could not retrieve geocode for address '{0}'.", address);
                Logger.Instance.LogException(typeof(Default), ex);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }

            return geocodes;
        }

        private string OSM(Dictionary<string, string> result)
        {
            OSMCode = "    OpenLayers.Lang.setCode('de');    " +
                      "    var lon = " + result["long"] + " ;" +
                      "    var lat = " + result["lat"] + " ;" +
                      "    var zoom =" + WebsiteConfiguration.Instance.OSMZoomLevel + " ;" +
                      "    map = new OpenLayers.Map('osmmap', {" +
                      "        projection: new OpenLayers.Projection(\"EPSG:900913\")," +
                      "        displayProjection: new OpenLayers.Projection(\"EPSG:4326\")," +
                      "        controls: [" +
                      "            new OpenLayers.Control.Navigation()," +
                      "            new OpenLayers.Control.LayerSwitcher()," +
                      "            new OpenLayers.Control.PanZoomBar()]," +
                      "        maxExtent:" +
                      "            new OpenLayers.Bounds(-20037508.34,-20037508.34," +
                      "                                    20037508.34, 20037508.34)," +
                      "        numZoomLevels: 18," +
                      "        maxResolution: 156543," +
                      "        units: 'meters'" +
                      "    });" +
                      "    layer_mapnik = new OpenLayers.Layer.OSM.Mapnik(\"Mapnik\");" +
                      "    layer_markers = new OpenLayers.Layer.Markers(\"Address\", { projection: new OpenLayers.Projection(\"EPSG:4326\"), " +
                      "    	                                          visibility: true, displayInLayerSwitcher: false });" +
                      "    map.addLayers([layer_mapnik, layer_markers]);" +
                      "    jumpTo(lon, lat, zoom); " +
                      "    addMarker(layer_markers, lon, lat);";
            return OSMHead;
        }

        private string GoogleMaps(Operation operation, Dictionary<string, string> result)
        {
            StringBuilder builder = new StringBuilder();
            String longitute = result["long"];
            String latitude = result["lat"];
            String variables =
                "directionsDisplay = new google.maps.DirectionsRenderer();" +
                "var zoomOnAddress = true;" +
                "var dest = new google.maps.LatLng(" + latitude + "," + longitute + ");" +
                "var address = '" + operation.Einsatzort.Street + " " + operation.Einsatzort.StreetNumber + " " +
                operation.Einsatzort.ZipCode + " " + operation.Einsatzort.City + "';" +
                "var Home = '" + _configuration.Home + "';" +
                "var ZoomLevel =" + (_configuration.GoogleZoomLevel / 100.0D).ToString(CultureInfo.InvariantCulture) + ";" +
                "var mapType = google.maps.MapTypeId." + _configuration.Maptype + ";" +
                "var mapOptions = {" +
                "zoom: 10," +
                "overviewMapControl: true," +
                "panControl: false," +
                "streetViewControl: false," +
                "ZoomControl: " + _configuration.ZoomControl.ToString().ToLower() + "," +
                "mapTypeId: mapType" +
                "};" +
                "map = new google.maps.Map(document.getElementById(\"googlemap\")," +
                "mapOptions);";

            builder.Append(BeginnHead);
            builder.Append(variables);
            builder.Append(_configuration.Route ? Showroute : CenterCoord);
            if (_configuration.Tilt)
            {
                builder.Append(Tilt);
            }
            if (_configuration.Traffic)
            {
                builder.Append(Traffic);
            }
            builder.Append("}");
            if (_configuration.Route)
            {
                builder.Append(RouteFunc);
            }
            builder.Append("google.maps.event.addDomListener(window, \"load\", initialize);");
            return builder.ToString();
        }

        #endregion

        #region Event handlers

        protected void UpdateTimer_Tick(object sender, EventArgs e)
        {

            Page page = this;
            ServiceConnection.Instance.CheckForUpdate(ref page);


        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="T:System.EventArgs" /> object that contains the event data.
        /// </param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // This page should not feature any postbacks (like the result from the user clicking on a link, button or such).
            if (IsPostBack)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(Request["id"]))
            {
                Page page = this;
                ServiceConnection.Instance.CheckForUpdate(ref page);
            }
            else
            {
                SetAlarmDisplay();
            }
        }

        #endregion

        protected void ResetButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (WrappedService<IAlarmWorkflowServiceInternal> service = InternalServiceProxy.GetServiceInstance())
                {
                    service.Instance.AcknowledgeOperation(Int32.Parse(Request["id"]));
                    Page page = this;
                    ServiceConnection.Instance.RedirectToNoAlarm(ref page);
                }
            }
            catch (EndpointNotFoundException)
            {
                Page page = this;
                ServiceConnection.Instance.RedirectToErrorPage(ref page);
            }
        }
    }
}