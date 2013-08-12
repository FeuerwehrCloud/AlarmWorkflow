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
using System.IO;
using System.Threading.Tasks;
using AlarmWorkflow.Job.ExternalTool.Properties;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Diagnostics;
using AlarmWorkflow.Shared.Engine;
using AlarmWorkflow.Shared.Extensibility;
using AlarmWorkflow.Shared.Settings;

namespace AlarmWorkflow.Job.ExternalTool
{
    [Export("ExternalToolJob", typeof(IJob))]
    [Information(DisplayName = "ExportJobDisplayName", Description = "ExportJobDescription")]
    class ExternalToolJob : IJob
    {
        #region Constants

        private static readonly string[] SupportedExtensions = { ".bat", ".cmd", ".exe" };

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalToolJob"/> class.
        /// </summary>
        public ExternalToolJob()
        {
        }

        #endregion

        #region IJob Members

        void IJob.Execute(IJobContext context, Operation operation)
        {
            switch (context.Phase)
            {
                case JobPhase.OnOperationSurfaced:
                    StartPrograms(operation, "ExternalToolsOnOperationSurfaced");
                    break;
                case JobPhase.AfterOperationStored:
                    StartPrograms(operation, "ExternalToolsAfterOperationStored");
                    break;
                default:
                    break;
            }
        }

        private void StartPrograms(Operation operation, string phaseSettingName)
        {
            string[] expressedLines = SettingsManager.Instance.GetSetting("ExternalToolJob", phaseSettingName).GetStringArray();
            foreach (string exprline in expressedLines)
            {
                string fileName = null;
                try
                {
                    fileName = operation.ToString(exprline);

                    Task.Factory.StartNew(StartProgramTask, fileName);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogFormat(LogType.Error, this, Resources.CreatingProgramFailed, exprline, fileName);
                    Logger.Instance.LogException(this, ex);
                }
            }
        }

        private void StartProgramTask(object parameter)
        {
            string fileNameWithArguments = (string)parameter;

            string fileName = "";
            string arguments = "";

            try
            {
                // Search for the extension. Take everything before as file name, and everything after as arguments.
                int iExt = -1;
                foreach (string ext in SupportedExtensions)
                {
                    iExt = fileNameWithArguments.IndexOf(ext);
                    if (iExt > -1)
                    {
                        fileName = fileNameWithArguments.Substring(0, iExt + ext.Length);
                        arguments = fileNameWithArguments.Remove(0, fileName.Length).Trim();

                        break;
                    }
                }

                // If program file is unsupported, skip execution and warn user.
                if (iExt == -1)
                {
                    Logger.Instance.LogFormat(LogType.Warning, this, Resources.ProgramNotSupported, fileNameWithArguments, string.Join(", ", SupportedExtensions));
                    return;
                }

                ProcessWrapper proc = new ProcessWrapper();
                proc.FileName = fileName;
                proc.WorkingDirectory = Path.GetDirectoryName(fileName);
                proc.Arguments = arguments;

                proc.Start();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogFormat(LogType.Error, this, Resources.ProgramStartFailed, fileNameWithArguments);
                Logger.Instance.LogException(this, ex);
            }
        }

        bool IJob.Initialize()
        {
            return true;
        }

        bool IJob.IsAsync
        {
            get { return false; }
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {

        }

        #endregion
    }
}
