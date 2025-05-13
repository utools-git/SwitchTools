using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

class Repack4Retail
{
    static void Main(string[] args)
    {
        Console.WriteLine("Yurr dis lil' program turns devkit nsp's into ones that you can install normally\nMade by maybekoi <3\nOriginal program by Slluxx on GBATemp\n");
        var sdkRoot = Environment.GetEnvironmentVariable("NINTENDO_SDK_ROOT");
        string authoringTool = Path.Combine(sdkRoot, "Tools", "CommandLineTools", "AuthoringTool", "AuthoringTool.exe");
        string hacPack = "hacpack";

        string prodKeys = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch", "prod.keys");
        if (!File.Exists(prodKeys))
        {
            prodKeys = "prod.keys";
            if (!File.Exists(prodKeys))
            {
                Console.WriteLine("Could not find prod.keys!");
                return;
            }
        }

        string nspFilePath = args.Length > 0 ? args[0] : Prompt("Enter the nsp filepath: ");
        string nspFileName = Path.GetFileName(nspFilePath);
        string nspParentPath = Path.GetDirectoryName(nspFilePath);

        Console.WriteLine($"Looking for TITLE_ID.txt in: {nspParentPath}");
        string nspTitleID = "";
        string titleIdPath = Path.Combine(nspParentPath, "TITLE_ID.txt");
        if (File.Exists(titleIdPath))
        {
            Console.WriteLine("Found TITLE_ID.txt!");
            nspTitleID = File.ReadAllText(titleIdPath).Trim();
            Console.WriteLine($"Read from file: {nspTitleID}");
            if (Regex.IsMatch(nspTitleID, "^[0-9a-fA-F]{16}$"))
            {
                Console.WriteLine($"Found title ID in TITLE_ID.txt! Title ID: {nspTitleID}");
            }
            else
            {
                Console.WriteLine($"TITLE_ID.txt exists but contains invalid title ID format: {nspTitleID}");
                nspTitleID = "";
            }
        }
        else
        {
            Console.WriteLine("TITLE_ID.txt not found in directory.");
        }

        if (string.IsNullOrEmpty(nspTitleID))
        {
            Console.WriteLine("Checking NSP path for title ID...");
            var match = Regex.Match(nspFilePath, "[0-9a-fA-F]{16}");
            if (!match.Success)
            {
                Console.WriteLine("Could not find title ID in path or TITLE_ID.txt! Manual Entry Required.");
                nspTitleID = Prompt("Enter the NSP's title ID: ");
            }
            else
            {
                nspTitleID = match.Value;
                Console.WriteLine($"Found title ID in path! Title ID: {nspTitleID}");
            }
        }

        string repackerExtract = Path.Combine(nspParentPath, "repacker_extract");
        string tmpPath = Path.Combine(Path.GetTempPath(), "NCA");

        TryDelete(repackerExtract);
        TryDelete(tmpPath);
        TryDelete("hacpack_backup");

        Directory.CreateDirectory(repackerExtract);
        RunProcess(authoringTool, $"extract -o \"{repackerExtract}\" \"{nspFilePath}\"");

        string controlPath = null, programPath = null;
        foreach (var dir in Directory.GetDirectories(repackerExtract))
        {
            if (File.Exists(Path.Combine(dir, "fs0", "control.nacp")))
                controlPath = dir;
            else if (File.Exists(Path.Combine(dir, "fs0", "main.npdm")))
                programPath = dir;

            if (controlPath != null && programPath != null)
                break;
        }

        if (controlPath == null || programPath == null)
        {
            Console.WriteLine("Missing required paths: " +
                (controlPath == null ? " control" : "") +
                (programPath == null ? " program" : ""));
            return;
        }

        string output = RunProcessWithOutput(hacPack, $"-k \"{prodKeys}\" -o \"{tmpPath}\" --titleid {nspTitleID} --type nca --ncatype program --exefsdir \"{Path.Combine(programPath, "fs0")}\" --romfsdir \"{Path.Combine(programPath, "fs1")}\" --logodir \"{Path.Combine(programPath, "fs2")}\"");
        Console.WriteLine(output);
        string tmpProgramNcaPath = ExtractCreatedPath(output, "Created Program NCA:");
        if (tmpProgramNcaPath == null) { Console.WriteLine("Failed getting the Program NCA!"); return; }

        output = RunProcessWithOutput(hacPack, $"-k \"{prodKeys}\" -o \"{tmpPath}\" --titleid {nspTitleID} --type nca --ncatype control --romfsdir \"{Path.Combine(controlPath, "fs0")}\"");
        Console.WriteLine(output);
        string tmpControlNcaPath = ExtractCreatedPath(output, "Created Control NCA:");
        if (tmpControlNcaPath == null) { Console.WriteLine("Failed getting the Control NCA!"); return; }

        RunProcess(hacPack, $"-k \"{prodKeys}\" -o \"{tmpPath}\" --titleid {nspTitleID} --type nca --ncatype meta --titletype application --programnca \"{tmpProgramNcaPath}\" --controlnca \"{tmpControlNcaPath}\"");

        string outputNsp = Path.Combine(nspParentPath, Path.GetFileNameWithoutExtension(nspFilePath) + "_repacked.nsp");
        if (File.Exists(outputNsp)) File.Delete(outputNsp);
        RunProcess(hacPack, $"-k \"{prodKeys}\" -o \"{nspParentPath}\" --titleid {nspTitleID} --type nsp --ncadir \"{tmpPath}\"");
        File.Move(Path.Combine(nspParentPath, $"{nspTitleID}.nsp"), outputNsp);

        TryDelete(repackerExtract);
        TryDelete(tmpPath);
        TryDelete("hacpack_backup");
    }

    static string Prompt(string message)
    {
        Console.Write(message);
        return Console.ReadLine();
    }

    static void TryDelete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }

    static void RunProcess(string fileName, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }

    static string RunProcessWithOutput(string fileName, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    static string ExtractCreatedPath(string output, string prefix)
    {
        foreach (string line in output.Split('\n'))
        {
            if (line.Contains(prefix))
                return line.Substring(line.IndexOf(prefix) + prefix.Length).Trim();
        }
        return null;
    }
}
