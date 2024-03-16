using SD.Code.Compiler;
using SD.Code.Decompile;

class Program
{
    static void Main()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        string logo = "                      _ _           _                            _             \n  ___ _   _ _ __   __| (_)       __| | ___  ___ _ __ _   _ _ __ | |_ ___  _ __ \n / __| | | | '_ \\ / _` | |_____ / _` |/ _ \\/ __| '__| | | | '_ \\| __/ _ \\| '__|\n \\__ \\ |_| | | | | (_| | |_____| (_| |  __/ (__| |  | |_| | |_) | || (_) | |   \n |___/\\__, |_| |_|\\__,_|_|      \\__,_|\\___|\\___|_|   \\__, | .__/ \\__\\___/|_|   \n      |___/                                          |___/|_|                  \n";
        
        while (true)
        {
            Console.Clear();
            Console.WriteLine(logo);
            Console.WriteLine("1) Decompile build");
            Console.WriteLine("2) Compiler");
            Console.WriteLine("0) Exit");

            ConsoleKeyInfo key = Console.ReadKey();

            int choice = -1;
            if (char.IsDigit(key.KeyChar))
                choice = int.Parse(key.KeyChar.ToString());

            Console.WriteLine();
            switch (choice)
            {
                case 1:
                    Decompile.Run();
                    break;

                case 2:
                    Compiler.Run();
                    break;

                case 0:
                    return;
            }
        }
    }
}
