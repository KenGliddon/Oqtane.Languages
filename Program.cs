using System;
using System.Text;
using System.Xml.Linq;
using Azure;
using Azure.AI.Translation.Text;

namespace Oqtane.Languages
{
    class Program
    {
        //// Define the 11 official South African languages with their culture codes
        //private static readonly Dictionary<string, string> SaLanguages = new Dictionary<string, string>
        //{
        //    { "Afrikaans", "af-ZA" },
        //    { "English", "en-ZA" }, // Already provided in Oqtane, but included for completeness
        //    { "isiNdebele", "nr-ZA" },
        //    { "isiXhosa", "xh-ZA" },
        //    { "isiZulu", "zu-ZA" },
        //    { "Sepedi", "nso-ZA" },
        //    { "Sesotho", "st-ZA" },
        //    { "Setswana", "tn-ZA" },
        //    { "SiSwati", "ss-ZA" },
        //    { "Tshivenda", "ve-ZA" },
        //    { "Xitsonga", "ts-ZA" }
        //};

        // Short List for testing
        private static readonly Dictionary<string, string> _languages = new Dictionary<string, string>
        {
            { "English", "en-ZA" },
            { "Afrikaans", "af-ZA" }
        };

        // Batch size to avoid exceeding the Azure Translator limits (max 1000 texts per request)
        private const int BatchSize = 1000;

        static void Main(string[] args)
        {
            // Configuration: Update these with your Azure Translator credentials
            string translatorKey = "YOUR_AZURE_TRANSLATOR_KEY";
            string translatorEndpoint = "YOUR_AZURE_TRANSLATOR_ENDPOINT";
            string translatorRegion = "YOUR_AZURE_REGION"; // e.g., "global" or "westus"

            // Paths
            string sourceResourcesPath = @"path\to\oqtane.framework\Oqtane.Client\Resources"; // Update with actual path
            string outputBasePath = @"path\to\output\Resources"; // Update with desired output path
            string oqtaneVersion = "6.1.3"; // Update with the target Oqtane version

            // Initialize Azure Translator client
            var credential = new AzureKeyCredential(translatorKey);
            var client = new TextTranslationClient(credential, new Uri(translatorEndpoint), translatorRegion);

            // Step 1: Collect distinct values from all .resx files
            var distinctValues = CollectDistinctValues(sourceResourcesPath);

            // Step 2: Translate distinct values for each language (skip en-ZA as it's the source)
            var translations = new Dictionary<string, Dictionary<string, string>>();
            foreach (var lang in _languages.Where(l => l.Key != "English"))
            {
                string cultureCode = lang.Value;
                string targetLang = cultureCode.Split('-')[0];
                translations[cultureCode] = TranslateDistinctValues(client, distinctValues, "en", targetLang);
                Console.WriteLine($"Translated distinct values for {cultureCode}");
            }

            // Step 3: Copy, rename, and update .resx files recursively
            foreach (var lang in _languages.Where(l => l.Key != "English"))
            {
                string cultureCode = lang.Value;
                string outputFolder = Path.Combine(outputBasePath, $"Oqtane.Translations.{cultureCode}");
                string outputName = $"Oqtane.Translations.{cultureCode}";
                string resourceFolder = Path.Combine(outputFolder, $"Oqtane.Client/Resources");
                CopyAndUpdateResxFilesRecursively(sourceResourcesPath, resourceFolder, cultureCode, translations[cultureCode]);

                // Step 4: Create .csproj and .sln files for Visual Studio validation
                CreateProjectAndSolutionFiles(outputFolder, outputName, cultureCode, oqtaneVersion);
            }

            Console.WriteLine("Translation and file creation completed.");
        }

        static List<string> CollectDistinctValues(string sourceFolder)
        {
            var distinctValues = new HashSet<string>();

            // Recursively get all .resx files
            var resxFiles = Directory.GetFiles(sourceFolder, "*.resx", SearchOption.AllDirectories);

            foreach (var sourceFile in resxFiles)
            {
                var content = File.ReadAllText(sourceFile);
                var doc = XDocument.Parse(content);
                var values = doc.Descendants("data")
                    .Select(d => d.Element("value")?.Value)
                    .Where(v => !string.IsNullOrEmpty(v));

                foreach (var value in values)
                {
                    distinctValues.Add(value);
                }
            }

            return distinctValues.ToList();
        }

