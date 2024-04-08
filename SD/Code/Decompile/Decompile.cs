using Microsoft.Data.Sqlite;

namespace SD.Code.Decompile;

class Decompile
{
    public Decompile() { }

    public static void Run()
    {
        int id;

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

        CreateDirs();

        // Open File
        using SqliteConnection connection = new($"Data Source={connectionString}");
        connection.Open();

        // Maybe i fix this, but not today :)
        long tempCount = 0;
        try
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n{ex.Message}\nReturning to main menu.\nPress any key...");
            Console.ReadKey();
            return;
        }
        int buildcount = Convert.ToInt32(tempCount);

        //Build Lists

        List<string> buildsList = new();
        List<string> buildId = new();
        string adrBuilds = "SELECT ForkId FROM ContentVersion";

        // This is support for Multi-launchers and versions
        // Because this shit try to find table which CAN exist and CAN'T

        using (SqliteCommand command = new(adrBuilds, connection))
        {
            SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
                buildsList.Add(reader["ForkId"].ToString());

            reader.Close();
        }

        using (SqliteCommand command = new("SELECT Id FROM ContentVersion", connection))
        {
            SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
                buildId.Add(reader["Id"].ToString());

            reader.Close();
        }
        var copy = string.Empty;

        for (int c = 0; c < buildsList.Count; c++)
        {

            if (buildsList[c].StartsWith("git@github.com"))
                buildsList[c] = buildsList[c].Substring(" git@github.com".Length).TrimStart();

            int count = 0;
            for (int j = 0; j < buildsList.Count; j++)
            {
                if (buildsList[c] == buildsList[j])
                {
                    count++;
                    if (count > 1)
                        buildsList[j] = buildsList[j] + "_";
                }
            }
        }
        Console.WriteLine($"Founded {buildsList.Count} build" + (buildcount == 1 ? " " : "s"));
        for (int i = 0; i < buildsList.Count; i++)
            Console.WriteLine(buildsList[i] + buildId[i]);

        Console.ReadKey(true);

        for (int build = 1; build < buildsList.Count + 1; build++)
        {
            GetMaxMin(connection, out var maxID, out var minID, Convert.ToInt32(buildId[build - 1]));

            int maxID_ = (int)maxID;
            int minID_ = (int)minID;

            for (int j = minID_; j < maxID_ + 1; j++)
            {
                double progress = ((double)j - minID_) / maxID_ * 100;

                Console.WriteLine($"Progress: {progress:0.00}%");

                //Console.ReadKey(true);
                id = Convert.ToInt32(j);

                // Searching Path
                int ContentId = id;

                SqliteCommand commandPath = new("SELECT Path FROM ContentManifest WHERE ContentId = @ContentId AND VersionId = @VersionId", connection);
                commandPath.Parameters.AddWithValue("@ContentId", ContentId);
                commandPath.Parameters.AddWithValue("@VersionId", Convert.ToInt32(buildId[build - 1]));

                // Scanning
                using (SqliteDataReader reader = commandPath.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        path = reader.GetString(0);
                        Console.WriteLine($"Path of ContentId {ContentId}: {path}");
                    }
                    else
                    {
                        Console.WriteLine($"No logs for ContentId {ContentId}.");
                    }
                    reader.Close();
                }

                string temp = path;

                // Creating folder address
                string result_ = path;
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

                // Creating folder
                Directory.CreateDirectory($"Decoded\\{buildsList[build - 1]}\\{str}");

                // Creating Format
                result_ = str;

                string result = string.Empty;

                if (count == 0)
                    result = path;
                else
                    result = path.Substring(str.Length + 1);

                for (i = temp.Length - 1; i != 0; i--)
                {
                    char c = temp[i];
                    if (c == '.')
                        break;
                }

                int index = temp.LastIndexOf('.');
                string format = temp.Substring(index + 1);

                // Decompile the file
                using SqliteCommand command = new("SELECT Data FROM Content WHERE ID = @id", connection);
                command.Parameters.AddWithValue("@id", id);

                string compressionLevel_ = string.Empty;

                SqliteCommand commandPath2 = new("SELECT Compression FROM Content WHERE ID = @id", connection);
                commandPath2.Parameters.AddWithValue("@id", id);

                using (SqliteDataReader reader = commandPath2.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        compressionLevel_ = reader.GetString(0);
                    }
                    reader.Close();
                }

                int compressionLevel;
                if (compressionLevel_ == string.Empty || compressionLevel_ == "0")
                    compressionLevel = 0;
                else
                    compressionLevel = Convert.ToInt32(compressionLevel_);

                using (SqliteDataReader reader = command.ExecuteReader())
                {

                    if (!reader.Read())
                        Console.WriteLine("Wrong ID! ");
                    else
                    {
                        if (compressionLevel == 0) // 0 == dec 
                        {
                            byte[] data = (byte[])reader["Data"];
                            string fileName = $"Decoded\\{buildsList[build - 1]}\\{result_}\\{result}";
                            // Save
                            File.WriteAllBytes(fileName, data);
                        }
                        else // 0 < dec
                        {
                            byte[] blobData = (byte[])reader["Data"];

                            Stream stream = new MemoryStream(blobData);
                            File.Delete(result);
                            using var decompressionStream = new ZstdSharp.DecompressionStream(stream);

                            using var output = File.OpenWrite("temp\\" + result);
                            decompressionStream.CopyTo(output);
                            output.Close();

                            // Save
                            if (!Directory.Exists($"Decoded\\{buildsList[build - 1]}\\{result_}\\{result}"))
                                File.Delete($"Decoded\\{buildsList[build - 1]}\\{result_}\\{result}");

                            Directory.Move($"temp\\{result}", $"Decoded\\{buildsList[build - 1]}\\{result_}\\{result}");
                        }
                    }
                }
            }
        }
        Directory.Delete("temp", true);

        Console.WriteLine("Press any key...");
        Console.ReadKey(true);
    }

    /// <summary>
    /// Creates the directories Decoded and temp if they do not exist.
    /// </summary>
    private static void CreateDirs()
    {
        if (!Directory.Exists("Decoded"))
            Directory.CreateDirectory("Decoded");

        if (!Directory.Exists("temp"))
            Directory.CreateDirectory("temp");
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
    /// Gets the maximum and minimum ContentId for a given VersionId in the ContentManifest table.
    /// </summary>
    /// <param name="connection">The SqliteConnection object to use for the query.</param>
    /// <param name="MaxID">The maximum ContentId.</param>
    /// <param name="MinID">The minimum ContentId.</param>
    /// <param name="VersionId">The VersionId to query.</param>
    private static void GetMaxMin(SqliteConnection connection, out long MaxID, out long MinID, int VersionId)
    {
        MaxID = long.MinValue;
        MinID = long.MaxValue;
        Console.WriteLine(VersionId);
        using (var command = new SqliteCommand("SELECT MAX(ContentId) FROM ContentManifest WHERE VersionId = @VersionId", connection))
        {
            command.Parameters.AddWithValue("@VersionId", VersionId);

            var res = command.ExecuteScalar();
            if (res != DBNull.Value)
                MaxID = Convert.ToInt64(res);

        }

        using (var command = new SqliteCommand("SELECT MIN(ContentId) FROM ContentManifest WHERE VersionId = @VersionId", connection))
        {
            command.Parameters.AddWithValue("@VersionId", VersionId);
            var res = command.ExecuteScalar();

            if (res != DBNull.Value)
                MinID = Convert.ToInt64(res);
        }
    }
}
