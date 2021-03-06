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
using System.Text.RegularExpressions;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Diagnostics;
using AlarmWorkflow.Shared.Extensibility;

namespace AlarmWorkflow.Parser.Library
{
    [Export("IlsErdingParser", typeof(IParser))]
    sealed class IlsErdingParser : IParser
    {
        #region Constants

        private static readonly string[] Keywords =
        {
            "", "ALARM", "EINSATZNUMMER", "NAME", "STRA�E", "ABSCHNITT", "ORT", "RUFNUMMER", "OBJEKT",
            "STATION", "SCHLAGW", "GEF. GER�TE", "ALARMIERT", "EINSATZPLANNUMMER"
        };

        #endregion

        #region Methods

        private string GetTextBetween(string line, string start, string stop)
        {
            int startIndex = 0;
            int stopIndex = 0;

            if (!string.IsNullOrWhiteSpace(start))
            {
                if (line.ToUpper().Contains(start.ToUpper()))
                {
                    startIndex = line.ToUpper().IndexOf(start.ToUpper()) + start.Length;
                }
            }

            if (!string.IsNullOrWhiteSpace(stop))
            {
                if (line.ToUpper().Contains(stop.ToUpper()))
                {
                    stopIndex = line.ToUpper().IndexOf(stop.ToUpper());
                }
            }

            if (!string.IsNullOrWhiteSpace(stop))
            {
                if (stopIndex < startIndex)
                {
                    return line.Substring(startIndex).Trim();
                }
                else
                {
                    int length = stopIndex - startIndex;
                    return line.Substring(startIndex, length).Trim();
                }
            }
            return line.Substring(startIndex).Trim();
        }

        private DateTime ReadFaxTimestamp(string line, DateTime fallback)
        {
            DateTime date = fallback;
            TimeSpan timestamp = date.TimeOfDay;

            Match dt = Regex.Match(line, @"(0[1-9]|[12][0-9]|3[01])[- /.](0[1-9]|1[012])[- /.](19|20)\d\d");
            Match ts = Regex.Match(line, @"([01]?[0-9]|2[0-3]):[0-5][0-9]");
            if (dt.Success)
            {
                DateTime.TryParse(dt.Value, out date);
            }
            if (ts.Success)
            {
                TimeSpan.TryParse(ts.Value, out timestamp);
            }

            return new DateTime(date.Year, date.Month, date.Day, timestamp.Hours, timestamp.Minutes, timestamp.Seconds, timestamp.Milliseconds, DateTimeKind.Local);
        }

        private bool StartsWithKeyword(string line, out string keyword)
        {
            line = line.ToUpperInvariant();
            foreach (string kwd in Keywords)
            {
                if (line.StartsWith(kwd))
                {
                    keyword = kwd;
                    return true;
                }
            }
            keyword = null;
            return false;
        }

        /// <summary>
        /// Returns the message text, which is the line text but excluding the keyword/prefix and a possible colon.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="prefix">The prefix that is to be removed (optional).</param>
        /// <returns></returns>
        private string GetMessageText(string line, string prefix)
        {
            if (prefix == null)
            {
                prefix = "";
            }

            if (prefix.Length > 0)
            {
                line = line.Remove(0, prefix.Length).Trim();
            }
            else
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex != -1)
                {
                    line = line.Remove(0, colonIndex + 1);
                }
            }

            if (line.StartsWith(":"))
            {
                line = line.Remove(0, 1).Trim();
            }

