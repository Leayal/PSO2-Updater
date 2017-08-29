using System;
using System.Collections.Generic;
using System.Threading;
using Leayal.PSO2.Updater;
using Leayal.PSO2.Updater.Events;
using Leayal;
using Microsoft.VisualBasic.ApplicationServices;
using System.IO;
using System.Windows.Forms;

namespace PSO2_Updater_Console
{
    internal class ConsoleController : WindowsFormsApplicationBase
    {
        private IntPtr consolePointer;
        private PSO2UpdateManager pso2updatemng;
        private SynchronizationContext syncConctext;

        string pso2directory, logoutput, cache;
        bool checkfileMode;

        private StreamWriter logStream;

        public ConsoleController() : base(AuthenticationMode.Windows)
        {
            this.pso2directory = null;
            this.logoutput = null;
            this.checkfileMode = false;
            this.EnableVisualStyles = false;
            this.IsSingleInstance = true;
            this.ShutdownStyle = ShutdownMode.AfterMainFormCloses;
            this.syncConctext = SynchronizationContext.Current;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            this.consolePointer = NativeMethods.GetConsoleWindow();
            Console.BackgroundColor = ConsoleColor.Black;

            Console.Title = "PSO2 Updater - Console";
            Console.CancelKeyPress += Console_CancelKeyPress;

            string currentArg;
            for (int i = 0; i < eventArgs.CommandLine.Count; i++)
            {
                currentArg = eventArgs.CommandLine[i];
                if (!string.IsNullOrWhiteSpace(currentArg)) {
                    if (currentArg.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    {
                        currentArg = currentArg.Remove(0, 1);
                        if (currentArg.StartsWith("dir:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (currentArg.Length > 4)
                                this.pso2directory = currentArg.Remove(0, 4);
                        }
                    }
                    else if (currentArg.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                    {
                        currentArg = currentArg.Remove(0, 1);
                        if (currentArg.IsEqual("check", true))
                            this.checkfileMode = true;
                        else if (currentArg.StartsWith("log:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (currentArg.Length > 4)
                                this.logoutput = currentArg.Remove(0, 4);
                        }
                        else if (currentArg.StartsWith("cache:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (currentArg.Length > 4)
                                this.cache = currentArg.Remove(0, 6);
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(this.pso2directory))
            {
                this.PrintGuide();
                Console.WriteLine();
                Console.WriteLine("Press any key to close.");
                Console.ReadKey();

                return false;
            }
            else
            {
                this.pso2directory = Path.GetFullPath(this.pso2directory);
                if (!string.IsNullOrWhiteSpace(this.logoutput))
                    this.logoutput = Path.GetFullPath(this.logoutput);

                // Clear log
                if (!string.IsNullOrWhiteSpace(this.logoutput))
                    this.logStream = new StreamWriter(Path.GetFullPath(this.logoutput), false, System.Text.Encoding.UTF8);

                this.CreateManager();
                Leayal.PSO2.Updater.ChecksumCache.ChecksumCache cachefile = null;

                if (!string.IsNullOrWhiteSpace(this.cache))
                {
                    this.cache = Path.GetFullPath(this.cache);
                    cachefile = Leayal.PSO2.Updater.ChecksumCache.ChecksumCache.OpenFromFile(this.cache);
                }

                Helpers.DummyForm dummy = new Helpers.DummyForm();
                dummy.FormClosed += Dummy_FormClosed;
                this.MainForm = dummy;

                if (cachefile != null)
                    this.pso2updatemng.ChecksumCache = cachefile;

                if (this.checkfileMode)
                    this.pso2updatemng.CheckLocalFiles(this.pso2directory);
                else
                    this.pso2updatemng.UpdateGame(this.pso2directory);
                

                return true;
            }
        }

        private void Dummy_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            if (this.consolePointer != IntPtr.Zero)
                NativeMethods.SetForegroundWindow(this.consolePointer);
        }

        private void PSO2UpdateManager_PSO2InstallCancelled(object sender, PSO2NotifyEventArgs e)
        {
            this.RemoveCacheFromManager();
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                Helpers.TaskbarItemInfo.TaskbarProgress.SetState(this.consolePointer, Helpers.TaskbarItemInfo.TaskbarProgress.TaskbarStates.NoProgress);
                Console.WriteLine("Press any key to close.");
                if (!this.pendingCloseForm)
                    Console.ReadKey(false);
                this.MainForm.Close();
            }), null);
        }

        private void PSO2UpdateManager_PSO2Installed(object sender, PSO2NotifyEventArgs e)
        {
            this.RemoveCacheFromManager();
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                Helpers.TaskbarItemInfo.TaskbarProgress.SetState(this.consolePointer, Helpers.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Indeterminate);
                if (e.Cancelled)
                {
                    Console.WriteLine("Press any key to close.");
                    if (!this.pendingCloseForm)
                        Console.ReadKey(false);
                    this.MainForm.Close();
                }
                else
                {
                    try { NativeMethods.FlashWindow(this.consolePointer, 10); } catch { }
                    if (e.FailedList == null)
                        this.ConsoleWriteOut("PSO2 has been downloaded successfully to latest version.");
                    else
                    {
                        if (e.FailedList.Count < 3)
                            this.ConsoleWriteOut(string.Format("PSO2 has been downloaded successfully to latest version. But there are {0} files have failed to be downloaded.", e.FailedList.Count));
                        else
                            this.ConsoleWriteOut(string.Format("PSO2 has failed to be downloaded. {0} files have failed to be downloaded.", e.FailedList.Count));
                    }
                    Console.WriteLine("Press any key to close.");
                    if (!this.pendingCloseForm)
                        Console.ReadKey(false);
                    this.MainForm.Close();
                }
            }), null);
        }

        private void PSO2UpdateManager_HandledException(object sender, HandledExceptionEventArgs e)
        {
            this.RemoveCacheFromManager();
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                Helpers.TaskbarItemInfo.TaskbarProgress.SetState(this.consolePointer, Helpers.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Error);
                this.ConsoleWriteOut(e.Error.ToString(), ConsoleColor.Red);
                Console.WriteLine("Press any key to close.");
                if (!this.pendingCloseForm)
                    Console.ReadKey(false);
                this.MainForm.Close();
            }), null);
        }

        private void PSO2UpdateManager_CurrentTotalProgressChanged(object sender, ProgressEventArgs e)
        {
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                this.progressmax = e.Progress;
            }), null);
        }
        int progresscurrent, progressmax;
        private void PSO2UpdateManager_CurrentProgressChanged(object sender, ProgressEventArgs e)
        {
            this.syncConctext.Post(new SendOrPostCallback(delegate
            {
                this.progresscurrent = e.Progress;
                this.ConsoleWriteOut(string.Format("Checking ({0}/{1})", e.Progress, this.progressmax));
                Helpers.TaskbarItemInfo.TaskbarProgress.SetValue(this.consolePointer, e.Progress, this.progressmax);
            }), null);
        }

        private void PSO2UpdateManager_CurrentStepChanged(object sender, StepEventArgs e)
        {
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                switch (e.Step)
                {
                    case UpdateStep.PSO2UpdateManager_BuildingFileList:
                        Helpers.TaskbarItemInfo.TaskbarProgress.SetState(this.consolePointer, Helpers.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Indeterminate);
                        this.ConsoleWriteOut("Preparing file list");
                        break;
                    case UpdateStep.PSO2UpdateManager_DownloadingFileStart:
                        this.ConsoleWriteOut(string.Format("Downloading ({0}/{1}): {2}", this.progresscurrent, this.progressmax, e.Value));
                        break;
                    case UpdateStep.PSO2UpdateManager_DownloadingPatchList:
                        Helpers.TaskbarItemInfo.TaskbarProgress.SetState(this.consolePointer, Helpers.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Indeterminate);
                        this.ConsoleWriteOut(string.Format("Downloading patch list {0}", e.Value));
                        break;
                    case UpdateStep.PSO2Updater_BeginFileCheckAndDownload:
                        Helpers.TaskbarItemInfo.TaskbarProgress.SetState(this.consolePointer, Helpers.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Normal);
                        this.ConsoleWriteOut("Begin checking and download old/missing files");
                        break;
                }
            }), null);
        }

        private void ConsoleWriteOut(string str)
        {
            this.ConsoleWriteOut(str, ConsoleColor.White);
        }
        private void ConsoleWriteOut(string str, ConsoleColor color)
        {
            if (Console.ForegroundColor != color)
                Console.ForegroundColor = color;
            Console.WriteLine(str);
            if (this.logStream != null)
                this.logStream.WriteLine(str);
        }

        private bool pendingCloseForm;
        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (this.pso2updatemng != null && this.pso2updatemng.IsBusy)
            {
                e.Cancel = true;
                this.pendingCloseForm = true;
                this.pso2updatemng.CancelAsync();
            }
        }

        private void RemoveCacheFromManager()
        {
            if (this.pso2updatemng.ChecksumCache != null)
            {
                this.pso2updatemng.ChecksumCache.Dispose();
                this.pso2updatemng.ChecksumCache = null;
            }
        }

        private void CreateManager()
        {
            this.pso2updatemng = new PSO2UpdateManager();

            // Progress handling
            this.pso2updatemng.CurrentStepChanged += PSO2UpdateManager_CurrentStepChanged;
            this.pso2updatemng.CurrentProgressChanged += PSO2UpdateManager_CurrentProgressChanged;
            this.pso2updatemng.CurrentTotalProgressChanged += PSO2UpdateManager_CurrentTotalProgressChanged;

            // Kind of callback
            this.pso2updatemng.HandledException += PSO2UpdateManager_HandledException;
            this.pso2updatemng.PSO2Installed += PSO2UpdateManager_PSO2Installed;
            this.pso2updatemng.PSO2InstallCancelled += PSO2UpdateManager_PSO2InstallCancelled;
        }

        private void PrintGuide(bool newscreen = false)
        {
            if (newscreen && !NativeMethods.IsOutputRedirected)
                Console.Clear();
            Console.WriteLine("PSO2 Updater Console Application by Dramiel Leayal.");
            Console.WriteLine($"Usage: {Path.GetFileName(AppInfo.ApplicationFilename)} [-check] [-log:<path>] [-cache:<path>] /dir:<path>");
            Console.WriteLine();
            Console.WriteLine("       /dir:   Required. Specify where the PSO2's \"pso2_bin\" directory is.");
            Console.WriteLine("       -check: Optional. Determine if the updater should skip version check");
            Console.WriteLine("                         and force to check all files.");
            Console.WriteLine("       -log:   Optional. Specify where the output log is.");
            Console.WriteLine("       -cache: Optional. Specify where the Checksum cache will be used,");
            Console.WriteLine("                         if the file is not exist, it will be created.");
        }
    }
}
