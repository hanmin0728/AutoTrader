using Newtonsoft.Json;

// 증권사 전체 응답 데이터
public class InquireBalanceResponse 
{
    public string rt_cd { get; set; }
    public string msg1 { get; set; }

    [JsonProperty("output1")]
    public StockItem[] StockList { get; set; }

    [JsonProperty("output2")]
    public AccountSummary[] AccountInfo { get; set; }

    //증권사 서버가 "output1"이라는 이름표의 정보를 보냄
    //Newtonsoft.Json이 C# 코드에서 이름표와 똑같은 변수명을 찾음
    //코드에 StockList라고만 적혀있다면 이름표와 다르다고 데이터를 버려버림 
    //그래서[JsonProperty("output1")] 이라는 안내문을 달아주면, 번역기가 알아듣고 StockList에 데이터를 넣는다
}
