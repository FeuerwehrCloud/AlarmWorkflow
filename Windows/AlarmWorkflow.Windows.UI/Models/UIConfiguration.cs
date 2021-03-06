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

using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Settings;

namespace AlarmWorkflow.Windows.UI.Models
{
    /// <summary>
    /// Represents the configuration for the Windows UI.
    /// </summary>
    internal sealed class UIConfiguration
    {
        #region Properties

        /// <summary>
        /// Gets/sets the alias of the operation viewer to use. Empty means that the default viewer shall be used.
        /// </summary>
        public string OperationViewer { get; set; }
        /// <summary>
        /// Gets/sets the automatic operation acknowledgement settings.
        /// </summary>
        public AutomaticOperationAcknowledgementSettings AutomaticOperationAcknowledgement { get; set; }
        /// <summary>
        /// Gets the maximum amount of parallel alarms in the UI.
        /// </summary>
        public int MaxAlarmsInUI { get; private set; }
        /// <summary>
        /// Gets the names of the jobs that are enabled.
        /// </summary>
        public ReadOnlyCollection<string> EnabledJobs { get; private set; }
        /// <summary>
        /// Gets the names of the idle-jobs that are enabled.
        /// </summary>
        public ReadOnlyCollection<string> EnabledIdleJobs { get; private set; }
        /// <summary>
        /// Gets the key to press to acknowledge operations.
        /// </summary>
        public Key AcknowledgeOperationKey { get; private set; }
        /// <summary>
        /// Gets whether or not the selected alarm should change automatic, if mulitple alarms are available.
        /// </summary>
        public bool SwitchAlarms { get; private set; }
        /// <summary>
        /// Gets whether or not the UI should be set to fullscreen on alarm.
        /// </summary>
        public bool FullscreenOnAlarm { get; private set; }
        /// <summary>
        /// Gets the time which should elapse between a change.
        /// </summary>
        public int SwitchTime { get; private set; }
        /// <summary>
        /// Gets whether or not to avoid screensaver or standby when an alarm is active.
        /// </summary>
        public bool AvoidScreensaver { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UIConfiguration"/> class.
        /// </summary>
        public UIConfiguration()
        {
            AutomaticOperationAcknowledgement = new AutomaticOperationAcknowledgementSettings();

            AcknowledgeOperationKey = System.Windows.Input.Key.B;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the UIConfiguration from its default path.
        /// </summary>
        /// <returns></returns>
        public static UIConfiguration Load()
        {
            UIConfiguration configuration = new UIConfiguration();
            configuration.OperationViewer = SettingsManager.Instance.GetSetting("UIConfiguration", "OperationViewer").GetString();
            configuration.FullscreenOnAlarm = SettingsManager.Instance.GetSetting("UIConfiguration", "FullscreenOnAlarm").GetBoolean();
            string acknowledgeOperationKeyS = SettingsManager.Instance.GetSetting("UIConfiguration", "AcknowledgeOperationKey").GetString();
            Key acknowledgeOperationKey = Key.B;
            Enum.TryParse<Key>(acknowledgeOperationKeyS, out acknowledgeOperationKey);
            configuration.AcknowledgeOperationKey = acknowledgeOperationKey;

            configuration.AutomaticOperationAcknowledgement.IsEnabled = SettingsManager.Instance.GetSetting("UIConfiguration", "AOA.IsEnabled").GetBoolean();
            configuration.AutomaticOperationAcknowledgement.MaxAge = SettingsManager.Instance.GetSetting("UIConfiguration", "AOA.MaxAge").GetInt32();
            configuration.AvoidScreensaver = SettingsManager.Instance.GetSetting("UIConfiguration", "AvoidScreensaver").GetBoolean();
            configuration.MaxAlarmsInUI = SettingsManager.Instance.GetSetting("UIConfiguration", "MaxAlarmsInUI").GetInt32();

            // Jobs configuration
            configuration.EnabledJobs = new ReadOnlyCollection<string>(SettingsManager.Instance.GetSetting("UIConfiguration", "JobsConfiguration").GetValue<ExportConfiguration>().GetEnabledExports());
            configuration.EnabledIdleJobs = new ReadOnlyCollection<string>(SettingsManager.Instance.GetSetting("UIConfiguration", "IdleJobsConfiguration").GetValue<ExportConfiguration>().GetEnabledExports());
            configuration.SwitchAlarms = SettingsManager.Instance.GetSetting("UIConfiguration", "SwitchAlarms").GetBoolean();
            configuration.SwitchTime = SettingsManager.Instance.GetSetting("UIConfiguration", "SwitchTime").GetInt32();
            return configuration;
        }

        #endregion

        #region Nested types

        /// <summary>
        /// Configures the automatic operation acknowledgement.
        /// </summary>
        public class AutomaticOperationAcknowledgementSettings
        {
            /// <summary>
            /// Gets/sets whether or not the automatic operation acknowledgement is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }
            /// <summary>
            /// Gets/sets the maximum age in minutes until an operation is automatically acknowleded.
            /// </summary>
            public int MaxAge { get; set; }
        }

        #endregion
    }
}