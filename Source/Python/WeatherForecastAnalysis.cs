namespace AspNetCore.API.Python;

public static class WeatherForecastAnalysis
{
    public static async Task Run(WebApplication app)
    {
        var runner = new PythonRunner();

        try
        {
            string result = await runner.RunScript("Python/test.py", "optional_arguments", app.Lifetime.ApplicationStopping);
            Console.WriteLine($"Script output: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}