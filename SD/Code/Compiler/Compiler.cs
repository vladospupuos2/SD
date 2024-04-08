using Microsoft.Data.Sqlite;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace SD.Code.Compiler;
class Compiler
{
    /// <summary>
    /// A class that contains methods for compiling code.
    /// </summary>
    public Compiler() { }

    /// <summary>
    /// Runs the compiler.
    /// </summary>
    public static void Run()
    {
        // Seacrhing Path
        // Nah, maybe i rework this, but its wokring
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Space Station 14");
        string[] folders = Directory.GetDirectories(path);
        Console.WriteLine("Choose launcher");
        int countFolders = 1;
        foreach (string folder in folders)
        {
            var temp = string.Empty;
            for (int i = folder.Length - 1; i > 0; i--)
            {
                if (folder[i] == '\\')
                    break;
                temp += folder[i];
            }
            temp = Reverse(temp);

            Console.WriteLine($"{countFolders}) {temp}");
            countFolders++;
        }

        ConsoleKeyInfo launcherChoice;
        int choice = -1;
        do
        {
            launcherChoice = Console.ReadKey(true);

            if (char.IsDigit(launcherChoice.KeyChar))
                choice = int.Parse(launcherChoice.KeyChar.ToString());
        }
        while (choice <= 0 || choice >= countFolders);

        string launcher = folders[choice - 1];

        string connectionString = Path.Combine(path, launcher, "content.db");

        // Some checking code
        if (!CheckingFolder(out var uploadFolder))
            return;

        Console.WriteLine("Select Mode:\n1) Upload single file\n2) Upload folder ");

        ModeSelect(out var selectedMode);

        Console.WriteLine("Enter filename: ");
        string input = Console.ReadLine()!;
        input = input.Replace('\\', '/')!;
        List<string> fileNames = new();
        if (selectedMode != 2)
        {
            fileNames.Add(input);
        }
        else
        {
            Console.WriteLine();
            if (Directory.Exists(Path.Combine(uploadFolder, input)))
            {
                string[] files = Directory.GetFiles(Path.Combine(uploadFolder, input));

                foreach (string file in files)
                    fileNames.Add(Path.GetFileName(file));

                foreach (string fileName in fileNames)
                    Console.WriteLine(fileName);
            }
            else
            {
                Console.WriteLine("Folder not found.");
                Console.ReadKey();
                return;
            }
        }

        char lastChar = input[input.Length - 1];

        if (lastChar == '\\' || lastChar == '/')
            input = input.Substring(0, input.Length - 1);

        string output = "UpFile";

        Console.WriteLine();

        using SqliteConnection connection = new($"Data Source={connectionString}");
        connection.Open();

        foreach (string file in fileNames)
        {
            List<int> contentIds;
            if (selectedMode == 2)
                contentIds = GetContentIdsByPath(connection, input + "/" + file);
            else
                contentIds = GetContentIdsByPath(connection, input);

            if (contentIds.Count > 0)
            {
                Console.WriteLine($"Found {contentIds.Count}:");
                foreach (int contentId in contentIds)
                    Console.WriteLine($"ContentId: {contentId}");
            }
            else
            {
                Console.WriteLine("Not found.");
                Console.ReadKey();
                return;
            }

            if (selectedMode == 1)
            {
                long compressedFileSize;
                int compressionLevel;
                foreach (int contentId in contentIds)
                {
                    using SqliteCommand command = new("SELECT Data FROM Content WHERE ID = @contentId", connection);

                    command.Parameters.AddWithValue("@contentId", contentId);
                    string compressionLevel_ = string.Empty;

                    SqliteCommand commandPath2 = new("SELECT Compression FROM Content WHERE ID = @contentId", connection);
                    commandPath2.Parameters.AddWithValue("@contentId", contentId);

                    using (SqliteDataReader reader = commandPath2.ExecuteReader())
                    {
                        if (reader.Read())
                            compressionLevel_ = reader.GetString(0);

                        reader.Close();
                    }

                    if (compressionLevel_ == string.Empty || compressionLevel_ == "0")
                    {
                        compressionLevel = 0;
                    }
                    else
                    {
                        compressionLevel = Convert.ToInt32(compressionLevel_);
                        CompressDataToFile(Path.Combine(uploadFolder, input), output, compressionLevel);
                    }


                    compressedFileSize = new FileInfo(Path.Combine(uploadFolder, input)).Length;

                    using SqliteCommand command2 = new("UPDATE Content SET data = @imageData, Size = @size WHERE ID = @id", connection);
                    byte[] FileData;

                    if (compressionLevel != 0)
                        FileData = File.ReadAllBytes(output);
                    else
                        FileData = File.ReadAllBytes("Upload\\" + input);

                    command2.Parameters.AddWithValue("@imageData", FileData);
                    command2.Parameters.AddWithValue("@size", compressedFileSize);
                    command2.Parameters.AddWithValue("@id", contentId);
                    command2.ExecuteNonQuery();


                }
                Console.WriteLine("DONE!");
                connection.Close();
                File.Delete(output);

            }
            else
            {
                long compressedFileSize;
                int compressionLevel;
                foreach (int contentId in contentIds)
                {

                    using SqliteCommand command = new("SELECT Data FROM Content WHERE ID = @contentId", connection);

                    command.Parameters.AddWithValue("@contentId", contentId);
                    string compressionLevel_ = string.Empty;

                    SqliteCommand commandPath2 = new("SELECT Compression FROM Content WHERE ID = @contentId", connection);
                    commandPath2.Parameters.AddWithValue("@contentId", contentId);

                    bool noDec = false;

                    using (SqliteDataReader reader = commandPath2.ExecuteReader())
                    {
                        if (reader.Read())
                            compressionLevel_ = reader.GetString(0);

                        reader.Close();
                    }

                    if (compressionLevel_ == string.Empty || compressionLevel_ == "0")
                        compressionLevel = 0;
                    else
                        compressionLevel = Convert.ToInt32(compressionLevel_);

                    if (compressionLevel != 0)
                    {
                        File.Create(file + ".bin");
                        CompressDataToFile(Path.Combine(uploadFolder, input, file), file + ".bin", compressionLevel);
                    }
                    else
                    {
                        noDec = true;
                    }

                    compressedFileSize = new FileInfo(Path.Combine(uploadFolder, input, file)).Length;

                    using SqliteCommand command2 = new("UPDATE Content SET data = @imageData, Size = @size WHERE ID = @id", connection);
                    byte[] FileData;

                    if (!noDec)
                        FileData = File.ReadAllBytes(file + ".bin");
                    else
                        FileData = File.ReadAllBytes(Path.Combine(uploadFolder, input, file));

                    command2.Parameters.AddWithValue("@imageData", FileData);
                    command2.Parameters.AddWithValue("@size", compressedFileSize);
                    command2.Parameters.AddWithValue("@id", contentId);
                    command2.ExecuteNonQuery();

                    if (noDec)
                        File.Delete(file + ".bin");
                }
                Console.WriteLine("DONE!");
            }
        }
        Console.ReadKey();
        connection.Close();
    }

