using JetBrains.Annotations;

namespace AspNetCore.API.Python;

public sealed class WorldAnalysis : PythonRunner
{
    private readonly CancellationToken _token;

    public WorldAnalysis(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration) : base(configuration["PythonExePath"]!) =>
        _token = hostApplicationLifetime.ApplicationStopping;

    [UsedImplicitly]
    public async Task Run()
    {
        string result = await RunScript("Python/test.py", "optional_arguments", _token);
        Console.WriteLine($"Script output: {result}");
    }
}