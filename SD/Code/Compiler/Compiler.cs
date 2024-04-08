using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection.Emit;
using System.Xml.Linq;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace SD.Code.Compiler;
class Compiler
{

    /// <summary>
    /// 
    /// </summary>
    public Compiler() { }

    /// <summary>
    /// 
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

        string uploadFolder = "Upload";

        // Some checking code
        if (!Directory.Exists(uploadFolder))
        {
            Console.WriteLine("\nFolder not found, please create an \"Upload\" folder and upload files.");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Select Mode:\n1) Upload single file\n2) Upload folder ");

        ConsoleKeyInfo modeChoice;
        int Mchoice = -1;
        do
        {
            modeChoice = Console.ReadKey(true);

            if (char.IsDigit(modeChoice.KeyChar))
                Mchoice = int.Parse(modeChoice.KeyChar.ToString());
        }
        while (Mchoice <= 0 || Mchoice >= 3);

        Console.WriteLine("Enter filename: ");
        string input = string.Empty;
        input = Console.ReadLine();
        input = input.Replace('\\', '/');
        List<string> fileNames = new List<string>();
        if (Mchoice == 2)
        {
            Console.WriteLine();
            if (Directory.Exists(Path.Combine(uploadFolder, input)))
            {

                string[] files = Directory.GetFiles(Path.Combine(uploadFolder, input));

                foreach (string file in files)
                {
                    fileNames.Add(Path.GetFileName(file));
                }


                foreach (string fileName in fileNames)
                {
                    Console.WriteLine(fileName);
                }
            }
            else
            {
                Console.WriteLine("Folder not found.");
                Console.ReadKey();
                return;
            }
        }
        else
        {
            fileNames.Add(input);
        }

            char lastChar = input[input.Length - 1];

            if (lastChar == '\\' || lastChar == '/')
            {
                input = input.Substring(0, input.Length - 1);
            }

            string output = "UpFile";

        Console.WriteLine();

        using SqliteConnection connection = new($"Data Source={connectionString}");
        connection.Open();

        foreach (string file in fileNames)
        {
            List<int> contentIds;
            if (Mchoice == 2)
                contentIds = GetContentIdsByPath(connection, input + "/" + file);
            else
                contentIds = GetContentIdsByPath(connection, input);

            if (contentIds.Count > 0)
            {
                Console.WriteLine($"Found {contentIds.Count}:");
                foreach (int contentId in contentIds)
                {
                    Console.WriteLine($"ContentId: {contentId}");
                }
            }
            else
            {
                Console.WriteLine("Not found.");
                Console.ReadKey();
                return;
            }

            if (Mchoice == 1)
            {
                long compressedFileSize;
                int compressionLevel;
                foreach (int contentId in contentIds)
                {
                    using (SqliteCommand command = new("SELECT Data FROM Content WHERE ID = @contentId", connection))
                    {

                        command.Parameters.AddWithValue("@contentId", contentId);
                        string compressionLevel_ = string.Empty;

                        SqliteCommand commandPath2 = new("SELECT Compression FROM Content WHERE ID = @contentId", connection);
                        commandPath2.Parameters.AddWithValue("@contentId", contentId);

                        using (SqliteDataReader reader = commandPath2.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                compressionLevel_ = reader.GetString(0);
                            }
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

                        string sql = "UPDATE Content SET data = @imageData, Size = @size WHERE ID = @id";

                        using (SqliteCommand command2 = new SqliteCommand(sql, connection))
                        {
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

                    }


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

                    using (SqliteCommand command = new("SELECT Data FROM Content WHERE ID = @contentId", connection))
                    {

                        command.Parameters.AddWithValue("@contentId", contentId);
                        string compressionLevel_ = string.Empty;

                        SqliteCommand commandPath2 = new("SELECT Compression FROM Content WHERE ID = @contentId", connection);
                        commandPath2.Parameters.AddWithValue("@contentId", contentId);

                        bool noDec = false;

                        using (SqliteDataReader reader = commandPath2.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                compressionLevel_ = reader.GetString(0);
                            }
                            reader.Close();
                        }
                        if (compressionLevel_ == string.Empty || compressionLevel_ == "0")
                        {
                            compressionLevel = 0;
                        }
                        else
                        {
                            compressionLevel = Convert.ToInt32(compressionLevel_);
                        }
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

                        string sql = "UPDATE Content SET data = @imageData, Size = @size WHERE ID = @id";

                        using (SqliteCommand command2 = new SqliteCommand(sql, connection))
                        {
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


                    }
                }
                Console.WriteLine("DONE!");
            }
              
        }
        Console.ReadKey();
        connection.Close();
    }

    public static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    static void CompressDataToFile(string inputFilePath, string outputFilePath, int compressionLevel)
    {
        using var input = File.OpenRead(inputFilePath);
        using var output = File.OpenWrite(outputFilePath);
        using var compressionStream = new CompressionStream(output, compressionLevel);
        compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);
        input.CopyTo(compressionStream);
    }

    static string Trim(string s)
    {
        string temp = s;

        string result_ = s;
        int i;
        int count = 0;

        for (i = result_.Length - 1; i >= 0; i--)
        {
            char c = result_[i];
            if (c == '/')
                count++;

        }
        string str = string.Empty;
        int c_ = 0;
        for (i = 0; i < result_.Length; i++)
        {

            if (result_[i] == '/')
                c_++;
            if (c_ == count)
                break;
            str += result_[i];
        }

        string result = string.Empty;

        if (count == 0)
            result = s;
        else
            result = s.Substring(str.Length + 1);

        return result;
    }

    static List<int> GetContentIdsByPath(SqliteConnection connection, string userInputPath)
    {
        List<int> contentIds = new List<int>();

            string query = "SELECT ContentId, Path FROM ContentManifest";
        using (SqliteCommand command = new SqliteCommand(query, connection))
        {
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string pathFromDatabase = reader.GetString(1);
                    //Console.WriteLine(pathFromDatabase);
                    //pathFromDatabase = Trim(pathFromDatabase);


                    if (string.Equals(pathFromDatabase, userInputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        contentIds.Add(reader.GetInt32(0));
                    }
                }
                reader.Close();
            }
        }
        

        return contentIds;
    }
}



