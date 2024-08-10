namespace Project1
{
    internal class Log
    {

        public static void Server(string? value)
        {
            Console.WriteLine($"[Server]: {value}");
        }

        public static void Socket(string? value)
        {
            Console.WriteLine($"[Socket]: {value}");
        }

        public static void Simulate(string? value)
        {
            Console.WriteLine($"[Simulate]: {value}");
        }
    }
}
