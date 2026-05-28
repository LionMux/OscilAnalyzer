using System.Diagnostics;
using System.IO;

namespace OscilAnalyzer
{
    /// <summary>
    /// Утилитарный класс для конвертации PyTorch-модели (.pth) в ONNX-формат.
    /// Запускает Python-скрипт export_onnx.py, который экспортирует CNN1D и сохраняет
    /// параметры нормализации (scalers.json) рядом с .onnx файлом.
    /// </summary>
    public static class OnnxModelExporter
    {
        private const string ExportScriptName = "export_onnx.py";

        /// <summary>
        /// Конвертирует .pth файл в ONNX + сохраняет scalers.json.
        /// </summary>
        /// <param name="pthPath">Путь к best_model.pth</param>
        /// <param name="outputDir">Директория, куда сохранить .onnx и .json</param>
        /// <returns>Результат конвертации</returns>
        public static ExportResult Export(string pthPath, string outputDir)
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptPath = Path.Combine(exeDir, ExportScriptName);

            if (!File.Exists(scriptPath))
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"Скрипт конвертации не найден: {scriptPath}"
                };
            }

            if (!File.Exists(pthPath))
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"Файл модели не найден: {pthPath}"
                };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" --input \"{pthPath}\" --output \"{outputDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = "Не удалось запустить процесс Python"
                };
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(120_000);

            if (process.ExitCode != 0 || !output.Contains("\"success\": true"))
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = string.IsNullOrEmpty(error) ? output : error
                };
            }

            return new ExportResult
            {
                Success = true,
                OnnxPath = Path.Combine(outputDir, "best_model.onnx"),
                ScalersPath = Path.Combine(outputDir, "scalers.json"),
            };
        }

        /// <summary>
        /// Проверяет, установлен ли Python и доступен ли в PATH.
        /// </summary>
        public static bool IsPythonAvailable()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(startInfo);
                process?.WaitForExit(5000);
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public string? OnnxPath { get; set; }
        public string? ScalersPath { get; set; }
        public string? ErrorMessage { get; set; }
    }
}