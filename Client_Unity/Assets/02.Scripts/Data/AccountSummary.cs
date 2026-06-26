using Newtonsoft.Json;

// 내 계좌 종합 요약 정보 (보유 현금 및 총 자산)
public class AccountSummary 
{
    [JsonProperty("dnca_tot_amt")]
    public string TotalCash { get; set; }   // 예수금 총액 

    [JsonProperty("tot_evlu_amt")]
    public string TotalAsset { get; set; }  // 총 평가 금액
}
