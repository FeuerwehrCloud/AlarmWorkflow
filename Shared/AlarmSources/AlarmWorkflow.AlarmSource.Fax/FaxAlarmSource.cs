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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AlarmWorkflow.AlarmSource.Fax.Extensibility;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Diagnostics;
using AlarmWorkflow.Shared.Extensibility;

namespace AlarmWorkflow.AlarmSource.Fax
{
    /// <summary>
    /// Implements the <see cref="IAlarmSource"/>-interface to provide an alarm source that handles incoming faxes.
    /// </summary>
    [Export("FaxAlarmSource", typeof(IAlarmSource))]
    [Information(DisplayName = "ExportAlarmSourceDisplayName", Description = "ExportAlarmSourceDescription")]
    sealed class FaxAlarmSource : IAlarmSource
    {
        #region Constants

        private const int ErrorRetryCount = 10;

        #endregion

        #region Fields

        private FaxConfiguration _configuration;

        private DirectoryInfo _faxPath;
        private DirectoryInfo _archivePath;
        private DirectoryInfo _analysisPath;

        private IOcrSoftware _ocrSoftware;
        private IParser _parser;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FaxAlarmSource"/> class.
        /// </summary>
        public FaxAlarmSource()
        {
            _configuration = new FaxConfiguration();
        }

        #endregion

        #region Methods

        private void InitializeSettings()
        {
            _faxPath = new DirectoryInfo(_configuration.FaxPath);
            _archivePath = new DirectoryInfo(_configuration.ArchivePath);
            _analysisPath = new DirectoryInfo(_configuration.AnalysisPath);

            InitializeOcrSoftware();

            // Import parser with the given name/alias
            _parser = ExportedTypeLibrary.Import<IParser>(_configuration.AlarmFaxParserAlias);
            Logger.Instance.LogFormat(LogType.Info, this, "Using parser '{0}'.", _parser.GetType().FullName);
        }

        private void InitializeOcrSoftware()
        {
            AssertCustomOcrPathExist();

            _ocrSoftware = ExportedTypeLibrary.Import<IOcrSoftware>(_configuration.OCRSoftware);
            Logger.Instance.LogFormat(LogType.Info, this, Properties.Resources.InitializeUsingOcrSoftware, _configuration.OCRSoftware);
        }

        private void AssertCustomOcrPathExist()
        {
            if (string.IsNullOrWhiteSpace(_configuration.OCRSoftwarePath))
            {
                return;
            }

            if (Directory.Exists(_configuration.OCRSoftwarePath))
            {
                return;
            }

            throw new DirectoryNotFoundException(string.Format("The OCR software '{0}' was suggested to be found in path '{1}', which doesn't exist!", _configuration.OCRSoftware, _configuration.OCRSoftwarePath));
        }

        /// <summary>
        /// Makes sure that the required directories exist and we don't run into unnecessary exceptions.
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            try
            {
                _faxPath.Refresh();
                _archivePath.Refresh();
                _analysisPath.Refresh();

                if (!_faxPath.Exists)
                {
                    _faxPath.Create();
                    Logger.Instance.LogFormat(LogType.Trace, this, "Created required directory '{0}'.", _faxPath.FullName);
                }
                if (!_archivePath.Exists)
                {
                    Logger.Instance.LogFormat(LogType.Trace, this, "Created required directory '{0}'.", _archivePath.FullName);
                    _archivePath.Create();
                }
                if (!_analysisPath.Exists)
                {
                    Logger.Instance.LogFormat(LogType.Trace, this, "Created required directory '{0}'.", _analysisPath.FullName);
                    _analysisPath.Create();
                }
            }
            catch (IOException)
            {
                Logger.Instance.LogFormat(LogType.Warning, this, "Could not create any of the default directories. Try running the process as Administrator, or create the directories in advance.");
            }
        }

