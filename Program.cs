using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CanvasQuizConverter.Generators;
using CanvasQuizConverter.Models;
using Json.Schema;

namespace CanvasQuizConverter.Cli
{
    public static class Program
    {
        [STAThread]
        public static async Task Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var openFileDialog = new OpenFileDialog
            {
                Title = "Select Quiz JSON Files",
                Filter = "JSON files (*.json)|*.json",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "QTI_Exports");
            Directory.CreateDirectory(exportDir);

            var summary = new List<string>();

            foreach (var fileName in openFileDialog.FileNames)
            {
                await ProcessFile(fileName, exportDir, summary);
            }

            Console.WriteLine("----- Summary -----");
            foreach (var message in summary)
            {
                Console.WriteLine(message);
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task ProcessFile(string filePath, string exportDir, ICollection<string> summary)
        {
            var fileName = Path.GetFileName(filePath);
            Console.WriteLine($"Processing '{fileName}'...");

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var schemaJson = await File.ReadAllTextAsync("quiz-schema.json");
                var schema = JsonSchema.FromText(schemaJson);
                var validationResult = schema.Evaluate(JsonDocument.Parse(jsonContent).RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Details.Where(d => d.HasErrors).SelectMany(d => d.Errors!.Select(e => $"'{d.InstanceLocation}': {e.Value}")).ToList();
                    LogError(fileName, "Schema validation failed.", summary, errorMessages);
                    return;
                }
                LogSuccess(fileName, "Validated against schema.", summary);

                var quiz = JsonSerializer.Deserialize<Quiz>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (quiz.MultipleChoiceQuestions.Any(q => q.Answers.Count(a => a.IsCorrect) != 1))
                {
                    LogError(fileName, "Each multiple-choice question must have exactly one correct answer.", summary);
                    return;
                }
                LogSuccess(fileName, "Logical validation passed.", summary);

                var zipPath = Path.Combine(exportDir, $"{quiz.QuizId}.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

                var manifestIdentifier = $"g_{Guid.NewGuid():N}";
                var assessmentIdentifier = $"g_{Guid.NewGuid():N}";
                var resourceIdentifiers = new Dictionary<string, string>();
                var dependencyIdentifiers = new List<string>();

                var allQuestions = quiz.MultipleChoiceQuestions.Select(q => (object)q).Concat(quiz.FreeResponseQuestions.Select(q => (object)q)).ToList();

                foreach (var question in allQuestions)
                {
                    string questionId, resourceId, qtiXml;
                    if (question is MultipleChoiceQuestion mcq)
                    {
                        questionId = mcq.Id;
                        resourceId = $"g_{Guid.NewGuid():N}";
                        qtiXml = XmlGenerator.GenerateQuestionItemQti(mcq);
                    }
                    else if (question is FreeResponseQuestion frq)
                    {
                        questionId = frq.Id;
                        resourceId = $"g_{Guid.NewGuid():N}";
                        qtiXml = XmlGenerator.GenerateFreeResponseQti(frq);
                    }
                    else continue;

                    resourceIdentifiers.Add(questionId, resourceId);
                    dependencyIdentifiers.Add(resourceId);
                    await AddEntryToZip(archive, $"{resourceId}/{questionId}.xml", qtiXml);
                }

                var assessmentXml = XmlGenerator.GenerateAssessmentQti(quiz.QuizTitle, assessmentIdentifier, resourceIdentifiers.Keys.ToList());
                await AddEntryToZip(archive, $"{assessmentIdentifier}/{assessmentIdentifier}.xml", assessmentXml);

                var manifestXml = XmlGenerator.GenerateImsManifest(manifestIdentifier, assessmentIdentifier, dependencyIdentifiers, resourceIdentifiers);
                await AddEntryToZip(archive, "imsmanifest.xml", manifestXml);

                LogSuccess(fileName, $"Generated '{Path.GetFileName(zipPath)}' with {allQuestions.Count} questions.", summary);
            }
            catch (Exception ex)
            {
                LogError(fileName, $"An unexpected error occurred: {ex.Message}", summary);
            }
            finally
            {
                Console.WriteLine("-------------");
            }
        }

        private static async Task AddEntryToZip(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            await using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            await writer.WriteAsync(content);
        }

        private static void LogSuccess(string fileName, string message, ICollection<string> summary)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"SUCCESS: {message}");
            Console.ResetColor();
            summary.Add($"SUCCESS ({fileName}): {message}");
        }

        private static void LogError(string fileName, string message, ICollection<string> summary, List<string> details = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR in '{fileName}': {message}");
            if (details != null)
            {
                foreach (var detail in details)
                {
                    Console.WriteLine($"	{detail}");
                }
            }
            Console.ResetColor();
            summary.Add($"ERROR ({fileName}): {message}");
        }
    }
}