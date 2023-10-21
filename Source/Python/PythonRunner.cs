namespace AspNetCore.API.Python;

using System;
using System.Diagnostics;
using System.IO;

public sealed class PythonRunner
{
    public string PythonExecutablePath { get; set; }

    public PythonRunner(string pythonExecutablePath = "python") => PythonExecutablePath = pythonExecutablePath;

    public async Task<string> RunScript(string scriptPath, string arguments, CancellationToken token)
    {
        try
        {
            if (!File.Exists(scriptPath)) throw new FileNotFoundException($"Python script not found: {scriptPath}");

            var startInfo = new ProcessStartInfo {
                FileName = PythonExecutablePath,
                Arguments = $"\"{scriptPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync(token);
            string errorOutput = await process.StandardError.ReadToEndAsync(token);

            await process.WaitForExitAsync(token);

            if (process.ExitCode != 0) throw new Exception($"Python script exited with code {process.ExitCode}. Error output: {errorOutput}");

            return output;
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while running the Python script", ex);
        }
    }
}