using System.Diagnostics;
using System.Net.Sockets;

namespace Erp.Api.Infrastructure;

public sealed class DockerSqlServerOptions
{
    public bool Enabled { get; set; }
    public string ComposeFile { get; set; } = "docker-compose.yml";
    public string ProjectName { get; set; } = "erp-suite";
    public string ServiceName { get; set; } = "sqlserver";
    public string[] Services { get; set; } = [];
    public string ContainerName { get; set; } = "erp-suite-sqlserver";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1433;
    public int StartupTimeoutSeconds { get; set; } = 90;
}

public static class DockerSqlServerInitializer
{
    public static async Task EnsureStartedAsync(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var options = configuration
            .GetSection("Database:DockerSqlServer")
            .Get<DockerSqlServerOptions>() ?? new DockerSqlServerOptions();

        if (!options.Enabled)
        {
            return;
        }

        var composeFile = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.ComposeFile));
        if (!File.Exists(composeFile))
        {
            throw new FileNotFoundException($"Docker Compose file not found: {composeFile}", composeFile);
        }

        var services = (options.Services.Length > 0 ? options.Services : [options.ServiceName])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Console.WriteLine($"Starting Docker Compose services: {string.Join(", ", services)}.");
        await RunDockerAsync(["compose", "-p", options.ProjectName, "-f", composeFile, "up", "-d", .. services], cancellationToken);
        await WaitUntilReadyAsync(options, cancellationToken);
    }

    private static async Task WaitUntilReadyAsync(DockerSqlServerOptions options, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Max(options.StartupTimeoutSeconds, 1));
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var lastHealth = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var health = await TryGetContainerHealthAsync(options.ContainerName, cancellationToken);
            if (string.Equals(health, "healthy", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("SQL Server Docker container is healthy.");
                return;
            }

            if (string.IsNullOrWhiteSpace(health) && await CanOpenTcpConnectionAsync(options, cancellationToken))
            {
                Console.WriteLine("SQL Server Docker container is accepting TCP connections.");
                return;
            }

            if (!string.Equals(lastHealth, health, StringComparison.OrdinalIgnoreCase))
            {
                lastHealth = health;
                Console.WriteLine(string.IsNullOrWhiteSpace(health)
                    ? "Waiting for SQL Server Docker container."
                    : $"Waiting for SQL Server Docker container. Health: {health}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException($"SQL Server Docker container did not become ready within {timeout.TotalSeconds:N0} seconds.");
    }

    private static async Task<string?> TryGetContainerHealthAsync(string containerName, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RunProcessAsync(
                "docker",
                ["inspect", "-f", "{{.State.Health.Status}}", containerName],
                throwOnError: false,
                cancellationToken);

            return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> CanOpenTcpConnectionAsync(DockerSqlServerOptions options, CancellationToken cancellationToken)
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(options.Host, options.Port, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task RunDockerAsync(IReadOnlyCollection<string> arguments, CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync("docker", arguments, throwOnError: true, cancellationToken);
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            Console.WriteLine(result.StandardOutput.Trim());
        }
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        bool throwOnError,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        try
        {
            process.Start();
        }
        catch (Exception ex) when (throwOnError)
        {
            throw new InvalidOperationException("Could not start Docker. Check if Docker is installed and running.", ex);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var result = new ProcessResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);

        if (throwOnError && result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Docker command failed with exit code {result.ExitCode}: {result.StandardError.Trim()}");
        }

        return result;
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
