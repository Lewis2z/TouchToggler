using TouchTogglerClassLibrary.Exceptions;
using System.Diagnostics;
using System;

namespace TouchTogglerClassLibrary.Helpers
{
    public static class ProcessHelper
    {
        public static Process StartAdminProcess(string executablePath, string arguments = "")
        {
            Process process = new Process();
            process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(executablePath);
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Verb = "runas";
            try {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                switch (ex.Message)
                {
                    case "The operation was canceled by the user":
                        throw new ProcessCancelledException();
                    case "The system cannot find the file specified":
                        throw new FIleNotFoundException(executablePath);
                }

                throw;
            }
            return process;
        }

        public static void StartProcessAs(string user, string executablePath, string arguments = "")
        {
            Process process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = arguments;
            //process.StartInfo.UserName = user;
            process.StartInfo.UseShellExecute = false;
            process.Start();
        }
    }
}
