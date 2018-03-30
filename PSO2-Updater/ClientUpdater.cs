using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Leayal.PSO2.Updater.Helpers;
using Leayal.Security.Cryptography;
using System.Collections.Concurrent;
using Leayal.PSO2.Updater.Events;
using System.Collections.ObjectModel;

namespace Leayal.PSO2.Updater
{
    /// <summary>
    /// Provide methods to update or verify the PSO2 game client
    /// </summary>
    public class ClientUpdater
    {
        private static readonly char[] bangonly = { '=' };
        private static readonly char[] tabonly = { Microsoft.VisualBasic.ControlChars.Tab };
        private HttpClient downloader;

        /// <summary>
        /// Initialize the class
        /// </summary>
        public ClientUpdater()
        {
            this.cancelBag = new ConcurrentBag<CancellationTokenSource>();
            this.downloader = new HttpClient(new HttpClientHandlerEx()
            {
                UseProxy = false,
                Proxy = null,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = true,
                UseDefaultCredentials = false,
                DefaultTimeout = TimeSpan.FromMinutes(5)
            }, true);
            this.downloader.DefaultRequestHeaders.UserAgent.ParseAdd(DefaultValues.Web.UserAgent);
            this.downloader.Timeout = Timeout.InfiniteTimeSpan;
        }

        /// <summary>
        /// Get the patch management from SEGA server.
        /// </summary>
        /// <returns>This will be used for version checking.</returns>
        public async Task<ClientVersionCheckResult> GetPatchManagementAsync()
        {
            string management = await this.downloader.GetStringAsync(DefaultValues.PatchInfo.PatchManagement);
            if (string.IsNullOrWhiteSpace(management))
                throw new NullReferenceException("Latest version is null. Something bad happened.");
            else
            {
                string currentline, master = null, patch = null;
                string[] splitedline;

                using (StringReader sr = new StringReader(management))
                    while (sr.Peek() > -1)
                    {
                        currentline = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(currentline))
                        {
                            splitedline = currentline.Split(bangonly, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (string.Equals(splitedline[0], "MasterURL", StringComparison.OrdinalIgnoreCase))
                                master = splitedline[1];
                            else if (string.Equals(splitedline[0], "PatchURL", StringComparison.OrdinalIgnoreCase))
                                patch = splitedline[1];
                            if (!string.IsNullOrWhiteSpace(master) && !string.IsNullOrWhiteSpace(patch))
                                break;
                        }
                    }

                if (string.IsNullOrWhiteSpace(master))
                    throw new ArgumentNullException("MasterURL is not found");
                else if (string.IsNullOrWhiteSpace(patch))
                    throw new ArgumentNullException("PatchURL is not found");
                else
                {
                    string latestver = await this.downloader.GetStringAsync(UriHelper.URLConcat(patch, "version.ver"));

                    if (string.IsNullOrWhiteSpace(latestver))
                        throw new ArgumentNullException("Latest version file is not found");
                    else
                    {
                        // this._LastKnownLatestVersion = latestver;
                        return new ClientVersionCheckResult(latestver, Settings.VersionString, master, patch);
                    }
                }
            }
        }

        /// <summary>
        /// Get the patch lists from a specific version
        /// </summary>
        /// <param name="version">Specific version to determine the patchlist</param>
        /// <returns></returns>
        public Task<RemotePatchlist> GetPatchlistAsync(ClientVersionCheckResult version) => this.GetPatchlistAsync(version, PatchListType.Master | PatchListType.Patch | PatchListType.LauncherList);

