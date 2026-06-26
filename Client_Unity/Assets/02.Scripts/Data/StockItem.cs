using Newtonsoft.Json;

//각 주식 관련 데이터
public class StockItem
{
    [JsonProperty("pdno")]
    public string StockCode { get; set; }     // 종목코드 

    [JsonProperty("prdt_name")]
    public string StockName { get; set; }     // 종목명 

    [JsonProperty("hldg_qty")]
    public string Quantity { get; set; }      // 보유 수량

    [JsonProperty("pchr_avg_pric")]
    public string AveragePrice { get; set; }  // 매입 평단가

    [JsonProperty("evlu_pfls_rt")]
    public string ProfitRate { get; set; }    // 수익률
}
