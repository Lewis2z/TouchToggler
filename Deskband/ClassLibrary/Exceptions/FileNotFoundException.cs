namespace TouchTogglerClassLibrary.Exceptions
{
    public class FIleNotFoundException : System.ComponentModel.Win32Exception
    {
        public FIleNotFoundException(string filePath) : base(ModifyMessage("File not found", filePath))
        {
        }

        private static string ModifyMessage(string message, string extraInfo)
        {
            return message.ToLowerInvariant() + ": " + extraInfo;
        }
    }
}
