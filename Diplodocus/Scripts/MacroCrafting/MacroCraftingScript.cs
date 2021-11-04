using System;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.IoC;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace Diplodocus.Scripts.MacroCrafting
{
    public sealed class MacroCraftingScript
    {
        public struct CraftingSettings
        {
            public int            amount;
            public Action         OnScriptCompleted;
            public Action<string> OnScriptFailed;
        }

        [PluginService] private DataManager DataManager { get; set; }

        private ExcelSheet<Recipe> _recipeSheet;

        public void Enable()
        {
            _recipeSheet = DataManager.GameData.GetExcelSheet<Recipe>();
        }

        public void Disable()
        {

        }

        public void Dispose()
        {
            Disable();
        }

        public async Task Start(CraftingSettings settings)
        {

        }
    }
}
