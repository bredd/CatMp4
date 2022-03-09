using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatMp4
{
    class Program
    {
        const string c_syntax =
@"Syntax
  CatMp4 <inputFilename [-ss <startTime>] [-t <duration>] [-to <endTime>]> ... -out <fileName>

Concatenates multiple .mp4 files or snippets thereof into one video using ffmpeg.
";

        static bool s_showHelp;
        static List<InputFile> s_input = new List<InputFile>();
        static string s_output;

        static void Main(string[] args)
        {
            try
            {
                ParseCommandLine(args);
                if (s_showHelp)
                {
                    Console.WriteLine(c_syntax);
                }
                else
                {
                    ConcatVideos();
                }
            }
            catch (Exception err)
            {
#if DEBUG
                Console.WriteLine(err.ToString());
#else
                Console.WriteLine(err.Message);
#endif
            }

            CodeBit.ConsoleHelper.PromptAndWaitIfSoleConsole();
        }

        static void ParseCommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                s_showHelp = true;
                return;
            }

            var lexer = new CodeBit.CommandLineLexer(args);
            InputFile currentFile = null;
            while (lexer.MoveNext())
            {
                if (!lexer.IsOption)
                {
                    string path = Path.GetFullPath(lexer.Current);
                    if (!File.Exists(path)) lexer.ThrowValueError("File not found.");
                    currentFile = new InputFile() { FileName = path };
                    s_input.Add(currentFile);
                }
                else
                {
                    switch (lexer.Current.ToLowerInvariant())
                    {
                        case "-h":
                        case "-?":
                            s_showHelp = true;
                            break;

                        case "-out":
                            s_output = lexer.ReadNextValue();
                            break;

                        case "-ss":
                            AssertHasCurrentFile(currentFile, lexer);
                            currentFile.StartTime = lexer.ReadNextValue();
                            break;

                        case "-t":
                            AssertHasCurrentFile(currentFile, lexer);
                            currentFile.Duration = lexer.ReadNextValue();
                            break;

                        case "-to":
                            AssertHasCurrentFile(currentFile, lexer);
                            currentFile.EndTime = lexer.ReadNextValue();
                            break;

                        default:
                            lexer.ThrowUnexpectedArgError();
                            break;
                    }
                }
            }

            if (s_input.Count == 0)
            {
                lexer.ThrowError("No input files specified.");
            }
            if (string.IsNullOrEmpty(s_output))
            {
                lexer.ThrowError("No output filename specified.");
            }

            s_output = Path.GetFullPath(s_output);
            if (!Directory.Exists(Path.GetDirectoryName(s_output)))
            {
                lexer.ThrowError($"Invalid output filename '{s_output}'. Directory does not exist.");
            }

            if (File.Exists(s_output))
            {
                lexer.ThrowError($"Output filename '{s_output}' already exists.");
            }
        }

        static void AssertHasCurrentFile(InputFile currentFile, CodeBit.CommandLineLexer lexer)
        {
            if (currentFile == null)
            {
                lexer.ThrowValueError($"{lexer.Current} must come after an input filename.");
            }
        }

        static void ConcatVideos()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            var tempFiles = new List<CodeBit.IO.TempFile>();
            try
            {
                // Snip and recode each file into a bare stream
                foreach (var input in s_input)
                {
                    var tempFile = new CodeBit.IO.TempFile();
                    tempFiles.Add(tempFile);

                    var args = "-hide_banner";
                    if (!string.IsNullOrEmpty(input.StartTime)) args += $" -ss {input.StartTime}";
                    if (!string.IsNullOrEmpty(input.Duration)) args += $" -t {input.Duration}";
                    if (!string.IsNullOrEmpty(input.EndTime)) args += $" -to {input.EndTime}";
                    args += $" -i \"{input.FileName}\" -c copy -bsf:v h264_mp4toannexb -f mpegts \"{tempFile.FullName}\" -y";

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("FFMPeg ");
                    Console.WriteLine(args);
                    Console.ForegroundColor = ConsoleColor.White;

                    var procInfo = new ProcessStartInfo();
                    procInfo.FileName = "FFMpeg.exe";
                    procInfo.UseShellExecute = false;
                    procInfo.Arguments = args;
                    var process = Process.Start(procInfo);
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new ApplicationException($"FFMpeg exited with error code {process.ExitCode}");
                    }
                }

                // Put them together
                {
                    var sb = new StringBuilder();
                    foreach(var tempFile in tempFiles)
                    {
                        if (sb.Length == 0)
                        {
                            sb.Append("-hide_banner -i \"concat:");
                        }
                        else
                        {
                            sb.Append('|');
                        }
                        sb.Append(tempFile.FullName);
                    }
                    sb.Append("\" -c copy -bsf:a aac_adtstoasc -movflags faststart -f mp4 \"");
                    sb.Append(s_output);
                    sb.Append('"');
                    var args = sb.ToString();

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("FFMPeg ");
                    Console.WriteLine(args);
                    Console.ForegroundColor = ConsoleColor.White;

                    var procInfo = new ProcessStartInfo();
                    procInfo.FileName = "FFMpeg.exe";
                    procInfo.UseShellExecute = false;
                    procInfo.Arguments = args;

                    var process = Process.Start(procInfo);
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new ApplicationException($"FFMpeg exited with error code {process.ExitCode}");
                    }
                }
            }
            finally
            {
                foreach(var tempFile in tempFiles)
                {
                    try
                    {
                        tempFile.Dispose();
                    }
                    catch
                    {
                        // Do nothing
                    }
                }
            }

        }

        class InputFile
        {
            public string FileName { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Duration { get; set; }
        }
    }

}
