namespace OpenGaugeServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await ServerApp.Run(args);
        }
    }
}
