using System.Collections.Generic;
using UnityEngine;

public class ThemeDataManager : MonoBehaviour
{
    public List<MarketTheme> ThemeMasterList { get; private set; } = new List<MarketTheme>();
    private void Awake()
    {
        InitializeDummyThemes();
    }

    /// <summary>
    /// 초기 테스트용 데이터 세팅 (나중에 파이썬 서버나 외부 API에서 받아오게 대치)
    /// </summary>
    private void InitializeDummyThemes()
    {
        // 반도체 테마
        MarketTheme semiConductor = new MarketTheme
        {
            ThemeId = "THEME_SEMI",
            ThemeName = "반도체",
            Description = "대한민국 핵심 수출 및 첨단 반도체 소부장 기업"
        };
        semiConductor.StockList.Add(new ThemeStockInfo { StockCode = "005930", StockName = "삼성전자" });
        semiConductor.StockList.Add(new ThemeStockInfo { StockCode = "042700", StockName = "한미반도체" });

        // 전력설비 테마
        MarketTheme powerGrid = new MarketTheme
        {
            ThemeId = "THEME_POWER",
            ThemeName = "전력설비",
            Description = "AI 데이터센터 증설로 인한 변압기 및 전선 수혜주"
        };
        powerGrid.StockList.Add(new ThemeStockInfo { StockCode = "001440", StockName = "대한전선" });
        powerGrid.StockList.Add(new ThemeStockInfo { StockCode = "267260", StockName = "HD현대일렉트릭" });

        // 마스터 리스트에 등록
        ThemeMasterList.Add(semiConductor);
        ThemeMasterList.Add(powerGrid);

        Debug.Log($"[ThemeDataManager] 총 {ThemeMasterList.Count}개의 테마 데이터 로드 완료.");
    }

    /// <summary>
    /// 특정 테마 ID를 주면 해당 종목 리스트를 반환하는 헬퍼 함수 (UI 스크립트가 호출할 예정)
    /// </summary>
    public List<ThemeStockInfo> GetStocksByTheme(string themeId)
    {
        var theme = ThemeMasterList.Find(t => t.ThemeId == themeId);
        return theme?.StockList;
    }
}
