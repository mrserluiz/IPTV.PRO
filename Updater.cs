using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Collections.Generic;

internal static class AppUpdater
{
    const string Repository = "mrserluiz/IPTV.PRO";
    const int MaximumExeBytes = 64 * 1024 * 1024;
    internal static Version CurrentVersion { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

    internal sealed class Package
    {
        internal Version Version;
        internal string Path;
        internal string Digest;
    }

    static Version ReadTag(string tag)
    {
        Version version;
        return tag != null && Regex.IsMatch(tag, @"\Av[0-9]+\.[0-9]+\.[0-9]+\z")
            && Version.TryParse(tag.Substring(1), out version) ? version : null;
    }

    static byte[] Download(string url, int limit)
    {
        var destination = new Uri(url);
        var clock = Stopwatch.StartNew();
        for (int redirects = 0; redirects <= 5; redirects++)
        {
            if (destination.Scheme != Uri.UriSchemeHttps)
                throw new IOException("A atualização exige HTTPS.");
            var request = (HttpWebRequest)WebRequest.Create(destination);
            request.AllowAutoRedirect = false;
            request.Timeout = request.ReadWriteTimeout = 8000;
            request.UserAgent = "XCIPTV-PC/" + CurrentVersion;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                int code = (int)response.StatusCode;
                if (code >= 300 && code < 400)
                {
                    destination = new Uri(destination, response.Headers["Location"]);
                    continue;
                }
                if (code != 200 || response.ContentLength > limit)
                    throw new IOException("Resposta de atualização inválida.");
                using (var stream = response.GetResponseStream())
                using (var data = new MemoryStream())
                {
                    var buffer = new byte[32768];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (data.Length + read > limit || clock.Elapsed.TotalSeconds > 45)
                            throw new IOException("O download ultrapassou o limite permitido.");
                        data.Write(buffer, 0, read);
                    }
                    return data.ToArray();
                }
            }
        }
        throw new IOException("Redirecionamentos demais ao baixar a atualização.");
    }

    internal static Package Stage(string executable)
    {
        var api = "https://api.github.com/repos/" + Repository + "/releases?per_page=20";
        var serializer = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
        var releases = (object[])serializer.DeserializeObject(Encoding.UTF8.GetString(Download(api, 1024 * 1024)));
        string selectedTag = null;
        Version selectedVersion = CurrentVersion;
        foreach (Dictionary<string, object> release in releases.OfType<Dictionary<string, object>>())
        {
            if (Convert.ToBoolean(release["draft"])) continue;
            string tag = Convert.ToString(release["tag_name"]);
            Version version = ReadTag(tag);
            if (version == null || version.CompareTo(selectedVersion) <= 0) continue;
            var names = ((object[])release["assets"]).OfType<Dictionary<string, object>>()
                .Select(asset => Convert.ToString(asset["name"])).ToArray();
            if (!names.Contains("XCIPTV-PC.exe") || !names.Contains("update.sha256")) continue;
            selectedTag = tag;
            selectedVersion = version;
        }
        if (selectedTag == null) return null;
        string releaseUrl = "https://github.com/" + Repository + "/releases/download/" + selectedTag + "/";
        string digest = Encoding.ASCII.GetString(Download(releaseUrl + "update.sha256", 128)).Trim();
        if (!Regex.IsMatch(digest, @"\A[0-9a-fA-F]{64}\z"))
            throw new IOException("O arquivo de verificação da versão está inválido.");
        var bytes = Download(releaseUrl + "XCIPTV-PC.exe", MaximumExeBytes);
        var stage = executable + "." + Guid.NewGuid().ToString("N") + ".download";
        try
        {
            File.WriteAllBytes(stage, bytes);
            Verify(stage, digest, selectedVersion);
            return new Package { Version = selectedVersion, Path = stage, Digest = digest };
        }
        catch
        {
            Delete(stage);
            throw;
        }
    }

    static void Verify(string path, string digest, Version expected)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
        {
            string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            if (!string.Equals(digest, actual, StringComparison.OrdinalIgnoreCase))
                throw new IOException("O executável baixado não passou na verificação SHA-256.");
        }
        Version version = AssemblyName.GetAssemblyName(path).Version;
        if (version != new Version(expected.Major, expected.Minor, expected.Build, 0))
            throw new IOException("A versão do executável não corresponde à publicação.");
    }

    static string Quote(string path) { return "\"" + path + "\""; }
    static void Delete(string path) { try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    internal static void LaunchInstaller(Package package, string target)
    {
        string helper = Path.Combine(Path.GetTempPath(), "XCIPTV-PC-update-" + Guid.NewGuid().ToString("N") + ".exe");
        File.Copy(target, helper);
        try
        {
            var process = Process.Start(new ProcessStartInfo(helper,
                "--apply-update " + Process.GetCurrentProcess().Id + " " + Quote(target)
                + " " + Quote(package.Path) + " " + package.Digest + " " + package.Version)
                { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(target) });
            process.Dispose();
        }
        catch
        {
            Delete(helper);
            throw;
        }
    }

    internal static void Apply(string[] args)
    {
        if (args.Length != 6) return;
        string target = args[2];
        string stage = args[3];
        string backup = target + "." + Guid.NewGuid().ToString("N") + ".backup";
        bool parentExited = false;
        bool replaced = false;
        try
        {
            try
            {
                using (var parent = Process.GetProcessById(int.Parse(args[1])))
                    if (!parent.WaitForExit(45000))
                        throw new IOException("O programa ainda está aberto.");
            }
            catch (ArgumentException) { }
            parentExited = true;
            var version = Version.Parse(args[5]);
            if (version.CompareTo(AssemblyName.GetAssemblyName(target).Version) <= 0)
                throw new IOException("A versão instalada já está atualizada.");
            Verify(stage, args[4], version);
            File.Replace(stage, target, backup);
            replaced = true;
            Restart(target, "--update-cleanup " + Quote(Application.ExecutablePath));
            Delete(backup);
        }
        catch (Exception error)
        {
            if (replaced)
            {
                try { File.Replace(backup, target, null); }
                catch (Exception) { }
            }
            MessageBox.Show("Não foi possível instalar a atualização. " + error.Message, "XCIPTV-PC",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (parentExited && File.Exists(target))
                try { Restart(target, "--update-cleanup " + Quote(Application.ExecutablePath)); }
                catch (Exception) { }
        }
        finally { Delete(stage); }
    }

    static void Restart(string target, string arguments)
    {
        using (var process = Process.Start(new ProcessStartInfo(target, arguments)
            { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(target) })) { }
    }

    internal static async Task Cleanup(string helper)
    {
        string path = Path.GetFullPath(helper);
        if (!string.Equals(Path.GetDirectoryName(path), Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
            || !Regex.IsMatch(Path.GetFileName(path), @"\AXCIPTV-PC-update-[a-f0-9]{32}\.exe\z")) return;
        for (int attempt = 0; attempt < 10 && File.Exists(path); attempt++)
        {
            await Task.Delay(500);
            Delete(path);
        }
    }
}

public partial class XCIPTVPC
{
    bool checkingUpdate;

    async Task CheckForProgramUpdate(bool manual)
    {
        if (checkingUpdate) return;
        checkingUpdate = true;
        AppUpdater.Package package = null;
        bool handedOff = false;
        try
        {
            package = await Task.Run(() => AppUpdater.Stage(Application.ExecutablePath));
            if (IsDisposed || Disposing) return;
            if (package == null)
            {
                if (manual) MessageBox.Show(this, "Você já está com a versão mais recente disponível.", "XCIPTV-PC");
                return;
            }
            Text = "XCIPTV-PC — versão " + package.Version + " pronta para instalar";
            while (busy || playbackScreen.Visible || !Enabled || WindowState == FormWindowState.Minimized)
            {
                await Task.Delay(1000);
                if (IsDisposed || Disposing) return;
            }
            AppUpdater.LaunchInstaller(package, Application.ExecutablePath);
            handedOff = true;
            Close();
        }
        catch (Exception)
        {
            if (manual && !IsDisposed && !Disposing)
                MessageBox.Show(this, "Não foi possível atualizar. Confira a conexão e se a pasta do programa permite gravação.", "XCIPTV-PC");
        }
        finally
        {
            if (!handedOff && package != null)
                try { File.Delete(package.Path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            checkingUpdate = false;
            if (!handedOff && !IsDisposed) Text = "XCIPTV-PC — Windows " + AppUpdater.CurrentVersion.ToString(3);
        }
    }
}
