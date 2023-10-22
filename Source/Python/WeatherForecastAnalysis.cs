namespace AspNetCore.API.Python;

public sealed class WeatherForecastAnalysis
{
    private CancellationToken _token;
    
    public WeatherForecastAnalysis(IHostApplicationLifetime hostApplicationLifetime) => _token = hostApplicationLifetime.ApplicationStopping;

    public async Task Run()
    {
        var runner = new PythonRunner();

        try
        {
            string result = await runner.RunScript("Python/test.py", "optional_arguments", _token);
            Console.WriteLine($"Script output: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}