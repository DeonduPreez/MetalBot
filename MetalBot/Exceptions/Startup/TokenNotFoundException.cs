using System;

namespace MetalBot.Exceptions.Startup
{
    public class TokenNotFoundException: Exception
    {
        public TokenNotFoundException()
        {
        }
        
        public TokenNotFoundException(string message): base(message)
        {
        }
    }
}