# Oqtane Language Translator

This console application automates the translation of Oqtane resource files (.resx) into the 11 official South African languages using Azure Cognitive Services Translator. It generates translated .resx files, creates Visual Studio project (.csproj) and solution (.sln) files for validation (e.g., with ResxExplorer), and sets up the project to generate NuGet packages on build for easy integration with Oqtane's Language Manager.

Developed with assistance from Grok, built by Spot & xAI.

## Features
- Translates Oqtane .resx files into 10 South African languages (excluding English/en-ZA): Afrikaans (af-ZA), isiNdebele (nr-ZA), isiXhosa (xh-ZA), isiZulu (zu-ZA), Sepedi (nso-ZA), Sesotho (st-ZA), Setswana (tn-ZA), SiSwati (ss-ZA), Tshivenda (ve-ZA), Xitsonga (ts-ZA).
- Collects distinct resource values for consistent batch translation.
- Handles Azure Translator limits by batching requests (max 1000 texts per call).
- Recursively processes subfolders (e.g., Installer, Modules, Themes, UI) while preserving structure.
- Generates Visual Studio-compatible .csproj and .sln files for each language, enabling manual validation and editing.
- Sets up projects to automatically generate NuGet packages (.nupkg) on build, following Oqtane naming conventions (e.g., Oqtane.Translations.af-ZA).
- Escapes XML special characters in translated values for valid .resx files.
- Fallback to original values if translation fails (useful for less-supported languages).

## Prerequisites
- .NET SDK 9.0 or later (Oqtane 6.1.3 targets .NET 9.0).
- Azure Translator resource: Create one in the Azure portal and obtain the Key, Endpoint (e.g., https://api.cognitive.microsofttranslator.com/), and Region.
- Visual Studio 2022 or later for project validation and building NuGet packages.
- NuGet CLI (optional for manual packing): Install with `dotnet tool install -g NuGet.CommandLine --add-source https://api.nuget.org/v3/index.json`.
- Azure.AI.Translation.Text NuGet package: Installed automatically via the project, but ensure it's referenced.

## Setup
1. **Clone or Download the Repository**:
   ```
   git clone https://your-github-repo/Oqtane-Language-Translator.git
   cd Oqtane-Language-Translator
   ```

2. **Install Dependencies**:
   ```
   dotnet restore
   ```

3. **Configure the Application**:
   - Open `Program.cs` and update:
     - Azure Translator credentials (`translatorKey`, `translatorEndpoint`, `translatorRegion`).
     - `sourceResourcesPath`: Path to your Oqtane.Client/Resources folder (e.g., D:\Temp\Oqtane\Modules\Languages\ResourceBase\Resources).
     - `outputBasePath`: Path for output folders (e.g., D:\Temp\Oqtane\Modules\Languages\ResourceOutput).
     - `_languages`: Customize the languages dictionary if needed (defaults to all 11, but skips en-ZA). You can add any of the translation supported Language Codes.
     - `oqtaneVersion`: Set to your Oqtane version (e.g., "6.1.3").

## Usage
1. **Run the Application**:
   ```
   dotnet run
   ```
   - This:
     - Translates resources.
     - Generates output folders (e.g., Oqtane.Translations.af-ZA) with translated .resx files in Oqtane.Client/Resources subfolder.
     - Automatically Creates .csproj, .sln, README.md, and sets up for NuGet package generation on build.

2. **Validate and Edit Translations**:
   - Open the .sln file in Visual Studio (e.g., Oqtane.Translations.af-ZA/LanguagePack.sln).
   - Use ResxExplorer or similar tools to validate/edit .resx files (e.g., for undocumented languages like ts-ZA).
   - If changes are made, rebuild the project.

3. **Build and Generate NuGet Package**:
   - In Visual Studio: Build > Build Solution (or Release configuration).
   - Or via command line (in the output folder):
     ```
     dotnet build LanguagePack.csproj -c Release
     ```
   - The .nupkg is generated in bin/Release (e.g., Oqtane.Translations.afZA.6.1.3.nupkg), thanks to `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`.

## Output
- **Translated Folders**: e.g., ResourceOutput/Oqtane.Translations.af-ZA/Oqtane.Client/Resources with .resx files like filename.af-ZA.resx.
- **Project Files**: LanguagePack.csproj, LanguagePack.sln, README.md in each folder.
- **NuGet Package**: Generated on build in bin/Release/Oqtane.Translations.{cultureCodeNoHyphen}.{oqtaneVersion}.nupkg.

## Integration with Oqtane
1. **Deploy the NuGet Package**:
   - Copy the .nupkg to your Oqtane.Server/wwwroot/Packages folder and restart the application.
   - Or upload via Oqtane Admin Dashboard > Module Management.

2. **Add Languages**:
   - In Oqtane Admin Dashboard > Language Management, add the culture code (e.g., af-ZA).
   - Update Oqtane.Server/appsettings.json or web.config to register the culture if needed.

3. **Test**:
   - Switch languages in Oqtane and verify translations.

## Notes
- **Translation Quality**: Azure Translator may have limited support for languages like nso-ZA or ts-ZA. Always validate with native speakers or tools like ResxExplorer.
- **XML Escaping**: Special characters (&, <, >, ") are escaped in translations for valid .resx files.
- **Batch Translation**: Handles Azure limits (1000 texts per request); fallback to original if fails.
- **Customization**: Edit `_languages` dictionary to process specific languages.
- **Troubleshooting**: If build fails, ensure .NET 9.0 SDK is installed and paths are correct. Run as Administrator if needed.

## License
MIT License. See LICENSE file for details.
