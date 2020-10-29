namespace MetalBot.Helpers.Interactive
{
    public static class ResponseHelper
    {
        public static bool IsYesResponse(string message)
        {
            if (message == null)
            {
                return false;
            }

            var lowerCaseMessage = message.ToLower();
            return lowerCaseMessage.Contains("yes") || lowerCaseMessage.Contains("y");
        }
    }
}