        static Dictionary<string, string> TranslateDistinctValues(TextTranslationClient client, List<string> values, string sourceLang, string targetLang)
        {
            var translationMap = new Dictionary<string, string>();
            for (int i = 0; i < values.Count; i += BatchSize)
            {
                var batch = values.Skip(i).Take(BatchSize).ToList();
                try
                {
                    var response = client.Translate(targetLang, batch, sourceLang);
                    var translations = response.Value.ToList();

                    for (int j = 0; j < batch.Count; j++)
                    {
                        string translatedText = translations[j].Translations.FirstOrDefault()?.Text ?? batch[j];
                        translationMap[batch[j]] = translatedText;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error translating batch to {targetLang}: {ex.Message}");
                    // Fallback: use original values if translation fails
                    foreach (var value in batch)
                    {
                        translationMap[value] = value;
                    }
                }
            }
            return translationMap;
        }

        static void CopyAndUpdateResxFilesRecursively(string sourceFolder, string outputFolder, string cultureCode, Dictionary<string, string> translationMap)
        {
            // Create the output folder if it doesn't already exist
            Directory.CreateDirectory(outputFolder);
            // Get all .resx files in the current source folder
            var resxFiles = Directory.GetFiles(sourceFolder, "*.resx");
            foreach (var sourceFile in resxFiles)
            {
                // Read the source file content
                string content = File.ReadAllText(sourceFile);
                // Replace values with translated ones
                foreach (var pair in translationMap)
                {
                    // Escape special characters in the original and translated values for safe replacement
                    string originalValue = EscapeForXml(pair.Key);
                    string translatedValue = EscapeForXml(pair.Value);
                    content = content.Replace($"<value>{originalValue}</value>", $"<value>{translatedValue}</value>");
                }
                // Create new .resx file with translated values
                string fileName = Path.GetFileNameWithoutExtension(sourceFile);
                string newFilePath = Path.Combine(outputFolder, $"{fileName}.{cultureCode}.resx");
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                File.WriteAllText(newFilePath, content, Encoding.UTF8);
                Console.WriteLine($"Created {newFilePath}");
            }
            // Recursively process subfolders
            var subfolders = Directory.GetDirectories(sourceFolder);
            foreach (var subfolder in subfolders)
            {
                string subfolderName = Path.GetFileName(subfolder);
                string newOutputSubfolder = Path.Combine(outputFolder, subfolderName);
                CopyAndUpdateResxFilesRecursively(subfolder, newOutputSubfolder, cultureCode, translationMap);
            }
        }

        static string EscapeForXml(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Escape XML special characters
            return value.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&apos;");
        }

        static void CreateProjectAndSolutionFiles(string outputFolder, string outputName, string cultureCode, string oqtaneVersion)
        {
            // Create .csproj file
            // Updated to automatically build the nuget package on project build.
            string csprojPath = Path.Combine(outputFolder, $"{outputName}.csproj");
            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <AssemblyName>Oqtane.Translation.{cultureCode}</AssemblyName>
    <RootNamespace>Oqtane.Client</RootNamespace>
    <Culture>{cultureCode}</Culture>
    <OutputType>Library</OutputType>
    <NoCode>true</NoCode>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>Oqtane.Translation.{cultureCode.Replace("-","")}</PackageId>
    <Version>{oqtaneVersion}</Version>
    <Authors>Spot</Authors>
    <Description>Language resources for Oqtane Framework ({cultureCode})</Description>
    <Copyright>Spot</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/oqtane/oqtane.framework</PackageProjectUrl>
    <PackageTags>oqtane language pack</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include=""*.{cultureCode}.resx"" LogicalName=""Oqtane.Client.%(Filename).resources"" />
    <EmbeddedResource Include=""**/*. {cultureCode}.resx"" LogicalName=""Oqtane.Client.%(RecursiveDir)%(Filename).resources"" />
    <None Include=""README.md"" Pack=""true"" PackagePath="""" />
  </ItemGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);
            Console.WriteLine($"Created {csprojPath}");

            // Create .sln file
            string guid = Guid.NewGuid().ToString().ToUpper();
            string slnPath = Path.Combine(outputFolder, $"{outputName}.sln");
            string slnContent = $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{outputName}"", ""{outputName}.csproj"", ""{{{guid}}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{{guid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{{guid}}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal";
            File.WriteAllText(slnPath, slnContent);
            Console.WriteLine($"Created {slnPath}");

            // Create README.md file required for the nuget package generation
            string mdPath = Path.Combine(outputFolder, $"README.md");
            string mdContent = $@"# Oqtane Language Pack {cultureCode}

This package provides ({cultureCode}) language resources for Oqtane Framework {oqtaneVersion}.

## Installation
- Copy the .nupkg to Oqtane.Server/wwwroot/Packages and restart Oqtane.
- Add '{cultureCode}' in the Language Manager.

Generated with assistance from Grok, built by xAI.";
            File.WriteAllText(mdPath, mdContent);
            Console.WriteLine($"Created {mdPath}");
        }
    }
}