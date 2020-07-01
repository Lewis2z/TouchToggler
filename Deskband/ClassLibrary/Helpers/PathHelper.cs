using System.ComponentModel;
using System.IO;

namespace TouchTogglerClassLibrary.Helpers
{
    public static class PathHelper
    {
        public enum ApplicationComponent
        {
            [Description("TouchTogglerDeskBand.dll")]
            DeskBand,
            [Description("TouchTogglerService.exe")]
            ServiceExecutable
        }
        public static string GetApplicationPath()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }
        public static string GetApplicationDirectory()
        {
            return Path.GetDirectoryName(GetApplicationPath());
        }
        public static string GetPathToApplicationComponent(ApplicationComponent component)
        {
            return Path.Combine(GetApplicationDirectory(), component.GetDescription());
        }
    }
}
