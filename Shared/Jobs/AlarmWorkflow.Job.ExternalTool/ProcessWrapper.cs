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
using System.Diagnostics;
using AlarmWorkflow.Job.ExternalTool.Properties;
using AlarmWorkflow.Shared.Diagnostics;

namespace AlarmWorkflow.Job.ExternalTool
{
    /// <summary>
    /// Represents a wrapper around a <see cref="Process"/> that allows for convenient running a process and listening to its StdOut/StdErr to log them.
    /// </summary>
    sealed class ProcessWrapper
    {
        #region Fields

        private Process _process;

        #endregion

        #region Properties

        internal string WorkingDirectory
        {
            get { return _process.StartInfo.WorkingDirectory; }
            set { _process.StartInfo.WorkingDirectory = value; }
        }

        internal string FileName
        {
            get { return _process.StartInfo.FileName; }
            set { _process.StartInfo.FileName = value; }
        }

        internal string Arguments
        {
            get { return _process.StartInfo.Arguments; }
            set { _process.StartInfo.Arguments = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessWrapper"/> class.
        /// </summary>
        internal ProcessWrapper()
        {
            _process = new Process();

            _process.EnableRaisingEvents = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;

            _process.OutputDataReceived += _process_OutputDataReceived;
            _process.ErrorDataReceived += _process_ErrorDataReceived;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the process, while reading the output and error events from the process.
        /// </summary>
        internal void Start()
        {
            Logger.Instance.LogFormat(LogType.Trace, this, Resources.ProgramStart, FileName, Arguments);

            _process.Exited += _process_Exited;
            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();
        }

        private void _process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            Logger.Instance.LogFormat(LogType.Trace, this, Resources.ProcessDataEvent, e.Data);
        }

        private void _process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            Logger.Instance.LogFormat(LogType.Error, this, Resources.ProcessErrorEvent, e.Data);
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            _process.Exited -= _process_Exited;

            Logger.Instance.LogFormat(LogType.Trace, this, Resources.ProgramFinished, FileName, _process.ExitCode);
            _process.Dispose();
        }

        #endregion
    }
}
