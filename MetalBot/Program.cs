using System.Threading.Tasks;
using MetalBot.Core;

namespace MetalBot
{
    public class Program
    {
        internal static async Task Main(string[] args)
        {
            await Metal.StartAsync(args);
        }
    }
}