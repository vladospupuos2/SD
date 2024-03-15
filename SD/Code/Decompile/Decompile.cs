using Microsoft.Data.Sqlite;

namespace SD.Code.Decompile
{
    class Decompile
    {
        /// <summary>
        /// 
        /// </summary>
        public Decompile() { }

        /// <summary>
        /// 
        /// </summary>
        public static void Run()
        {
            string path = string.Empty;
            long buildcount = 1;
            int id;
            int compressionLevel = 0;
            string format = string.Empty;
            string launcher = "launcher-skyedra";

            string connectionString = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Space Station 14", launcher, "content.db");

            CreateDirs();

            // Open File
            using SqliteConnection connection = new($"Data Source={connectionString}");
            connection.Open();

            // Build counts
            // Maybe i fix this, but not today :)
            /*
            using (SqliteCommand command = new("SELECT MAX(Id) FROM ContentVersion", connection))
            {
                buildcount = (long)command.ExecuteScalar();
                Console.WriteLine("Version Count: " + buildcount);
            }
            */

            #region TODO. Maybe
            /*
            //Build Lists

            string adrBuilds = "SELECT ForkId FROM ContentVersion";

                                        using (SqliteCommand command = new SqliteCommand(adrBuilds, connection))
                                        {
                                            SqliteDataReader reader = command.ExecuteReader();

                                            while (reader.Read())
                                            {
                                                buildsList.Add(reader["ForkId"].ToString());
                                            }

                                            reader.Close();
                                        }


                                    for (int ñ = 0; ñ < buildsList.Count; ñ++)
                                    {
                                        int count = 0;
                                        for (int j = 0; j < buildsList.Count; j++)
                                        {
                                            if (buildsList[ñ] == buildsList[j])
                                            {
                                                count++;
                                                if (count > 1)
                                                {
                                                    buildsList[j] = buildsList[j] + "_";
                                                }
                                            }
                                        }
                                    }
                                    Console.WriteLine($"Founded {buildsList.Count} build" + (buildcount == 1 ? " " : "s"));
                                    foreach (var build in buildsList)
                                        Console.WriteLine(build);

            for(int build = 0; build < buildsList.Count; build++)
            {
            int VersionId = build + 1;
            Console.WriteLine($"{VersionId} build {build}");
            }
            */
            #endregion

            // ID counter
            long maxID = 0;
            long minID = 0;

            using (var command = new SqliteCommand("SELECT MAX(ContentId) FROM ContentManifest", connection))
            {

                var res = command.ExecuteScalar();

                if (res != DBNull.Value)
                {
                    maxID = Convert.ToInt64(res);
                }

            }

            using (var command = new SqliteCommand("SELECT MIN(ContentId) FROM ContentManifest", connection))
            {

                var res = command.ExecuteScalar();

                if (res != DBNull.Value)
                {
                    minID = Convert.ToInt64(res);
                }

            }

            int maxID_ = (int)maxID;
            int minID_ = (int)minID;

            for (int j = minID_; j < maxID_ + 1; j++)
            {

                double progress = ((double)j - minID_) / maxID_ * 100;

                Console.WriteLine($"Progress: {progress:0.00}%");

                //Console.ReadKey(true);
                id = Convert.ToInt32(j);
                string query = "SELECT Data FROM Content WHERE ID = @id";


                // Searching Path
                int ContentId = id;

                SqliteCommand commandPath = new("SELECT Path FROM ContentManifest WHERE ContentId = @ContentId", connection);
                commandPath.Parameters.AddWithValue("@ContentId", ContentId);



                // Scaning
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

                for (i = result_.Length - 1; i != 0; i--)
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

                Directory.CreateDirectory($"Decoded\\{str}");

                // Creating Format
                result_ = str;

                string result = path.Substring(str.Length + 1);


                for (i = temp.Length - 1; i != 0; i--)
                {
                    char c = temp[i];
                    if (c == '.')
                        break;

                }

                int index = temp.LastIndexOf('.');
                format = temp.Substring(index + 1);


                // Decompiler the file
                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    string compressionLevel_ = string.Empty;

                    string compspace_ = "SELECT Compression FROM Content WHERE ID = @id";
                    SqliteCommand commandPath2 = new SqliteCommand(compspace_, connection);
                    commandPath2.Parameters.AddWithValue("@id", id);

                    using (SqliteDataReader reader = commandPath2.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            compressionLevel_ = reader.GetString(0);

                        }
                        reader.Close();
                    }
                    if (compressionLevel_ == string.Empty || compressionLevel_ == "0")
                        compressionLevel = 0;
                    else
                        compressionLevel = Convert.ToInt32(compressionLevel_);


                    using (SqliteDataReader reader = command.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            if (compressionLevel == 0) // 0 == dec 
                            {
                                byte[] data = (byte[])reader["Data"];
                                string fileName = $"Decoded\\{result_}\\{result}";
                                // Save
                                File.WriteAllBytes(fileName, data);
                                //Console.WriteLine($"Succesful Blob to " + result);
                            }
                            else                                                        // 0 < dec
                            {
                                byte[] blobData = (byte[])reader["Data"];

                                Stream stream = new MemoryStream(blobData);
                                File.Delete(result);
                                using var decompressionStream = new ZstdSharp.DecompressionStream(stream);

                                using var output = File.OpenWrite("temp\\" + result);
                                decompressionStream.CopyTo(output);
                                output.Close();

                                // Save
                                if (!Directory.Exists($"Decoded\\{result_}\\{result}"))
                                {
                                    File.Delete($"Decoded\\{result_}\\{result}");

                                }

                                Directory.Move($"temp\\{result}", $"Decoded\\{result_}\\{result}");
                                //Console.WriteLine($"Succesful Blob to {result}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong ID! ");
                        }

                    }
                }
            }

            Directory.Delete("temp", true);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void CreateDirs()
        {
            if (!Directory.Exists("Decoded"))
                Directory.CreateDirectory("Decoded");

            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");
        }
    }
}