        /// <summary>
        /// Get the patch list(s) from a specific version
        /// </summary>
        /// <param name="version">Specific version to determine the patchlist</param>
        /// <param name="patchListType">Specify which patch lists will be downloaded</param>
        /// <returns></returns>
        public async Task<RemotePatchlist> GetPatchlistAsync(ClientVersionCheckResult version, PatchListType patchListType)
        {
            RemotePatchlist result = null;
            int total = 0, current = 0;
            if ((patchListType & PatchListType.Master) == PatchListType.Master)
                total++;
            if ((patchListType & PatchListType.Patch) == PatchListType.Patch)
                total++;
            if ((patchListType & PatchListType.LauncherList) == PatchListType.LauncherList)
                total++;

            if ((patchListType & PatchListType.Master) == PatchListType.Master)
            {
                using (var response = await this.downloader.GetAsync(UriHelper.URLConcat(version.MasterURL, DefaultValues.PatchInfo.called_patchlist), HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        if (result == null)
                            result = new RemotePatchlist();
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var sr = new StreamReader(responseStream))
                        {
                            string currentline;
                            while (!sr.EndOfStream)
                            {
                                currentline = sr.ReadLine();
                                if (!string.IsNullOrWhiteSpace(currentline))
                                    if (PSO2File.TryParse(currentline, version, out var _pso2file))
                                        result.AddOrUpdate(_pso2file);
                            }
                        }
                    }
                }
                Interlocked.Increment(ref current);
                this.ProgressChanged?.Invoke(current, total);
            }

