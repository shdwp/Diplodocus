using Lumina.Excel.GeneratedSheets;

namespace Diplodocus.Lib.GSheets
{
    public sealed partial class GSheetsClient
    {
        public struct Row
        {
            public string[] rawData;

            public Item item;

            public Row(Item item, string[] rawData)
            {
                this.item = item;
                this.rawData = rawData;
            }

            public float? GetFloat(int idx)
            {
                var value = GetString(idx);
                if (value != null && float.TryParse(value, out var floatValue))
                {
                    return floatValue;
                }
                else
                {
                    return null;
                }
            }

            public string? GetString(int idx)
            {
                if (idx > 0 && idx < rawData.Length)
                {
                    return rawData[idx];
                }
                else
                {
                    return null;
                }
            }
        }
    }
}