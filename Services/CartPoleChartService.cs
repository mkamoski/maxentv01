using ScottPlot;
using System.Diagnostics;
using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MyMaxEntV01.Services
{
    /// <summary>
    /// Service for generating CartPole training charts and managing output files
    /// </summary>
    public class CartPoleChartService
    {
        private const string OutputFolder = "output";
        private const int CleanupDaysThreshold = 28;

        public CartPoleChartService()
        {
            EnsureOutputFolderExists();
        }

        /// <summary>
        /// Generate a training chart with episode rewards, steps, and entropy
        /// </summary>
        /// <param name="episodeRewards">List of rewards per episode</param>
        /// <param name="episodeSteps">List of steps per episode</param>
        /// <param name="episodeEntropies">List of entropy values per episode</param>
        /// <returns>Full path to the saved PNG file</returns>
        public string GenerateTrainingChart(
            List<double> episodeRewards, 
            List<int> episodeSteps, 
            List<double> episodeEntropies,
            out string timestamp)
        {
            return GenerateTrainingChart(episodeRewards, episodeSteps, episodeEntropies, out timestamp, "cartpole");
        }

        /// <summary>
        /// Generate a training chart with episode rewards, steps, and entropy
        /// </summary>
        /// <param name="episodeRewards">List of rewards per episode</param>
        /// <param name="episodeSteps">List of steps per episode</param>
        /// <param name="episodeEntropies">List of entropy values per episode</param>
        /// <param name="experimentName">Name of the experiment for the filename suffix</param>
        /// <returns>Full path to the saved PNG file</returns>
        public string GenerateTrainingChart(
            List<double> episodeRewards, 
            List<int> episodeSteps, 
            List<double> episodeEntropies,
            out string timestamp,
            string experimentName)
        {
            // Create timestamp-based filename
            timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffff");
            string filename = $"{timestamp}-maxent-{experimentName}.png";
            string fullPath = Path.Combine(OutputFolder, filename);

            // Prepare episode numbers for x-axis
            double[] episodes = Enumerable.Range(1, episodeRewards.Count)
                .Select(x => (double)x)
                .ToArray();

            // Create three separate plots
            int width = 1200;
            int height = 300;

            // Plot 1: Episode Rewards
            var plt1 = new Plot();
            plt1.Title("Episode Rewards");
            plt1.XLabel("Episode");
            plt1.YLabel("Total Reward");

            var rewardScatter = plt1.Add.Scatter(episodes, episodeRewards.ToArray());
            rewardScatter.LineWidth = 2;
            rewardScatter.Color = ScottPlot.Color.FromHex("#2E7D32"); // Green
            rewardScatter.MarkerSize = 5;

            // Add moving average for rewards
            if (episodeRewards.Count >= 5)
            {
                var rewardMA = CalculateMovingAverage(episodeRewards, 5);
                var maScatter = plt1.Add.Scatter(episodes, rewardMA);
                maScatter.LineWidth = 3;
                maScatter.Color = ScottPlot.Color.FromHex("#1B5E20"); // Dark green
                maScatter.MarkerSize = 0;
                maScatter.LegendText = "5-Episode Moving Average";
            }

            plt1.ShowLegend();

            // Plot 2: Episode Steps (Duration)
            var plt2 = new Plot();
            plt2.Title("Episode Duration (Steps)");
            plt2.XLabel("Episode");
            plt2.YLabel("Steps");

            var stepsArray = episodeSteps.Select(s => (double)s).ToArray();
            var stepsScatter = plt2.Add.Scatter(episodes, stepsArray);
            stepsScatter.LineWidth = 2;
            stepsScatter.Color = ScottPlot.Color.FromHex("#1976D2"); // Blue
            stepsScatter.MarkerSize = 5;

            // Add moving average for steps
            if (episodeSteps.Count >= 5)
            {
                var stepsMA = CalculateMovingAverage(episodeSteps.Select(s => (double)s).ToList(), 5);
                var maScatter = plt2.Add.Scatter(episodes, stepsMA);
                maScatter.LineWidth = 3;
                maScatter.Color = ScottPlot.Color.FromHex("#0D47A1"); // Dark blue
                maScatter.MarkerSize = 0;
                maScatter.LegendText = "5-Episode Moving Average";
            }

            plt2.ShowLegend();

            // Plot 3: Policy Entropy
            var plt3 = new Plot();
            plt3.Title("Policy Entropy (Exploration)");
            plt3.XLabel("Episode");
            plt3.YLabel("Entropy");

            var entropyScatter = plt3.Add.Scatter(episodes, episodeEntropies.ToArray());
            entropyScatter.LineWidth = 2;
            entropyScatter.Color = ScottPlot.Color.FromHex("#F57C00"); // Orange
            entropyScatter.MarkerSize = 5;

            // Add moving average for entropy
            if (episodeEntropies.Count >= 5)
            {
                var entropyMA = CalculateMovingAverage(episodeEntropies, 5);
                var maScatter = plt3.Add.Scatter(episodes, entropyMA);
                maScatter.LineWidth = 3;
                maScatter.Color = ScottPlot.Color.FromHex("#E65100"); // Dark orange
                maScatter.MarkerSize = 0;
                maScatter.LegendText = "5-Episode Moving Average";
            }

            plt3.ShowLegend();

            // Save individual plots and combine them into one image
            string tempPath1 = Path.Combine(OutputFolder, "temp1.png");
            string tempPath2 = Path.Combine(OutputFolder, "temp2.png");
            string tempPath3 = Path.Combine(OutputFolder, "temp3.png");

            plt1.SavePng(tempPath1, width, height);
            plt2.SavePng(tempPath2, width, height);
            plt3.SavePng(tempPath3, width, height);

            // Combine the three plots vertically
            CombineImagesVertically(new[] { tempPath1, tempPath2, tempPath3 }, fullPath);

            // Clean up temp files
            try
            {
                File.Delete(tempPath1);
                File.Delete(tempPath2);
                File.Delete(tempPath3);
            }
            catch { /* Ignore cleanup errors */ }

            return fullPath;
        }

        /// <summary>
        /// Combine multiple images vertically into one
        /// </summary>
        private void CombineImagesVertically(string[] imagePaths, string outputPath)
        {
            using var img1 = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePaths[0]);
            using var img2 = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePaths[1]);
            using var img3 = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePaths[2]);

            int width = img1.Width;
            int totalHeight = img1.Height + img2.Height + img3.Height;

            using var combined = new SixLabors.ImageSharp.Image<Rgba32>(width, totalHeight);

            combined.Mutate(ctx =>
            {
                ctx.DrawImage(img1, new SixLabors.ImageSharp.Point(0, 0), 1f);
                ctx.DrawImage(img2, new SixLabors.ImageSharp.Point(0, img1.Height), 1f);
                ctx.DrawImage(img3, new SixLabors.ImageSharp.Point(0, img1.Height + img2.Height), 1f);
            });

            combined.SaveAsPng(outputPath);
        }

        /// <summary>
        /// Calculate simple moving average
        /// </summary>
        private double[] CalculateMovingAverage(List<double> data, int windowSize)
        {
            var result = new double[data.Count];

            for (int i = 0; i < data.Count; i++)
            {
                int start = Math.Max(0, i - windowSize + 1);
                int count = i - start + 1;
                double sum = 0;

                for (int j = start; j <= i; j++)
                {
                    sum += data[j];
                }

                result[i] = sum / count;
            }

            return result;
        }

        /// <summary>
        /// Open the chart image in Windows default image viewer
        /// </summary>
        [SupportedOSPlatform("windows")]
        public void OpenChartInViewer(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Chart file not found: {filePath}");
            }

            try
            {
                // Use Windows shell to open with default image viewer
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open chart in viewer: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clean up old chart files (older than 28 days)
        /// </summary>
        public int CleanupOldCharts()
        {
            EnsureOutputFolderExists();

            var outputDir = new DirectoryInfo(OutputFolder);
            var cutoffDate = DateTime.Now.AddDays(-CleanupDaysThreshold);
            int deletedCount = 0;

            var oldFiles = outputDir.GetFiles("*-cartpole.png")
                .Where(f => f.LastWriteTime < cutoffDate);

            foreach (var file in oldFiles)
            {
                try
                {
                    file.Delete();
                    deletedCount++;
                }
                catch
                {
                    // Ignore deletion errors (file in use, permissions, etc.)
                }
            }

            return deletedCount;
        }

        /// <summary>
        /// Ensure output folder exists
        /// </summary>
        private void EnsureOutputFolderExists()
        {
            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }
        }

        /// <summary>
        /// Save log content to a file with the same timestamp format as charts
        /// </summary>
        /// <param name="logContent">The log content to save</param>
        /// <param name="timestamp">Optional timestamp to use (if null, generates new timestamp)</param>
        /// <returns>Full path to the saved log file</returns>
        public string SaveLog(string logContent, string? timestamp = null)
        {
            EnsureOutputFolderExists();

            // Use provided timestamp or create new one
            if (string.IsNullOrEmpty(timestamp))
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffff");
            }

            string filename = $"{timestamp}-cartpole.log";
            string fullPath = Path.Combine(OutputFolder, filename);

            File.WriteAllText(fullPath, logContent);

            return fullPath;
        }

        /// <summary>
        /// Open the log file in default text editor
        /// </summary>
        public void OpenLogInViewer(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Log file not found: {filePath}");
            }

            try
            {
                // Use Windows shell to open with default text editor
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open log in viewer: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get list of all chart files sorted by date descending
        /// </summary>
        public List<FileInfo> GetAllCharts()
        {
            EnsureOutputFolderExists();

            var outputDir = new DirectoryInfo(OutputFolder);
            return outputDir.GetFiles("*-cartpole.png")
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();
        }
    }
}
