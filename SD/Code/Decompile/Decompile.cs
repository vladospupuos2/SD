using Microsoft.Data.Sqlite;

namespace SD.Code.Decompile
{
    class Decompiler
    {
        /// <summary>
        /// Main method to execute the decompilation process.
        /// </summary>
        public static void Run()
        {
            // Getting user path
            string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Space Station 14");
            string[] folders = Directory.GetDirectories(basePath);

            Console.WriteLine("Choose launcher:");
            for (int i = 0; i < folders.Length; i++)
                Console.WriteLine($"{i + 1}) {Path.GetFileName(folders[i])}");

            int choice;
            while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > folders.Length)
                Console.WriteLine("Wrong choice, try again.");

            string launcherPath = folders[choice - 1];
            string connectionString = Path.Combine(launcherPath, "content.db");

            CreateDirs();

            using var connection = new SqliteConnection($"Data Source={connectionString}");
            connection.Open();

            List<string> buildsList = new();
            List<string> buildIdList = new();

            using (var command = new SqliteCommand("SELECT ForkId, Id FROM ContentVersion", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    buildsList.Add(reader["ForkId"].ToString());
                    buildIdList.Add(reader["Id"].ToString());
                }
            }

            for (int i = 0; i < buildsList.Count; i++)
                buildsList[i] = ProcessBuildString(buildsList[i]);

            Console.WriteLine($"Found {buildsList.Count} build(s)");

            for (int i = 0; i < buildsList.Count; i++)
                Console.WriteLine($"{buildsList[i]}{buildIdList[i]}");

            Console.ReadKey(true);

            for (int buildIndex = 0; buildIndex < buildsList.Count; buildIndex++)
            {
                try
                {
                    ProcessBuild(connection, buildIdList[buildIndex], buildsList[buildIndex]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }
            Directory.Delete("temp", true);
            Console.WriteLine("Press any key...");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Creates necessary directories for the decompilation process.
        /// </summary>
        private static void CreateDirs()
        {
            Directory.CreateDirectory("Decoded");
            Directory.CreateDirectory("temp");
        }

        /// <summary>
        /// Processes the build string to remove unnecessary prefixes.
        /// </summary>
        /// <param name="buildString">The original build string.</param>
        /// <returns>Processed build string without the prefix.</returns>
        private static string ProcessBuildString(string buildString)
        {
            if (buildString.StartsWith("git@github.com"))
                buildString = buildString.Substring("git@github.com".Length).TrimStart();

            return buildString;
        }

        /// <summary>
        /// Processes each build by extracting its content and saving it to the appropriate directories.
        /// </summary>
        /// <param name="connection">SQLite connection to the content database.</param>
        /// <param name="buildId">The build ID from the database.</param>
        /// <param name="buildName">The name of the build.</param>
        private static void ProcessBuild(SqliteConnection connection, string buildId, string buildName)
        {
            GetMaxMin(connection, out long maxID, out long minID, int.Parse(buildId));

            for (int contentId = (int)minID; contentId <= maxID; contentId++)
            {
                double progress = ((double)contentId - minID) / (maxID - minID) * 100;
                Console.Write($"\rProgress: {progress:0.00}%");

                string path = GetContentPath(connection, contentId, int.Parse(buildId));
                string directoryPath = Path.Combine("Decoded", buildName, Path.GetDirectoryName(path));
                Directory.CreateDirectory(directoryPath);

                byte[] data = GetContentData(connection, contentId, out int compressionLevel);

                string resultFilePath = Path.Combine(directoryPath, Path.GetFileName(path));
                SaveContentData(resultFilePath, data, compressionLevel);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Retrieves the file path for the specified content and version IDs.
        /// </summary>
        /// <param name="connection">SQLite connection to the content database.</param>
        /// <param name="contentId">The content ID.</param>
        /// <param name="versionId">The version ID.</param>
        /// <returns>The file path of the content.</returns>
        /// <exception cref="Exception">Thrown if no path is found for the given IDs.</exception>
        private static string GetContentPath(SqliteConnection connection, int contentId, int versionId)
        {
            using var command = new SqliteCommand("SELECT Path FROM ContentManifest WHERE ContentId = @ContentId AND VersionId = @VersionId", connection);
            command.Parameters.AddWithValue("@ContentId", contentId);
            command.Parameters.AddWithValue("@VersionId", versionId);

            var result = command.ExecuteScalar();

            if (result == null || result.ToString() == null)
                throw new Exception($"No path found for ContentId {contentId} and VersionId {versionId}");

#pragma warning disable CS8603 // It is possible to return a reference that allows a NULL value. FUCK U COMPILE
            return result.ToString();
#pragma warning restore CS8603 // It is possible to return a reference that allows a NULL value. FUCK U COMPILE
        }

        /// <summary>
        /// Retrieves the content data and its compression level for the specified content ID.
        /// </summary>
        /// <param name="connection">SQLite connection to the content database.</param>
        /// <param name="contentId">The content ID.</param>
        /// <param name="compressionLevel">Output parameter for the compression level.</param>
        /// <returns>The content data as a byte array.</returns>
        /// <exception cref="Exception">Thrown if no data is found for the given content ID.</exception>
        private static byte[] GetContentData(SqliteConnection connection, int contentId, out int compressionLevel)
        {
            using var command = new SqliteCommand("SELECT Data, Compression FROM Content WHERE ID = @id", connection);
            command.Parameters.AddWithValue("@id", contentId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw new Exception($"No data found for ContentId {contentId}");
            }

            compressionLevel = reader.GetInt32(1);
            return (byte[])reader["Data"];
        }

        /// <summary>
        /// Saves the content data to the specified file path, decompressing it if necessary.
        /// </summary>
        /// <param name="filePath">The file path to save the content.</param>
        /// <param name="data">The content data as a byte array.</param>
        /// <param name="compressionLevel">The compression level of the data.</param>
        private static void SaveContentData(string filePath, byte[] data, int compressionLevel)
        {
            if (compressionLevel == 0)
            {
                File.WriteAllBytes(filePath, data);
            }
            else
            {
                using var inputStream = new MemoryStream(data);
                using var decompressionStream = new ZstdSharp.DecompressionStream(inputStream);
                using var outputStream = File.Create(filePath);
                decompressionStream.CopyTo(outputStream);
            }
        }

        /// <summary>
        /// Retrieves the maximum and minimum content IDs for the specified version.
        /// </summary>
        /// <param name="connection">SQLite connection to the content database.</param>
        /// <param name="maxID">Output parameter for the maximum content ID.</param>
        /// <param name="minID">Output parameter for the minimum content ID.</param>
        /// <param name="versionId">The version ID.</param>
        private static void GetMaxMin(SqliteConnection connection, out long maxID, out long minID, int versionId)
        {
            maxID = ExecuteScalar<long>(connection, "SELECT MAX(ContentId) FROM ContentManifest WHERE VersionId = @VersionId", versionId);
            minID = ExecuteScalar<long>(connection, "SELECT MIN(ContentId) FROM ContentManifest WHERE VersionId = @VersionId", versionId);
        }

        /// <summary>
        /// Executes a scalar query and returns the result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="connection">SQLite connection to the content database.</param>
        /// <param name="query">The query to execute.</param>
        /// <param name="versionId">The version ID.</param>
        /// <returns>The result of the query.</returns>
        /// <exception cref="Exception">Thrown if the query returns no result.</exception>
        private static T ExecuteScalar<T>(SqliteConnection connection, string query, int versionId)
        {
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@VersionId", versionId);
            var result = command.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                throw new Exception($"Query returned no result for VersionId {versionId}");

            return (T)result;
        }
    }
}
