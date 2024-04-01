using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using static System.Net.WebRequestMethods;
using System.Linq;
using System.CommandLine.Invocation;
using File = System.IO.File;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
string[] languages = { "cs", "py", "c", "cpp", "java", "js", "ts", "html", "sln","txt" };
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
var createRspCommand = new Command("create-rsp", "Create a response file with a ready command");
var fileNameAndPathOutput = new Option<FileInfo>("--output", "File path and name");
var fileNameAndPathLanguage = new Option<string>("--language", "Programming language to include use all for all languages");
var filePathNote = new Option<bool>("--note", "Show File routing");
var fileSort = new Option<bool>("--sort", "Sort files by type ,Default is name");
var removeEmptyLines = new Option<bool>("--remove-empty-lines", "Remove empty lines from the source code");
var authorOption = new Option<string>("--author", "Name of the creator");
bundleCommand.AddOption(fileNameAndPathOutput);
bundleCommand.AddOption(fileNameAndPathLanguage);
bundleCommand.AddOption(filePathNote);
bundleCommand.AddOption(fileSort);
bundleCommand.AddOption(removeEmptyLines);
bundleCommand.AddOption(authorOption);
fileNameAndPathOutput.AddAlias("-o");
fileNameAndPathLanguage.AddAlias("-l");
filePathNote.AddAlias("-n");
fileSort.AddAlias("-s");
removeEmptyLines.AddAlias("-r");
authorOption.AddAlias("-a");
bundleCommand.SetHandler((output, language, note, sort, remove, author) =>
{
    if (output.Exists)
    {
        Console.WriteLine("Output file already exist. Please choose a differente name.");
        return;
    }
    string[] files;
    if (language.ToLower() == "all")
    {
        files = Directory.GetFiles(Directory.GetCurrentDirectory(), ".", SearchOption.AllDirectories);
        files = files.Where(file => languages.Any(suffix => file.EndsWith(suffix))).ToArray();
    }
    else
    {
        files = Directory.GetFiles(Directory.GetCurrentDirectory(), $"*.{language}", SearchOption.AllDirectories).ToArray();
    }
    if (files.Length == 0)
    {
        Console.WriteLine("No files to bundle");
        return;
    }
    if (!sort)
        files = files.OrderBy(f => Path.GetFileName(f)).ToArray();
    else files = files.OrderBy(f => Path.GetExtension(f)).ThenBy(f => Path.GetFileName(f)).ToArray();
    try
    {
        using (var outputFile = System.IO.File.CreateText(output.FullName))
        {
            if (!string.IsNullOrEmpty(author))
            {
                outputFile.WriteLine($"// Author: {author}");
            }
            foreach (var file in files)
            {
                string fileContent = System.IO.File.ReadAllText(file);
                if (remove)
                {
                    var nonEmptyLines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    fileContent = string.Join(Environment.NewLine, nonEmptyLines);
                    System.IO.File.WriteAllText(file, fileContent);
                }
                if (note)
                {
                    outputFile.WriteLine("------------------------------------");
                    outputFile.WriteLine($"// File: {file}");
                }
                outputFile.WriteLine(fileContent);
            }
        }
        Console.WriteLine($"Files bundled successfully: {output.FullName}");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("ERROR: File path is invalid");
    }
}, fileNameAndPathOutput, fileNameAndPathLanguage, filePathNote, fileSort, removeEmptyLines, authorOption);
string Prompt(string question)
{
    Console.Write(question);
    return Console.ReadLine();
}
createRspCommand.Handler = CommandHandler.Create((InvocationContext context) =>
{
    string output, language, note, remove, author, sort;
    output = Prompt("Enter the output file path and name: ");
    while (output.Length == 0)
    {
        Console.WriteLine("this field is required!!! enter again");
        output = Console.ReadLine();
    }
    language = Prompt("Enter the programming language (use 'all' for all languages): ");
    while (language.Length == 0)
    {
        Console.WriteLine("this field is required!!! enter again");
        language = Console.ReadLine();
    }
    note = Prompt("Show File routing? enter t: ");
    sort = Prompt("Sort files by type? enter t: ");
    remove = Prompt("Remove empty lines from the source code? enter t: ");
    author = Prompt("Enter the name of the creator: ");
    var noteOption = note.ToLower() == "t" ? "-n" : " ";
    var sortOption = sort.ToLower() == "t" ? "-s" : " ";
    var removeOption = remove.ToLower() == "t" ? "-r" : " ";
    var authorOption = author.Length == 0 ? " " : $" -a {author}";
    var rspContent = $"bundle -o {output}.txt -l {language} {noteOption} {sortOption} {removeOption} {authorOption}";
    var rspFileName = "rsp.rsp";
    File.WriteAllText(rspFileName, rspContent);
    Console.WriteLine($"Response file rsp created successfully.");
});
var rootCommand = new RootCommand("root command for file bundle CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);