using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace WebJson
{
    class Program
    {
        static string sourceDirectory = null, outputDirectory = null;

        static void Main(string[] args)
        {
            Console.WriteLine("WebJson - Use templates to convert JSON data to webpages.");

            if (args.Length != 2)
            {
                Console.WriteLine("Usage: dotnet WebJson.dll <source directory> <output directory>");

                if (args.Length == 0)
                {
                    Environment.Exit(0);
                }
            }

            if (args.Length == 1 || args.Length > 2)
            {
                Console.WriteLine("Invalid argument list.");
                Environment.Exit(1);
            }

            sourceDirectory = args[0];

            if (!sourceDirectory.EndsWith('/') && !sourceDirectory.EndsWith('\\'))
            {
                sourceDirectory += '/';
            }

            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine("Invalid source directory.");
                Environment.Exit(2);
            }

            outputDirectory = args[1];

            if (!outputDirectory.EndsWith('/') && !outputDirectory.EndsWith('\\'))
            {
                outputDirectory += '/';
            }

            ProcessDirectory(sourceDirectory);
        }

        static void ProcessDirectory(string directory)
        {
            Console.WriteLine($"Processing directory: {Path.GetRelativePath(sourceDirectory, directory)}");

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var info = new FileInfo(file);

                if (info.Extension == ".json")
                {
                    ProcessJsonFile(file);
                }
                else
                {
                    CopyFile(file);
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                if (!new DirectoryInfo(dir).Name.StartsWith('_'))
                {
                    ProcessDirectory(dir);
                }
            }
        }

        static void ProcessJsonFile(string jsonFile)
        {
            var info = new FileInfo(jsonFile);

            var json = JObject.Parse(File.ReadAllText(jsonFile));

            var template = (string)json["template"];

            if (template == null)
            {
                Console.WriteLine($"Found JSON file: {info.Name} - No template was specified. Skipping.");
            }
            else
            {
                Console.WriteLine($"Found JSON file: {info.Name} - Uses template: {template}");

                var templatePath = Path.Combine(sourceDirectory, "_templates/", $"{template}.html");

                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"Template not found: {template}");
                }
                else
                {
                    var html = File.ReadAllText(templatePath);

                    var includes = new HashSet<string>();

                    foreach (Match match in Regex.Matches(html, @"\[@(\S*?)@\]", RegexOptions.None))
                    {
                        var include = match.Groups[1].Value;

                        if (!includes.Contains(include))
                        {
                            includes.Add(include);
                        }
                    }

                    foreach (var include in includes)
                    {
                        Console.WriteLine($"Found reference to include: {include}");

                        var includePath = Path.Combine(sourceDirectory, "_includes/", $"{include}.html");

                        if (!File.Exists(includePath))
                        {
                            Console.WriteLine($"Include not found: {include}");
                        }
                        else
                        {
                            var includeHtml = File.ReadAllText(includePath);

                            html = html.Replace($"[@{include}@]", includeHtml);
                        }
                    }

                    foreach (var property in json.Properties())
                    {
                        html = html.Replace($"[{{{property.Name}}}]", (string)property.Value);
                    }

                    var relativePath = Path.GetRelativePath(Path.GetDirectoryName(jsonFile), sourceDirectory).Replace('\\', '/');

                    if (!relativePath.EndsWith('/') && !relativePath.EndsWith('\\'))
                    {
                        relativePath += '/';
                    }

                    // Built-in properties
                    html = html.Replace("[{relative_path}]", relativePath);

                    var outputDir = Path.Combine(outputDirectory, Path.GetRelativePath(sourceDirectory, Path.GetDirectoryName(jsonFile)));
                    var outputFile = $"{info.Name.Remove(info.Name.LastIndexOf(info.Extension))}.html";
                    var outputPath = Path.Combine(outputDir, outputFile);

                    if (!Directory.Exists(outputDir))
                    {
                        try
                        {
                            Directory.CreateDirectory(outputDir);
                        }
                        catch
                        {
                            Console.WriteLine("Failed to create output directory.");
                            Environment.Exit(3);
                        }
                    }

                    try
                    {
                        File.WriteAllText(outputPath, html);
                        Console.WriteLine($"Outputted HTML file: {outputFile}");
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to output HTML file: {outputFile}");
                    }
                }
            }
        }

        static void CopyFile(string file)
        {
            var info = new FileInfo(file);

            var outputDir = Path.Combine(outputDirectory, Path.GetRelativePath(sourceDirectory, Path.GetDirectoryName(file)));
            var outputFile = info.Name;
            var outputPath = Path.Combine(outputDir, outputFile);

            if (!Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch
                {
                    Console.WriteLine("Failed to create output directory.");
                    Environment.Exit(3);
                }
            }

            try
            {
                File.Copy(file, outputPath, true);
                Console.WriteLine($"File copied: {info.Name}");
            }
            catch
            {
                Console.WriteLine($"Failed to copy file: {info.Name}");
            }
        }
    }
}
