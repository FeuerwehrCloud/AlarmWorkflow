﻿// This file is part of AlarmWorkflow.
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

using System.Collections.ObjectModel;
using AlarmWorkflow.Shared.Settings;
using AlarmWorkflow.Shared.Specialized;

namespace AlarmWorkflow.Shared.Core
{
    /// <summary>
    /// Represents the current configuration. Components can access this configuration to get common information.
    /// </summary>
    public sealed class AlarmWorkflowConfiguration
    {
        #region Fields

        private static readonly object Lock = new object();
        private static AlarmWorkflowConfiguration _instance;

        /// <summary>
        /// Gets the current Configuration.
        /// </summary>
        public static AlarmWorkflowConfiguration Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new AlarmWorkflowConfiguration();
                    }
                    return _instance;
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the information about the current fire department.
        /// </summary>
        /// <remarks>This information is used (among others) to provide the route information to the operation destination.</remarks>
        public FireDepartmentInfo FDInformation { get; private set; }
        /// <summary>
        /// Gets the replace dictionary to use for replacing strings with other strings in parsed content.
        /// </summary>
        public ReplaceDictionary ReplaceDictionary { get; private set; }

        internal ReadOnlyCollection<string> EnabledJobs { get; private set; }
        internal ReadOnlyCollection<string> EnabledAlarmSources { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="AlarmWorkflowConfiguration"/> class from being created.
        /// </summary>
        private AlarmWorkflowConfiguration()
        {
            // Public configuration
            this.FDInformation = new FireDepartmentInfo();
            this.FDInformation.Name = SettingsManager.Instance.GetSetting("Shared", "FD.Name").GetString();
            this.FDInformation.Location = new PropertyLocation();
            this.FDInformation.Location.ZipCode = SettingsManager.Instance.GetSetting("Shared", "FD.ZipCode").GetString();
            this.FDInformation.Location.City = SettingsManager.Instance.GetSetting("Shared", "FD.City").GetString();
            this.FDInformation.Location.Street = SettingsManager.Instance.GetSetting("Shared", "FD.Street").GetString();
            this.FDInformation.Location.StreetNumber = SettingsManager.Instance.GetSetting("Shared", "FD.StreetNumber").GetString();

            this.ReplaceDictionary = SettingsManager.Instance.GetSetting("Shared", "ReplaceDictionary").GetValue<ReplaceDictionary>();

            // Internal configuration
            this.EnabledJobs = new ReadOnlyCollection<string>(SettingsManager.Instance.GetSetting("Shared", "JobsConfiguration").GetValue<ExportConfiguration>().GetEnabledExports());
            this.EnabledAlarmSources = new ReadOnlyCollection<string>(SettingsManager.Instance.GetSetting("Shared", "AlarmSourcesConfiguration").GetValue<ExportConfiguration>().GetEnabledExports());
        }

        #endregion

        #region Nested types

        /// <summary>
        /// Represents information about the current fire department site.
        /// </summary>
        public sealed class FireDepartmentInfo
        {
            /// <summary>
            /// Gets the name of the site.
            /// </summary>
            public string Name { get; internal set; }
            /// <summary>
            /// Gets the location of the site.
            /// </summary>
            public PropertyLocation Location { get; internal set; }
        }


        #endregion
    }
}