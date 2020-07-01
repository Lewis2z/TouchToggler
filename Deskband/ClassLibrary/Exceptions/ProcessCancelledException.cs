namespace TouchTogglerClassLibrary.Exceptions
{
    public class ProcessCancelledException : System.ComponentModel.Win32Exception
    {
        public new string Message = "The operation was canceled by the user";
        public ProcessCancelledException()
        {
        }
    }
}
