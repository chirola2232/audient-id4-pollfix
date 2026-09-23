using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;


[assembly: AssemblyTitle("Audient iD 4.4.2 PollFix")]
[assembly: AssemblyDescription("Unofficial community workaround for the Audient iD Software 4.4.2 USB polling/logging issue")]
[assembly: AssemblyProduct("Audient iD 4.4.2 PollFix")]
[assembly: AssemblyCompany("Community Tool")]
[assembly: AssemblyCopyright("Community project")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace AudientPollFix
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public sealed class MainForm : Form
    {
        const string Target = @"C:\Program Files\Audient\iD\iD.exe";
        const string OriginalSha = "932ac9b791a158975d6d26389a3b079037abec8c98ad233b73ff9267fe8fe794";
        const string PatchedSha  = "eaef2ef805e33bcee3d3c4c0bf4eac57c7c87c9541c28a4e71bef20ea015f381";
        const long PatchOffset = 0x1360A0;
        static readonly byte[] OriginalBytes = { 0x48, 0x8B, 0x49 };
        static readonly byte[] PatchedBytes  = { 0x31, 0xC0, 0xC3 };

        readonly string Backup = Target + ".original-4.4.2-backup";
        Label status = new Label();
        Label hash = new Label();
        Button apply = new Button();
        Button restore = new Button();

        public MainForm()
        {
            Text = "Audient iD 4.4.2 PollFix";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(650, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9F);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Label title = new Label();
            title.Text = "Audient iD 4.4.2 PollFix";
            title.Font = new Font("Segoe UI Semibold", 18F);
            title.AutoSize = true;
            title.Location = new Point(24, 20);
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "Unofficial community workaround · tested on iD4 MKII";
            sub.ForeColor = Color.DimGray;
            sub.AutoSize = true;
            sub.Location = new Point(27, 60);
            Controls.Add(sub);

            Label path = new Label();
            path.Text = Target;
            path.AutoSize = true;
            path.Location = new Point(27, 96);
            Controls.Add(path);

            Label s1 = new Label();
            s1.Text = "Status:";
            s1.Font = new Font("Segoe UI Semibold", 9F);
            s1.AutoSize = true;
            s1.Location = new Point(27, 132);
            Controls.Add(s1);

            status.AutoSize = true;
            status.Location = new Point(95, 132);
            Controls.Add(status);

            Label s2 = new Label();
            s2.Text = "SHA-256:";
            s2.Font = new Font("Segoe UI Semibold", 9F);
            s2.AutoSize = true;
            s2.Location = new Point(27, 160);
            Controls.Add(s2);

            hash.AutoEllipsis = true;
            hash.Location = new Point(95, 160);
            hash.Size = new Size(520, 20);
            Controls.Add(hash);

            apply.Text = "Apply Fix";
            apply.Size = new Size(150, 42);
            apply.Location = new Point(30, 205);
            apply.Click += delegate { ApplyFix(); };
            Controls.Add(apply);

            restore.Text = "Restore Original";
            restore.Size = new Size(150, 42);
            restore.Location = new Point(195, 205);
            restore.Click += delegate { RestoreOriginal(); };
            Controls.Add(restore);

            Button refresh = new Button();
            refresh.Text = "Refresh";
            refresh.Size = new Size(120, 42);
            refresh.Location = new Point(360, 205);
            refresh.Click += delegate { RefreshStatus(); };
            Controls.Add(refresh);

            Label note = new Label();
            note.Text = "Only patches the exact verified iD Software 4.4.2 executable.\r\nIt does not modify Audient drivers, firmware, Windows files, or install a background service.";
            note.ForeColor = Color.DimGray;
            note.AutoSize = true;
            note.Location = new Point(27, 272);
            Controls.Add(note);

            Shown += delegate { RefreshStatus(); };
        }

        void RefreshStatus()
        {
            try
            {
                if (!File.Exists(Target))
                {
                    status.Text = "iD.exe not found";
                    hash.Text = "-";
                    apply.Enabled = restore.Enabled = false;
                    return;
                }

                string h = Sha256(Target);
                hash.Text = h;

                if (h == OriginalSha)
                {
                    status.Text = "Compatible original 4.4.2";
                    apply.Enabled = true;
                    restore.Enabled = File.Exists(Backup);
                }
                else if (h == PatchedSha)
                {
                    status.Text = "FIX APPLIED";
                    apply.Enabled = false;
                    restore.Enabled = File.Exists(Backup);
                }
                else
                {
                    status.Text = "Unknown build - no changes will be made";
                    apply.Enabled = false;
                    restore.Enabled = File.Exists(Backup);
                }
            }
            catch (Exception ex)
            {
                status.Text = "Error: " + ex.Message;
                apply.Enabled = false;
            }
        }

        void ApplyFix()
        {
            try
            {
                if (!File.Exists(Target)) throw new FileNotFoundException("iD.exe was not found.", Target);
                if (Sha256(Target) != OriginalSha) throw new InvalidOperationException("This iD.exe does not match the supported 4.4.2 build.");

                if (!StopId()) throw new InvalidOperationException("Audient iD could not be closed.");

                byte[] current = ReadBytes(Target, PatchOffset, 3);
                if (!Equal(current, OriginalBytes)) throw new InvalidOperationException("Patch bytes do not match the expected build.");

                if (File.Exists(Backup))
                {
                    if (Sha256(Backup) != OriginalSha) throw new InvalidOperationException("An existing backup is present but its SHA-256 is not valid.");
                }
                else
                {
                    File.Copy(Target, Backup, false);
                    if (Sha256(Backup) != OriginalSha) throw new InvalidOperationException("Backup verification failed.");
                }

                WriteBytes(Target, PatchOffset, PatchedBytes);

                if (Sha256(Target) != PatchedSha)
                {
                    File.Copy(Backup, Target, true);
                    throw new InvalidOperationException("Final verification failed. The original file was restored.");
                }

                StartId();
                RefreshStatus();
                MessageBox.Show(this, "Fix applied and verified successfully.\r\n\r\nThe original iD.exe was backed up and Audient iD was restarted.", "PollFix", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                RefreshStatus();
                MessageBox.Show(this, ex.Message, "PollFix", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void RestoreOriginal()
        {
            try
            {
                if (!File.Exists(Backup)) throw new FileNotFoundException("Original backup was not found.", Backup);
                if (Sha256(Backup) != OriginalSha) throw new InvalidOperationException("The backup SHA-256 is not valid.");

                if (!StopId()) throw new InvalidOperationException("Audient iD could not be closed.");

                File.Copy(Backup, Target, true);
                if (Sha256(Target) != OriginalSha) throw new InvalidOperationException("Restore verification failed.");

                StartId();
                RefreshStatus();
                MessageBox.Show(this, "Original iD.exe restored and verified successfully.\r\n\r\nAudient iD was restarted.", "PollFix", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                RefreshStatus();
                MessageBox.Show(this, ex.Message, "PollFix", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        string FindLauncher()
        {
            string[] candidates = {
                @"C:\Program Files\Audient\USBAudioDriver\x64\AudientAppLauncher.exe",
                @"C:\Program Files\Audient\USBAudioDriver\AudientAppLauncher.exe",
                @"C:\Program Files\Audient\iD\AudientAppLauncher.exe"
            };
            foreach (string p in candidates) if (File.Exists(p)) return p;
            return null;
        }

        bool StopId()
        {
            Process[] ps = Process.GetProcessesByName("iD");
            if (ps.Length == 0) return true;

            string launcher = FindLauncher();
            if (launcher != null)
            {
                try
                {
                    ProcessStartInfo si = new ProcessStartInfo(launcher, "-exitall");
                    si.UseShellExecute = false;
                    si.CreateNoWindow = true;
                    Process p = Process.Start(si);
                    if (p != null) p.WaitForExit(5000);

                    for (int i = 0; i < 20; i++)
                    {
                        if (Process.GetProcessesByName("iD").Length == 0) return true;
                        Thread.Sleep(150);
                    }
                }
                catch { }
            }

            ps = Process.GetProcessesByName("iD");
            foreach (Process p in ps)
            {
                try { p.Kill(); p.WaitForExit(3000); }
                catch { }
                finally { p.Dispose(); }
            }

            Thread.Sleep(300);
            return Process.GetProcessesByName("iD").Length == 0;
        }

        void StartId()
        {
            string launcher = FindLauncher();
            if (launcher != null)
            {
                ProcessStartInfo si = new ProcessStartInfo(launcher, "-hide");
                si.UseShellExecute = false;
                si.CreateNoWindow = true;
                Process.Start(si);
            }
            else
            {
                Process.Start(Target);
            }
        }

        static string Sha256(string file)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] h = sha.ComputeHash(fs);
                return BitConverter.ToString(h).Replace("-", "").ToLowerInvariant();
            }
        }

        static byte[] ReadBytes(string file, long offset, int count)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Position = offset;
                byte[] b = new byte[count];
                if (fs.Read(b, 0, count) != count) throw new IOException("Could not read patch bytes.");
                return b;
            }
        }

        static void WriteBytes(string file, long offset, byte[] bytes)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                fs.Position = offset;
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }
        }

        static bool Equal(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
