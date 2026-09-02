using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreGuns
{
    public static class Config
    {
        public static MelonPreferences_Category Category { get; private set; }
        public static MelonPreferences_Entry<bool> EnableCrosshairForGuns { get; private set; }
        public static MelonPreferences_Entry<bool> AllowMinigunManualReload { get; private set; }
        public static string folderPath = "UserData/MoreGuns.cfg";
        public const string PREFIX = "MoreGuns";
        
        public static void Initialize()
        {
            Category = MelonPreferences.CreateCategory($"{PREFIX}-! User Settings");
            EnableCrosshairForGuns = Category.CreateEntry("Allow Gun Crosshair", true);
            AllowMinigunManualReload = Category.CreateEntry(
                "Allow Minigun Manual Reload",
                true,
                description: "If false, the MiniGun can only be reloaded by talking to Stan.");
            Category.SetFilePath(folderPath);
            Category.SaveToFile();
        }
    }
}
