using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection.PortableExecutable;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Microsoft.Data.Sqlite;
using ZstdSharp;
class Program
{
    static void Main()
    {
        int choice = -1;
        string path = string.Empty;
        //List<string> buildsList = new List<string>();
        long buildcount = 0;
        do
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            string logo = "                      _ _           _                            _             \n  ___ _   _ _ __   __| (_)       __| | ___  ___ _ __ _   _ _ __ | |_ ___  _ __ \n / __| | | | '_ \\ / _` | |_____ / _` |/ _ \\/ __| '__| | | | '_ \\| __/ _ \\| '__|\n \\__ \\ |_| | | | | (_| | |_____| (_| |  __/ (__| |  | |_| | |_) | || (_) | |   \n |___/\\__, |_| |_|\\__,_|_|      \\__,_|\\___|\\___|_|   \\__, | .__/ \\__\\___/|_|   \n      |___/                                          |___/|_|                  \n";
            Console.WriteLine(logo);
            Console.WriteLine("1) Decompiler");
            Console.WriteLine("2) Compiler");
            Console.WriteLine("0) Exit");
            ConsoleKey key;
            key = Console.ReadKey().Key;
            choice = Convert.ToInt32(key);
            Console.WriteLine();
            switch (choice)
            {
                case 49:
                    int id;
                    int compressionLevel = 0;
                    string format = string.Empty;
                    if (!Directory.Exists("Decoded"))
                        Directory.CreateDirectory("Decoded");


                    if (!Directory.Exists("temp"))
                        Directory.CreateDirectory("temp");

                    string connectionString = "Data Source=content.db";


                    // Open File
                    using (SqliteConnection connection = new SqliteConnection(connectionString))
                            {
                        connection.Open();

                        // Build counts
                        // Maybe i fix this, but not today :)
                        string bebey = "SELECT MAX(Id) FROM ContentVersion";
                        using (SqliteCommand command = new SqliteCommand(bebey, connection))
                        {
                            buildcount = (long)command.ExecuteScalar();
                            Console.WriteLine("Version Count: " + buildcount);
                        }

                        int buildcount_ = (int)buildcount;

                        //Build Lists

                        /*                        string adrBuilds = "SELECT ForkId FROM ContentVersion";

                                                    using (SqliteCommand command = new SqliteCommand(adrBuilds, connection))
                                                    {
                                                        SqliteDataReader reader = command.ExecuteReader();

                                                        while (reader.Read())
                                                        {
                                                            buildsList.Add(reader["ForkId"].ToString());
                                                        }

                                                        reader.Close();
                                                    }


                                                for (int с = 0; с < buildsList.Count; с++)
                                                {
                                                    int count = 0;
                                                    for (int j = 0; j < buildsList.Count; j++)
                                                    {
                                                        if (buildsList[с] == buildsList[j])
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
                                                    Console.WriteLine(build);*/

                        //for(int build = 0; build < buildsList.Count; build++)
                        //{
                        //int VersionId = build + 1;
                        //Console.WriteLine($"{VersionId} build {build}");


                        Console.ReadKey(true);
                        // ID counter
                        long maxID = 0;
                        long startId;

                        string buildAdr2 = "SELECT MAX(ContentId) FROM ContentManifest";

                        using (var command = new SqliteCommand(buildAdr2, connection))
                        {

                            var res = command.ExecuteScalar();

                            if (res != DBNull.Value)
                            {
                                maxID = Convert.ToInt64(res);
                            }

                        }

                        int maxID_ = (int)maxID;

                            for (int j = 1; j < maxID_ + 1; j++)
                            {
                               
                                double progress = (double)j / maxID_ * 100;

                                Console.WriteLine($"Progress: {progress.ToString("0.00")}%");

                                //Console.ReadKey(true);
                                id = Convert.ToInt32(j);
                                string query = "SELECT Data FROM Content WHERE ID = @id";


                                // Searching Path
                                int ContentId = id;

                                string namespace_ = "SELECT Path FROM ContentManifest WHERE ContentId = @ContentId";
                                SqliteCommand commandPath = new SqliteCommand(namespace_, connection);
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
                            // }
                             Directory.Delete("temp", true);
                        }
                    }
                        break;
                    case 50:
                        Console.WriteLine("WORK IN PROGRESS");
                        break;
                    case 48: choice = 0;
                        break;

                }
            
            Console.WriteLine("Press any key.");
                Console.ReadKey(true);

        } while(choice != 0);
    }
}
        

    