        private void ProcessNewImage(FileInfo file)
        {
            EnsureDirectoriesExist();

            string analyseFileName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            string archivedFilePath = Path.Combine(_archivePath.FullName, analyseFileName + ".tif");

            // Moves the file to a different location, and throws if it failed.
            MoveFileTo(file, archivedFilePath);

            List<string> analyzedLines = new List<string>();
            Stopwatch swParse = new Stopwatch();

            string[] parsedLines = null;
            try
            {
                OcrProcessOptions options = new OcrProcessOptions();
                options.SoftwarePath = _configuration.OCRSoftwarePath;
                options.AnalyzedFileDestinationPath = Path.Combine(_analysisPath.FullName, Path.GetFileNameWithoutExtension(file.FullName));
                options.ImagePath = file.FullName;

                Logger.Instance.LogFormat(LogType.Trace, this, Properties.Resources.OcrSoftwareParseBegin, file.FullName);

                swParse.Start();

                parsedLines = _ocrSoftware.ProcessImage(options);

                swParse.Stop();

                Logger.Instance.LogFormat(LogType.Trace, this, Properties.Resources.OcrSoftwareParseEndSuccess, swParse.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                swParse.Stop();

                Logger.Instance.LogFormat(LogType.Error, this, Properties.Resources.OcrSoftwareParseEndFail);
                Logger.Instance.LogException(this, ex);
                // Abort parsing
                // TODO: Introduce own exception for this!
                return;
            }

            // After the file has been parsed, read it back in ...
            // ... fetch all lines ...
            foreach (string preParsedLine in parsedLines)
            {
                // ... and add it to the list (
                analyzedLines.Add(AlarmWorkflowConfiguration.Instance.ReplaceDictionary.ReplaceInString(preParsedLine));
            }

            Operation operation = null;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                // Try to parse the operation. If parsing failed, ignore this but write to the log file!
                Logger.Instance.LogFormat(LogType.Trace, this, "Begin parsing incoming operation...");

                string[] lines = analyzedLines.ToArray();
                // Find out if the fax is a test-fax
                if (IsTestFax(lines))
                {
                    sw.Stop();
                    Logger.Instance.LogFormat(LogType.Trace, this, "Operation is a test-fax. Parsing is skipped.");
                }
                else
                {
                    operation = _parser.Parse(lines);

                    sw.Stop();
                    Logger.Instance.LogFormat(LogType.Trace, this, "Parsed operation in '{0}' milliseconds.", sw.ElapsedMilliseconds);

                    // If there is no timestamp, use the current time. Not too good but better than MinValue :-/
                    if (operation.Timestamp == DateTime.MinValue)
                    {
                        Logger.Instance.LogFormat(LogType.Warning, this, "Could not parse timestamp from the fax. Using the current time as the timestamp.");
                        operation.Timestamp = DateTime.Now;
                    }

                    Dictionary<string, object> ctxParameters = new Dictionary<string, object>();
                    ctxParameters["ArchivedFilePath"] = archivedFilePath;
                    ctxParameters["ImagePath"] = file.FullName;

                    AlarmSourceEventArgs args = new AlarmSourceEventArgs(operation);
                    args.Parameters = ctxParameters;

                    // Raise event...
                    OnNewAlarm(args);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.Instance.LogFormat(LogType.Warning, this, "An exception occurred while processing the alarmfax!");
                Logger.Instance.LogException(this, ex);
            }
        }

        private void MoveFileTo(FileInfo file, string archivedFilePath)
        {
            bool fileIsMoved = false;
            int tried = 0;
            while (!fileIsMoved)
            {
                tried++;
                try
                {
                    file.MoveTo(archivedFilePath);
                    fileIsMoved = true;
                }
                catch (IOException ex)
                {
                    if (tried < ErrorRetryCount)
                    {
                        Logger.Instance.LogFormat(LogType.Warning, this, "Coudn't move file. Try {0} of {1}!", tried, ErrorRetryCount);
                        Thread.Sleep(200);
                        fileIsMoved = false;
                    }
                    else
                    {
                        // Don't throw exception here, it will effectively stop the service. Issue warning and go ahead.
                        Logger.Instance.LogFormat(LogType.Error, this, "Coundn't move file. See log for more details.");
                        Logger.Instance.LogException(this, ex);
                    }
                }
            }
        }

        // Checks the raw line contents for any occurrences of test-fax keywords.
        private bool IsTestFax(string[] lines)
        {
            return lines.Any(l => _configuration.TestFaxKeywords.Any(kw => l.Contains(kw)));
        }

        #endregion

        #region IAlarmSource Members

        /// <summary>
        /// Raised when a new alarm has surfaced and processed for the Engine to handle.
        /// See documentation for further information.
        /// </summary>
        public event EventHandler<AlarmSourceEventArgs> NewAlarm;

        private void OnNewAlarm(AlarmSourceEventArgs args)
        {
            var copy = NewAlarm;
            if (copy != null)
            {
                copy(this, args);
            }
        }

        void IAlarmSource.Initialize()
        {
            InitializeSettings();
        }

        void IAlarmSource.RunThread()
        {
            Logger.Instance.LogFormat(LogType.Trace, this, "Using directory '{0}' for incoming faxes.", _faxPath.FullName);
            Logger.Instance.LogFormat(LogType.Trace, this, "Using directory '{0}' for analyzed faxes.", _analysisPath.FullName);
            Logger.Instance.LogFormat(LogType.Trace, this, "Using directory '{0}' for archived faxes.", _archivePath.FullName);

            EnsureDirectoriesExist();

            while (true)
            {
                FileInfo[] files = _faxPath.GetFiles("*.tif", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    Logger.Instance.LogFormat(LogType.Trace, this, "Processing '{0}' new faxes...", files.Length);

                    foreach (FileInfo file in files)
                    {
                        ProcessNewImage(file);
                    }

                    Logger.Instance.LogFormat(LogType.Trace, this, "Processing finished.");
                }
                Thread.Sleep(_configuration.RoutineInterval);
            }
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {

        }

        #endregion
    }
}