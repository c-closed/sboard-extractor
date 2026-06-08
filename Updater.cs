using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Updater
{
    static int Main(string[] args)
    {
        if (args.Length < 3)
            return 1;

        int pid;
        if (!int.TryParse(args[0], out pid))
            return 1;

        string extractDir = args[1];
        string targetExe = args[2];
        string targetDir = Path.GetDirectoryName(targetExe);

        try
        {
            var proc = Process.GetProcessById(pid);
            proc.WaitForExit(30000);
        }
        catch { }

        Thread.Sleep(1000);

        try
        {
            foreach (string src in Directory.GetFiles(extractDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(src));
                File.Copy(src, dest, true);
            }
        }
        catch { return 2; }

        try
        {
            if (args.Length >= 4 && !string.IsNullOrEmpty(args[3]))
            {
                string desktopXlsx = args[3];
                if (File.Exists(desktopXlsx))
                {
                    string destXlsx = Path.Combine(targetDir, Path.GetFileName(desktopXlsx));
                    File.Copy(desktopXlsx, destXlsx, true);
                }
            }
        }
        catch { }

        try { Directory.Delete(extractDir, true); } catch { }

        try { Process.Start(targetExe); } catch { return 3; }

        return 0;
    }
}