            if ((patchListType & PatchListType.Patch) == PatchListType.Patch)
            {
                using (var response = await this.downloader.GetAsync(UriHelper.URLConcat(version.PatchURL, DefaultValues.PatchInfo.called_patchlist), HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        if (result == null)
                            result = new RemotePatchlist();
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var sr = new StreamReader(responseStream))
                        {
                            string currentline;
                            while (!sr.EndOfStream)
                            {
                                currentline = sr.ReadLine();
                                if (!string.IsNullOrWhiteSpace(currentline))
                                    if (PSO2File.TryParse(currentline, version, out var _pso2file))
                                        result.AddOrUpdate(_pso2file);
                            }
                        }
                    }
                }
                Interlocked.Increment(ref current);
                this.ProgressChanged?.Invoke(current, total);
            }
            if ((patchListType & PatchListType.LauncherList) == PatchListType.LauncherList)
            {
                using (var response = await this.downloader.GetAsync(UriHelper.URLConcat(version.PatchURL, DefaultValues.PatchInfo.file_launcher), HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        if (result == null)
                            result = new RemotePatchlist();
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var sr = new StreamReader(responseStream))
                        {
                            string currentline;
                            while (!sr.EndOfStream)
                            {
                                currentline = sr.ReadLine();
                                if (!string.IsNullOrWhiteSpace(currentline))
                                    if (PSO2File.TryParse(currentline, version, out var _pso2file))
                                        result.AddOrUpdate(_pso2file);
                            }
                        }
                    }
                }
                Interlocked.Increment(ref current);
                this.ProgressChanged?.Invoke(current, total);
            }
            if (result == null)
                throw new WebException();
            else
                return result;
        }

        /// <summary>
        /// Download a <seealso cref="PSO2File"/> from the server
        /// </summary>
        /// <param name="file">Specify the file</param>
        /// <param name="outStream">Output stream</param>
        /// <returns></returns>
        public Task DownloadFileAsync(PSO2File file, Stream outStream) => this.DownloadFileAsync(file, outStream, null);

        /// <summary>
        /// Download a <seealso cref="PSO2File"/> from the server
        /// </summary>
        /// <param name="file">Specify the file</param>
        /// <param name="outStream">Output stream</param>
        /// <param name="cancellationTokenSource">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns></returns>
        public async Task DownloadFileAsync(PSO2File file, Stream outStream, CancellationTokenSource cancellationTokenSource)
        {
            if (!outStream.CanWrite)
                throw new ArgumentException("The stream should be writable", "outStream");

            if (cancellationTokenSource == null)
            {
                using (var response = await this.downloader.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead))
                    if (response.IsSuccessStatusCode)
                    {
                        using (Stream srcStream = await response.Content.ReadAsStreamAsync())
                        {
                            byte[] buffer = new byte[1024];
                            int readbyte = srcStream.Read(buffer, 0, buffer.Length);
                            while (readbyte > 0)
                            {
                                outStream.Write(buffer, 0, readbyte);
                                readbyte = srcStream.Read(buffer, 0, buffer.Length);
                            }
                            outStream.Flush();
                        }
                    }
            }
            else
            {
                using (var response = await this.downloader.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token))
                    if (response.IsSuccessStatusCode)
                    {
                        using (Stream srcStream = await response.Content.ReadAsStreamAsync())
                            if (!cancellationTokenSource.IsCancellationRequested)
                            {
                                byte[] buffer = new byte[1024];
                                int readbyte = srcStream.Read(buffer, 0, buffer.Length);
                                while (readbyte > 0 && !cancellationTokenSource.IsCancellationRequested)
                                {
                                    outStream.Write(buffer, 0, readbyte);
                                    readbyte = srcStream.Read(buffer, 0, buffer.Length);
                                }
                                outStream.Flush();
                            }
                    }
            }
        }

        /// <summary>
        /// Shortcut methods for <seealso cref="GetPatchlistAsync(ClientVersionCheckResult)"/> and <seealso cref="VerifyAndDownloadAsync(string, ClientVersionCheckResult, RemotePatchlist, ClientUpdateOptions)"/>
        /// </summary>
        /// <param name="clientDirectory">The pso2_dir directory</param>
        /// <param name="version">Specify the version</param>
        /// <returns></returns>
        public Task UpdateClientAsync(string clientDirectory, ClientVersionCheckResult version) => this.UpdateClientAsync(clientDirectory, version, new ClientUpdateOptions());

        /// <summary>
        /// Shortcut methods for <seealso cref="GetPatchlistAsync(ClientVersionCheckResult)"/> and <seealso cref="VerifyAndDownloadAsync(string, ClientVersionCheckResult, RemotePatchlist, ClientUpdateOptions)"/>
        /// </summary>
        /// <param name="clientDirectory">The pso2_dir directory</param>
        /// <param name="version">Specify the version</param>
        /// <param name="options">Provide the options for current download sessions</param>
        /// <returns></returns>
        public async Task UpdateClientAsync(string clientDirectory, ClientVersionCheckResult version, ClientUpdateOptions options)
        {
            RemotePatchlist patchlist = await this.GetPatchlistAsync(version);
            this.VerifyAndDownloadAsync(clientDirectory, version, patchlist, options);
        }

        /// <summary>
        /// Verify and redownload any missing/old files
        /// </summary>
        /// <param name="clientDirectory">The pso2_dir directory</param>
        /// <param name="version">Specify the version</param>
        /// <param name="filelist">Specify the patchlist to check and download</param>
        /// <param name="options">Provide the options for current download sessions</param>
        public void VerifyAndDownloadAsync(string clientDirectory, ClientVersionCheckResult version, RemotePatchlist filelist, ClientUpdateOptions options)
        {
            ConcurrentDictionary<PSO2File, Exception> failedfiles = new ConcurrentDictionary<PSO2File, Exception>();

            int currentprogress = 0, downloadedfiles = 0;

            CancellationTokenSource totalCancelSource = new CancellationTokenSource();
            options.ParallelOptions.CancellationToken = totalCancelSource.Token;

            //*
            options.ParallelOptions.CancellationToken.Register(() =>
            {
                if (options.ChecksumCache != null && downloadedfiles > 0)
                {
                    this.StepChanged?.Invoke(UpdateStep.WriteCache, options.ChecksumCache);
                    options.ChecksumCache.WriteChecksumCache(version.LatestVersion);
                }
                options.Dispose();
                this.UpdateCompleted?.Invoke(new PSO2NotifyEventArgs(true, clientDirectory, new ReadOnlyDictionary<PSO2File, Exception>(failedfiles)));
            });
            //*/

            this.cancelBag.Add(totalCancelSource);

            this.StepChanged.Invoke(UpdateStep.BeginFileCheckAndDownload, null);

            if (options.ChecksumCache == null)
                options.Profile = UpdaterProfile.PreferAccuracy;
            else
                options.ChecksumCache.ReadChecksumCache();
            try
            {
                switch (options.Profile)
                {
                    case UpdaterProfile.PreferAccuracy:
                        Parallel.ForEach(filelist.Values, options.ParallelOptions, (pso2file, state) =>
                        {
                            if (totalCancelSource.IsCancellationRequested)
                                state.Stop();
                            string fullpath = Path.Combine(clientDirectory, pso2file.WindowFilename);
                            try
                            {
                                if (string.Equals(pso2file.SafeFilename, DefaultValues.CensorFilename, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (File.Exists(fullpath))
                                    {
                                        string md5fromfile = MD5Wrapper.HashFromFile(fullpath);
                                        if (!string.Equals(md5fromfile, pso2file.MD5Hash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);
                                            using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                                try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                            File.Delete(fullpath);
                                            File.Move(fullpath + ".dtmp", fullpath);
                                            Interlocked.Increment(ref downloadedfiles);
                                            if (options.ChecksumCache != null)
                                            {
                                                ChecksumCache.PSO2FileChecksum newchecksum = ChecksumCache.PSO2FileChecksum.FromFile(clientDirectory, fullpath);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                            }
                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                        }
                                        else
                                        {
                                            if (!options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                            {
                                                ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, pso2file.Length, pso2file.MD5Hash);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                Interlocked.Increment(ref downloadedfiles);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fullpath = Path.Combine(clientDirectory, pso2file.WindowFilename + ".backup");
                                        if (File.Exists(fullpath))
                                        {
                                            string md5fromfile = MD5Wrapper.HashFromFile(fullpath);
                                            if (!string.Equals(md5fromfile, pso2file.MD5Hash, StringComparison.OrdinalIgnoreCase))
                                            {
                                                this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                                using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                                    try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                                File.Delete(fullpath);
                                                File.Move(fullpath + ".dtmp", fullpath);
                                                Interlocked.Increment(ref downloadedfiles);
                                                if (options.ChecksumCache != null)
                                                {
                                                    ChecksumCache.PSO2FileChecksum newchecksum = ChecksumCache.PSO2FileChecksum.FromFile(clientDirectory, fullpath);
                                                    options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                }

                                                this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                            }
                                            else
                                            {
                                                if (!options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                                {
                                                    ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, pso2file.Length, pso2file.MD5Hash);
                                                    options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                    Interlocked.Increment(ref downloadedfiles);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (File.Exists(fullpath))
                                    {
                                        string md5fromfile = MD5Wrapper.HashFromFile(fullpath);
                                        if (!string.Equals(md5fromfile, pso2file.MD5Hash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                            using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                                try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                            File.Delete(fullpath);
                                            File.Move(fullpath + ".dtmp", fullpath);
                                            Interlocked.Increment(ref downloadedfiles);
                                            if (options.ChecksumCache != null)
                                            {
                                                ChecksumCache.PSO2FileChecksum newchecksum = ChecksumCache.PSO2FileChecksum.FromFile(clientDirectory, fullpath);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                            }
                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                        }
                                        else
                                        {
                                            if (!options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                            {
                                                ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, pso2file.Length, pso2file.MD5Hash);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                Interlocked.Increment(ref downloadedfiles);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                        using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                            try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                        File.Delete(fullpath);
                                        File.Move(fullpath + ".dtmp", fullpath);
                                        Interlocked.Increment(ref downloadedfiles);
                                        if (options.ChecksumCache != null)
                                        {
                                            ChecksumCache.PSO2FileChecksum newchecksum = ChecksumCache.PSO2FileChecksum.FromFile(clientDirectory, fullpath);
                                            options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                        }
                                        this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                    }
                                }
                            }
#if !DEBUG
                            catch (Exception ex)
                            {
                                failedfiles.TryAdd(pso2file, ex);
                            }
#endif
                            finally
                            {
                                try { File.Delete(fullpath + ".dtmp"); } catch { }
                                Interlocked.Increment(ref currentprogress);
                                this.ProgressChanged?.Invoke(currentprogress, filelist.Count);
                            }
                        });
                        break;
                    case UpdaterProfile.PreferSpeed:
                        Parallel.ForEach(filelist.Values, options.ParallelOptions, (pso2file, state) =>
                        {
                            if (totalCancelSource.IsCancellationRequested)
                                state.Stop();
                            string fullpath = Path.Combine(clientDirectory, pso2file.WindowFilename);
                            try
                            {
                                if (!string.Equals(pso2file.SafeFilename, DefaultValues.CensorFilename, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                    {
                                        if (!string.Equals(options.ChecksumCache.ChecksumList[pso2file.Filename].MD5, pso2file.MD5Hash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                            using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                                try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                            File.Delete(fullpath);
                                            File.Move(fullpath + ".dtmp", fullpath);
                                            Interlocked.Increment(ref downloadedfiles);
                                            ChecksumCache.PSO2FileChecksum newchecksum = ChecksumCache.PSO2FileChecksum.FromFile(clientDirectory, fullpath);
                                            options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));

                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                        }
                                    }
                                    else
                                    {
                                        this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                        using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                            try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                        File.Delete(fullpath);
                                        File.Move(fullpath + ".dtmp", fullpath);
                                        Interlocked.Increment(ref downloadedfiles);
                                        ChecksumCache.PSO2FileChecksum newchecksum = ChecksumCache.PSO2FileChecksum.FromFile(clientDirectory, fullpath);
                                        options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));

                                        this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                    }
                                }
                            }
#if !DEBUG
                            catch (Exception ex)
                            {
                                failedfiles.TryAdd(pso2file, ex);
                            }
#endif
                            finally
                            {
                                try { File.Delete(fullpath + ".dtmp"); } catch { }
                                Interlocked.Increment(ref currentprogress);
                                this.ProgressChanged?.Invoke(currentprogress, filelist.Count);
                            }
                        });
                        break;
                    default:
                        Parallel.ForEach(filelist.Values, options.ParallelOptions, (pso2file, state) =>
                        {
                            if (totalCancelSource.IsCancellationRequested)
                                state.Stop();
                            string fullpath = Path.Combine(clientDirectory, pso2file.WindowFilename);
                            try
                            {
                                if (string.Equals(pso2file.SafeFilename, DefaultValues.CensorFilename, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (File.Exists(fullpath))
                                    {
                                        string md5fromfile = null;
                                        if (options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                        {
                                            var checksumfile = options.ChecksumCache.ChecksumList[pso2file.Filename];
                                            using (FileStream fs = File.OpenRead(fullpath))
                                                if (fs.Length == checksumfile.FileSize)
                                                    md5fromfile = checksumfile.MD5;
                                        }
                                        if (string.IsNullOrEmpty(md5fromfile))
                                            md5fromfile = MD5Wrapper.HashFromFile(fullpath);

                                        if (!string.Equals(md5fromfile, pso2file.MD5Hash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                            using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                            {
                                                try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                                ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, fs.Length, pso2file.MD5Hash);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                            }
                                            File.Delete(fullpath);
                                            File.Move(fullpath + ".dtmp", fullpath);
                                            Interlocked.Increment(ref downloadedfiles);

                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                        }
                                        else
                                        {
                                            if (!options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                            {
                                                ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, pso2file.Length, pso2file.MD5Hash);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                Interlocked.Increment(ref downloadedfiles);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fullpath = Path.Combine(clientDirectory, pso2file.Filename + ".backup");
                                        if (File.Exists(fullpath))
                                        {
                                            string md5fromfile = null;
                                            if (options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                            {
                                                var checksumfile = options.ChecksumCache.ChecksumList[pso2file.Filename];
                                                using (FileStream fs = File.OpenRead(fullpath))
                                                    if (fs.Length == checksumfile.FileSize)
                                                        md5fromfile = checksumfile.MD5;
                                            }
                                            if (string.IsNullOrEmpty(md5fromfile))
                                                md5fromfile = MD5Wrapper.HashFromFile(fullpath);

                                            if (!string.Equals(md5fromfile, pso2file.MD5Hash, StringComparison.OrdinalIgnoreCase))
                                            {
                                                this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                                using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                                {
                                                    try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                                    ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, fs.Length, pso2file.MD5Hash);
                                                    options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                }
                                                File.Delete(fullpath);
                                                File.Move(fullpath + ".dtmp", fullpath);
                                                Interlocked.Increment(ref downloadedfiles);

                                                this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                            }
                                            else
                                            {
                                                if (!options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                                {
                                                    ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, pso2file.Length, pso2file.MD5Hash);
                                                    options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                    Interlocked.Increment(ref downloadedfiles);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (File.Exists(fullpath))
                                    {
                                        string md5fromfile = null;
                                        if (options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                        {
                                            var checksumfile = options.ChecksumCache.ChecksumList[pso2file.Filename];
                                            using (FileStream fs = File.OpenRead(fullpath))
                                                if (fs.Length == checksumfile.FileSize)
                                                    md5fromfile = checksumfile.MD5;
                                        }
                                        if (string.IsNullOrEmpty(md5fromfile))
                                            md5fromfile = MD5Wrapper.HashFromFile(fullpath);

                                        if (!string.Equals(md5fromfile, pso2file.MD5Hash, StringComparison.OrdinalIgnoreCase))
                                        {
                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                            using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                            {
                                                try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                                ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, fs.Length, pso2file.MD5Hash);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                            }
                                            File.Delete(fullpath);
                                            File.Move(fullpath + ".dtmp", fullpath);
                                            Interlocked.Increment(ref downloadedfiles);

                                            this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                        }
                                        else
                                        {
                                            if (!options.ChecksumCache.ChecksumList.ContainsKey(pso2file.Filename))
                                            {
                                                ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, pso2file.Length, pso2file.MD5Hash);
                                                options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                                Interlocked.Increment(ref downloadedfiles);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        this.StepChanged?.Invoke(UpdateStep.DownloadingFileStart, pso2file);

                                        using (FileStream fs = File.Create(fullpath + ".dtmp"))
                                        {
                                            try { this.DownloadFileAsync(pso2file, fs, totalCancelSource).GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
                                            ChecksumCache.PSO2FileChecksum newchecksum = new ChecksumCache.PSO2FileChecksum(pso2file.Filename, fs.Length, pso2file.MD5Hash);
                                            options.ChecksumCache.ChecksumList.AddOrUpdate(pso2file.Filename, newchecksum, new Func<string, ChecksumCache.PSO2FileChecksum, ChecksumCache.PSO2FileChecksum>((key, oldval) => { return newchecksum; }));
                                        }
                                        File.Delete(fullpath);
                                        File.Move(fullpath + ".dtmp", fullpath);
                                        Interlocked.Increment(ref downloadedfiles);

                                        this.StepChanged?.Invoke(UpdateStep.DownloadingFileEnd, pso2file);
                                    }
                                }
                            }
#if !DEBUG
                            catch (Exception ex)
                            {
                                failedfiles.TryAdd(pso2file, ex);
                            }
#endif
                            finally
                            {
                                try { File.Delete(fullpath + ".dtmp"); } catch { }
                                Interlocked.Increment(ref currentprogress);
                                this.ProgressChanged?.Invoke(currentprogress, filelist.Count);
                            }
                        });
                        break;
                }

                if (options.ChecksumCache != null && downloadedfiles > 0)
                {
                    this.StepChanged?.Invoke(UpdateStep.WriteCache, options.ChecksumCache);
                    options.ChecksumCache.WriteChecksumCache(version.LatestVersion);
                }
                options.Dispose();
                if (!totalCancelSource.IsCancellationRequested)
                {
                    if (failedfiles.Count < 4)
                        Settings.VersionString = version.LatestVersion;
                    this.UpdateCompleted?.Invoke(new PSO2NotifyEventArgs(version.LatestVersion, clientDirectory, new ReadOnlyDictionary<PSO2File, Exception>(failedfiles)));
                }
                else
                    this.UpdateCompleted?.Invoke(new PSO2NotifyEventArgs(true, clientDirectory, new ReadOnlyDictionary<PSO2File, Exception>(failedfiles)));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (options.ChecksumCache != null && downloadedfiles > 0)
                {
                    this.StepChanged?.Invoke(UpdateStep.WriteCache, options.ChecksumCache);
                    options.ChecksumCache.WriteChecksumCache(version.LatestVersion);
                }
                options.Dispose();
                throw ex;
            }
        }

        /// <summary>
        /// Callback event when the async operation is finished. <seealso cref="VerifyAndDownloadAsync(string, ClientVersionCheckResult, RemotePatchlist, ClientUpdateOptions)"/> will also trigger this event upon complete.
        /// </summary>
        public event Action<PSO2NotifyEventArgs> UpdateCompleted;
        /// <summary>
        /// Callback event to report the progress of the current operation
        /// </summary>
        public event Action<int, int> ProgressChanged;
        /// <summary>
        /// Callback event to report the current step of the current operation
        /// </summary>
        public event Action<UpdateStep, object> StepChanged;

        private ConcurrentBag<CancellationTokenSource> cancelBag;

        /// <summary>
        /// Cancel all async operations
        /// </summary>
        public void CancelDownloadOperations()
        {
            while (cancelBag.Count > 0)
                if (cancelBag.TryTake(out var cancellation))
                    cancellation.Cancel();
            this.downloader.CancelPendingRequests();
        }
    }
}