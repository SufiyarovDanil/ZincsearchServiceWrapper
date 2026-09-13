using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ZincSearchServiceWrapper;

public class ZincSearchServiceWorker : BackgroundService
{
    private readonly ILogger<ZincSearchServiceWorker> _logger;
    private readonly ZincSearchSettings _settings;
    private Process? _zincSearchProcess;

    public ZincSearchServiceWorker(ILogger<ZincSearchServiceWorker> logger, IOptions<ZincSearchSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    private void ThrowIfZincSearchEnvironmentIsInvalid()
    {
        if (!File.Exists(_settings.ExecutablePath))
        {
            throw new Exception("Указанного пути к zincsearch.exe не существует");
        }

        var pathRootDir = Path.GetDirectoryName(_settings.ExecutablePath)!;
        var envPath = Path.Combine(pathRootDir, ".env");

        if (!File.Exists(envPath))
        {
            throw new Exception($"В пути {pathRootDir} нет файла с переменными окружения .env");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            ThrowIfZincSearchEnvironmentIsInvalid();

            var rootDirectory = Path.GetDirectoryName(_settings.ExecutablePath);
            var procStartInfo = new ProcessStartInfo
            {
                FileName = _settings.ExecutablePath,
                WorkingDirectory = rootDirectory,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            };

            _zincSearchProcess = new Process
            {
                StartInfo = procStartInfo,
                EnableRaisingEvents = true
            };

            _zincSearchProcess.Start();
            _logger.LogInformation($"ZincSearch запущен: PID {_zincSearchProcess.Id}.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Не удалось запустить ZincSearch: {ex.Message}");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Служба останавливается. Останавливаем дочерний процесс...");

        if (_zincSearchProcess is null || _zincSearchProcess.HasExited)
        {
            return;
        }

        _zincSearchProcess.Kill(entireProcessTree: true);
        await _zincSearchProcess.WaitForExitAsync();
        _zincSearchProcess.Dispose();
        _zincSearchProcess = null;

        await base.StopAsync(cancellationToken);
    }
}
