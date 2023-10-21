namespace AspNetCore.API.Python;

public sealed class WeatherForecastAnalysis
{
    public async Task Run(CancellationToken token)
    {
        var runner = new PythonRunner();

        try
        {
            string result = await runner.RunScript("Python/test.py", "optional_arguments", token);
            Console.WriteLine($"Script output: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}