    /// <summary>
    /// Reverses the characters in a string.
    /// </summary>
    /// <param name="s">The string to reverse.</param>
    /// <returns>The reversed string.</returns>
    public static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    /// <summary>
    /// Compresses data from an input file to an output file using the Zstandard compression algorithm.
    /// </summary>
    /// <param name="inputFilePath">The path of the input file.</param>
    /// <param name="outputFilePath">The path of the output file.</param>
    /// <param name="compressionLevel">The compression level. 0 for fastest, 22 for slowest.</param>
    static void CompressDataToFile(string inputFilePath, string outputFilePath, int compressionLevel)
    {
        using var input = File.OpenRead(inputFilePath);
        using var output = File.OpenWrite(outputFilePath);
        using var compressionStream = new CompressionStream(output, compressionLevel);
        compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);
        input.CopyTo(compressionStream);
    }

    /// <summary>
    /// A function that prompts the user to select a mode and stores the choice in an integer variable.
    /// </summary>
    /// <param name="Mchoice">An integer variable that will store the user's choice.</param>
    static void ModeSelect(out int Mchoice)
    {
        Mchoice = -1;

        do
        {
            ConsoleKeyInfo modeChoice = Console.ReadKey(true);

            if (char.IsDigit(modeChoice.KeyChar))
                Mchoice = int.Parse(modeChoice.KeyChar.ToString());

        } while (Mchoice <= 0 || Mchoice >= 3);

    }
    /// <summary>
    /// Checks if the upload folder exists. If it does not exist, it prompts the user to create it.
    /// </summary>
    /// <param name="uploadFolder">The path to the upload folder.</param>
    /// <returns>True if the folder exists, false if the folder does not exist and the user did not create it.</returns>
    static bool CheckingFolder(out string uploadFolder)
    {
        uploadFolder = "Upload";

        if (!Directory.Exists(uploadFolder))
        {
            Console.WriteLine($"\nFolder not found, please create an \"Upload\" folder and upload files.");
            Console.ReadKey();
            return false;
        }
        return true;
    }

    /// <summary>
    /// Returns a list of content IDs that match a given path.
    /// </summary>
    /// <param name="connection">The SqliteConnection object.</param>
    /// <param name="userInputPath">The path to match.</param>
    /// <returns>A list of content IDs that match the path.</returns>
    static List<int> GetContentIdsByPath(SqliteConnection connection, string userInputPath)
    {
        List<int> contentIds = new();

        using SqliteCommand command = new("SELECT ContentId, Path FROM ContentManifest", connection);
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string pathFromDatabase = reader.GetString(1);

            if (string.Equals(pathFromDatabase, userInputPath, StringComparison.OrdinalIgnoreCase))
                contentIds.Add(reader.GetInt32(0));
        }
        reader.Close();

        return contentIds;
    }
}