            return line;
        }

        /// <summary>
        /// Attempts to read the zip code from the city, if available.
        /// </summary>
        /// <param name="cityText"></param>
        /// <returns>The zip code of the city. -or- null, if there was no.</returns>
        private string ReadZipCodeFromCity(string cityText)
        {
            string zipCode = "";
            foreach (char c in cityText)
            {
                if (char.IsNumber(c))
                {
                    zipCode += c;
                    continue;
                }
                break;
            }
            return zipCode;
        }

        private bool GetSection(String line, ref CurrentSection section, ref bool keywordsOnly)
        {
            //MI TTE I LER must be considered when using tesseract because of recognition problems.
            if (line.Contains("MITTEILER") || line.Contains("M I TTE I LER"))
            {
                section = CurrentSection.BMitteiler;
                keywordsOnly = true;
                return true;
            }
            if (line.Contains("EINSATZORT"))
            {
                section = CurrentSection.CEinsatzort;
                //Is not true but only works that way
                keywordsOnly = true;
                return true;
            }
            if (line.Contains("ZIELORT"))
            {
                section = CurrentSection.DZielort;
                //Is not true but only works that way
                keywordsOnly = true;
                return true;
            }
            if (line.Contains("PATIENT"))
            {
                section = CurrentSection.HFooter;
                keywordsOnly = false;
                return true;
            }
            if (line.Contains("EINSATZGRUND"))
            {
                section = CurrentSection.EEinsatzgrund;
                keywordsOnly = true;
                return true;
            }
            if (line.Contains("EINSATZMITTEL"))
            {
                section = CurrentSection.FEinsatzmittel;
                keywordsOnly = true;
                return true;
            }
            if (line.Contains("BEMERKUNG"))
            {
                section = CurrentSection.GBemerkung;
                keywordsOnly = false;
                return true;
            }
            if (line.Contains("ENDE FAX"))
            {
                section = CurrentSection.HFooter;
                keywordsOnly = false;
                return true;
            }
            return false;
        }

        #endregion

        #region IParser Members

        Operation IParser.Parse(string[] lines)
        {
            Operation operation = new Operation();
            OperationResource last = new OperationResource();

            lines = Utilities.Trim(lines);

            CurrentSection section = CurrentSection.AHeader;
            bool keywordsOnly = true;

            InnerSection innerSection = InnerSection.AStra�e;
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    string line = lines[i];
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    // Try to parse the header and extract date and time if possible
                    operation.Timestamp = ReadFaxTimestamp(line, operation.Timestamp);


                    if (GetSection(line.Trim(), ref section, ref keywordsOnly))
                    {
                        continue;
                    }

                    string msg = line;
                    string prefix = "";

                    // Make the keyword check - or not (depends on the section we are in; see above)
                    if (keywordsOnly)
                    {
                        string keyword;
                        if (!StartsWithKeyword(line, out keyword))
                        {
                            continue;
                        }

                        int x = line.IndexOf(':');
                        if (x == -1)
                        {
                            // If there is no colon found (may happen occasionally) then simply remove the length of the keyword from the beginning
                            prefix = keyword;
                            msg = line.Remove(0, prefix.Length).Trim();
                        }
                        else
                        {
                            prefix = line.Substring(0, x);
                            msg = line.Substring(x + 1).Trim();
                        }

                        prefix = prefix.Trim().ToUpperInvariant();
                    }

                    // Parse each section
                    switch (section)
                    {
                        case CurrentSection.AHeader:
                            {
                                switch (prefix)
                                {
                                    case "ALARM":
                                        operation.Timestamp = ReadFaxTimestamp(msg, DateTime.Now);
                                        break;
                                    case "EINSATZNUMMER":
                                        operation.OperationNumber = msg;
                                        break;
                                }
                            }
                            break;
                        case CurrentSection.BMitteiler:
                            {
                                // This switch would not be necessary in this section (there is only "Name")...
                                switch (prefix)
                                {
                                    case "NAME":
                                        operation.Messenger = msg;
                                        break;
                                    case "RUFNUMMER":
                                        if (operation.Messenger != null)
                                        {
                                            operation.Messenger += " " + msg;
                                        }
                                        break;
                                }
                            }
                            break;
                        case CurrentSection.CEinsatzort:
                            {
                                switch (prefix)
                                {
                                    case "STRA�E":
                                        {
                                            innerSection = InnerSection.AStra�e;
                                            // The street here is mangled together with the street number. Dissect them...
                                            operation.Einsatzort.Street = GetTextBetween(msg, null, "Haus-Nr.:");
                                            operation.Einsatzort.StreetNumber = GetTextBetween(msg, "Haus-Nr.:", null);
                                        }
                                        break;
                                    case "ABSCHNITT":
                                        operation.Einsatzort.Intersection = msg;
                                        break;
                                    case "ORT":
                                        {
                                            innerSection = InnerSection.BOrt;
                                            operation.Einsatzort.ZipCode = ReadZipCodeFromCity(msg);
                                            if (string.IsNullOrWhiteSpace(operation.Einsatzort.ZipCode))
                                            {
                                                Logger.Instance.LogFormat(LogType.Warning, this, "Could not find a zip code for city '{0}'. Route planning may fail or yield wrong results!", operation.Einsatzort.City);
                                            }

                                            operation.Einsatzort.City = msg.Remove(0, operation.Einsatzort.ZipCode.Length).Trim();
                                        }
                                        break;
                                    case "OBJEKT":
                                        innerSection = InnerSection.CObjekt;
                                        operation.Einsatzort.Property = msg;
                                        break;
                                    case "EINSATZPLANNUMMER":
                                        operation.OperationPlan = msg;
                                        break;
                                    case "STATION":
                                        innerSection = InnerSection.DStation;
                                        operation.CustomData["Einsatzort Station"] = msg;
                                        break;
                                    default:
                                        switch (innerSection)
                                        {
                                            case InnerSection.AStra�e:
                                                //Quite dirty because of Streetnumber. Looking for better solution
                                                operation.Einsatzort.Street += msg;
                                                break;
                                            case InnerSection.BOrt:
                                                operation.Einsatzort.City += msg;
                                                break;
                                            case InnerSection.CObjekt:
                                                operation.Einsatzort.Property += msg;
                                                break;
                                            case InnerSection.DStation:
                                                operation.CustomData["Einsatzort Station"] += msg;
                                                break;
                                        }
                                        break;
                                }
                            }
                            break;
                        case CurrentSection.DZielort:
                            {
                                switch (prefix)
                                {
                                    case "STRA�E":
                                        {
                                            innerSection = InnerSection.AStra�e;
                                            // The street here is mangled together with the street number. Dissect them...
                                            operation.Zielort.Street = GetTextBetween(msg, null, "Haus-Nr.:");
                                            operation.Zielort.StreetNumber = GetTextBetween(msg, "Haus-Nr.:", "Zusatz");
                                        }
                                        break;
                                    case "ORT":
                                        {
                                            innerSection = InnerSection.BOrt;
                                            operation.Zielort.ZipCode = ReadZipCodeFromCity(msg);
                                            if (string.IsNullOrWhiteSpace(operation.Zielort.ZipCode))
                                            {
                                                Logger.Instance.LogFormat(LogType.Warning, this, "Could not find a zip code for city '{0}'. Route planning may fail or yield wrong results!", operation.Zielort.City);
                                            }

                                            operation.Zielort.City = msg.Remove(0, operation.Zielort.ZipCode.Length).Trim();
                                        }
                                        break;
                                    case "OBJEKT":
                                        innerSection = InnerSection.CObjekt;
                                        operation.Zielort.Property = msg;
                                        break;
                                    case "STATION":
                                        innerSection = InnerSection.DStation;
                                        operation.CustomData["Zielort Station"] = msg;
                                        break;
                                    default:
                                        switch (innerSection)
                                        {
                                            case InnerSection.AStra�e:
                                                //Quite dirty because of Streetnumber. Looking for better solution
                                                operation.Zielort.Street += msg;
                                                break;
                                            case InnerSection.BOrt:
                                                operation.Zielort.City += msg;
                                                break;
                                            case InnerSection.CObjekt:
                                                operation.Zielort.Property += msg;
                                                break;
                                            case InnerSection.DStation:
                                                operation.CustomData["Zielort Station"] += msg;
                                                break;
                                        }
                                        break;
                                }
                            }
                            break;
                        case CurrentSection.EEinsatzgrund:
                            {
                                switch (prefix)
                                {
                                    case "SCHLAGW.":
                                        operation.Keywords.Keyword = msg;
                                        break;
                                    case "STICHWORT":
                                        operation.Keywords.EmergencyKeyword = msg;
                                        break;
                                }
                            }
                            break;
                        case CurrentSection.FEinsatzmittel:
                            {
                                switch (prefix)
                                {
                                    case "NAME":
                                        last.FullName = msg.Trim();
                                        break;
                                    case "GEF. GER�TE":
                                        // Only add to requested equipment if there is some text,
                                        // otherwise the whole vehicle is the requested equipment
                                        if (!string.IsNullOrWhiteSpace(msg))
                                        {
                                            last.RequestedEquipment.Add(msg);
                                        }
                                        break;
                                    case "ALARMIERT":
                                        // Only add to requested equipment if there is some text,
                                        // otherwise the whole vehicle is the requested equipment
                                        if (!string.IsNullOrWhiteSpace(msg))
                                        {
                                            last.Timestamp = msg;
                                        }
                                        operation.Resources.Add(last);
                                        last = new OperationResource();
                                        break;
                                }
                            }
                            break;
                        case CurrentSection.GBemerkung:
                            {
                                // Append with newline at the end in case that the message spans more than one line
                                operation.Comment = operation.Comment += msg + "\n";
                            }
                            break;
                        case CurrentSection.HFooter:
                            // The footer can be ignored completely.
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogFormat(LogType.Warning, this, "Error while parsing line '{0}'. The error message was: {1}", i, ex.Message);
                }
            }

            // Post-processing the operation if needed
            if (!string.IsNullOrWhiteSpace(operation.Comment) && operation.Comment.EndsWith("\n"))
            {
                operation.Comment = operation.Comment.Substring(0, operation.Comment.Length - 1).Trim();
            }
            return operation;
        }

        #endregion

        #region Nested types

        private enum CurrentSection
        {
            AHeader,
            BMitteiler,
            CEinsatzort,
            DZielort,
            EEinsatzgrund,
            FEinsatzmittel,
            GBemerkung,

            /// <summary>
            /// Footer text. Introduced by "ENDE FAX". Can be ignored completely.
            /// </summary>
            HFooter,
        }

        private enum InnerSection
        {
            AStra�e,
            BOrt,
            CObjekt,
            DStation,
        }

        #endregion
    }
}
