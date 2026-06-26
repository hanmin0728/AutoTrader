using System.Collections.Generic;

public class MarketTheme
{
    public string ThemeId { get; set; }
    public string ThemeName { get; set; }
    public string Description { get; set; }
    public List<ThemeStockInfo> StockList { get; set; } = new List<ThemeStockInfo>();
}