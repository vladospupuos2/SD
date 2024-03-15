using SD.Code.Compiler;
using SD.Code.Decompile;

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            string logo = "                      _ _           _                            _             \n  ___ _   _ _ __   __| (_)       __| | ___  ___ _ __ _   _ _ __ | |_ ___  _ __ \n / __| | | | '_ \\ / _` | |_____ / _` |/ _ \\/ __| '__| | | | '_ \\| __/ _ \\| '__|\n \\__ \\ |_| | | | | (_| | |_____| (_| |  __/ (__| |  | |_| | |_) | || (_) | |   \n |___/\\__, |_| |_|\\__,_|_|      \\__,_|\\___|\\___|_|   \\__, | .__/ \\__\\___/|_|   \n      |___/                                          |___/|_|                  \n";
            Console.WriteLine(logo);
            Console.WriteLine("1) Decompile build");
            Console.WriteLine("2) Compiler");
            Console.WriteLine("0) Exit");
            ConsoleKey key;
            key = Console.ReadKey().Key;
            int choice = Convert.ToInt32(key);
            Console.WriteLine();
            switch (choice)
            {
                case 49:
                    Decompile.Run();
                    break;

                case 50:
                    Compiler.Run();
                    break;

                case 48:
                    return;
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey(true);
        }
    }
}
