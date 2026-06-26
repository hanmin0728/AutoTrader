using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class StockApiClient : MonoBehaviour
{
    // 실무에서는 성능과 커넥션 재사용을 위해 HttpClient를 싱글톤 또는 static으로 관리합니다.
    private static readonly HttpClient _httpClient = new HttpClient();

    // 모의투자용 주소 (실전투자는 도메인이 다릅니다)
    private readonly string _baseUrl = "https://openapivts.koreainvestment.com:29443";

    private string _appKey;
    private string _appSecret;
    private string _accountNo;
    private string _accessToken;


    private async void Start()
    {
        // 보안키 파일 로드
        LoadConfig();

        // 증권사 서버에 토큰 요청

        bool isAuthSuccess = await RequestAccessTokenAsync();
        //isAuthSuccess = true;
        if (isAuthSuccess)
        {
            Debug.Log($"[인증 성공] 토큰이 발급되었습니다.\nToken: {_accessToken.Substring(0, 10)}...");
            //eyJ0eXAiOi...
            await FetchAccountBalanceAsync();
            await Task.Delay(1000);
            await PlaceBuyOrderAsync("005930", 1);
        }
        else
        {
            Debug.LogError("[인증 실패] AppKey 또는 AppSecret을 다시 확인하세요.");
        }
    }

    /// <summary>
    /// StreamingAssets 폴더에서 보안 키 정보 불러오기
    /// </summary>
    private void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "secret_config.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeAnonymousType(json, new { AppKey = "", AppSecret = "", AccountNo = "" });

            _appKey = config.AppKey;
            _appSecret = config.AppSecret;
            _accountNo = config.AccountNo;
        }
        else
        {
            Debug.LogError("secret_config.json 파일을 찾을 수 없습니다! 경로를 확인하세요.");
        }
    }

    /// <summary>
    /// 한국투자증권 API를 호출하여 모의투자 계좌 잔고 불러오기
    /// </summary>
    private async Task FetchAccountBalanceAsync()
    {
        if (string.IsNullOrEmpty(_accessToken)) return;

        // 한국투자증권 모의투자 주식잔고조회 URL 명세

        #region url 상세 설명
        // 기본 주소

        // {_baseUrl}: 모의투자 서버 주소 (실전 투자로 전환시 이 도메인 주소 교체)
        // /uapi/domestic-stock/v1/trading/inquire-balance: 증권사 서버 내부의 수많은 방 중에서 "국내 주식(domestic-stock)의 거래(trading) 탭에 있는 잔고 조회(inquire-balance) 창구"로 가겠다는 절대 경로
        // ?: "이제부터 상세 주문서(옵션)를 작성하겠다"라는 선언문

        // 핵심 식별 파라미터

        // CANO={_accountNo} (종합계좌번호 앞 8자리 )
        // ACNT_PRDT_CD=01 (계좌번호 뒤에 붙는 2자리 구분 코드) 내가 만든 일반 주식 종합 위탁 계좌는 무조건 01

        // 조회 옵션 파라미터

        // INQR_DVSN=02 (조회 구분) 02는 '대출 전체' 혹은 '종목별 합산' 조회를 의미합니다. 만약 주식을 신용대출로도 사고 현금으로도 샀을 때, 이를 쪼개서 보지 않고 '총 X주'로 합쳐서 예쁘게 보기 위한 세팅
        // UNPR_DVSN=01 (단가 구분) 매입 단가를 계산하는 방식. 01은 '원장가' 기준으로, 내 계좌 원장에 기록된 진짜 내 평균 매입 가격(평단가)을 기준으로 조회
        // PRCS_DVSN=01 (처리 구분) 01은 '전일 매매 포함' 조회 오늘 주식을 사거나 판 것뿐만 아니라, 어제까지 거래가 완료되어 완전히 내 계좌에 정착된 주식 자산까지 전부 포함해서 정확한 잔고 계산

        // 필터링 및 예외 처리

        // AFHR_FLPR_YN = N(시간외단일가 과표여부) 정규 주식 시장이 끝난 후 열리는 '시간외 단일가 매매' 때 발생하는 세금 계산을 적용할 것인가에 대한 옵션. 이 자동 매매 프로그램은 정규장에 돌리므로 No로 설정
        // OFL_YN=N (오프라인 여부) 지점 창구에 직접 방문해서 상담원과 오프라인으로 거래한 내역만 따로 필터링해서 볼 것인가 묻는 옵션. API 프로그램으로만 거래할 것이니 No로 설정
        // FUND_STTL_ICLD_YN = N(펀드결제포함 여부) 계좌에 주식 말고 '펀드' 상품도 섞여 있을 때, 그 펀드의 정산 금액까지 잔고에 포함할지 묻는 옵션. 펀드 잔고는 노이즈 데이터일 뿐이므로 No로 설정
        // FNCG_AMT_AUTO_RDPT_YN = N(융자금자동상환여부) 증권사 돈을 빌려서 주식을 샀을 때(신용거래), 현금이 생기면 빚부터 자동으로 갚을지 결정하는 옵션. 프로그램 오작동으로 의도치 않게 대출 상환 로직이 꼬이는 것을 막기 위해 안전하게 No로 설정

        // 연속 조회 파라미터 (데이터가 너무 많을 때)
        // CTX_AREA_FK100= / CTX_AREA_NK100= (연속조회 검색조건/키)
        #endregion


        string url = $"{_baseUrl}/uapi/domestic-stock/v1/trading/inquire-balance?" +
             $"CANO={_accountNo}&ACNT_PRDT_CD=01&" +
             "AFHR_FLPR_YN=N&OFL_YN=N&INQR_DVSN=02&UNPR_DVSN=01&" +
             "FUND_STTL_ICLD_YN=N&FNCG_AMT_AUTO_RDPT_YN=N&PRCS_DVSN=01&" +
             "CTX_AREA_FK100=&CTX_AREA_NK100=";

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            // 증권사 보안 시스템이 요구하는 필수 Header 세팅
            request.Headers.Add("authorization", $"Bearer {_accessToken}");
            request.Headers.Add("appkey", _appKey);
            request.Headers.Add("appsecret", _appSecret);
            request.Headers.Add("tr_id", "VTTC8434R"); // 모의투자 '주식잔고조회' 전용 TR ID
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // 응답받은 대량의 JSON 데이터를 C# 객체 구조로 변환
                    var balanceData = JsonConvert.DeserializeObject<InquireBalanceResponse>(responseBody);

                    if (balanceData != null && balanceData.rt_cd == "0")
                    {
                        Debug.Log("<color=yellow>====== [모의계좌 잔고 조회 성공] ======</color>");

                        // 1. 예수금 및 총 자산 출력 
                        if (balanceData.AccountInfo != null && balanceData.AccountInfo.Length > 0)
                        {
                            var accountInfo = balanceData.AccountInfo[0];
                            Debug.Log($"[계좌 예수금]: {accountInfo.TotalCash}원 | [총 평가금액]: {accountInfo.TotalAsset}원");
                        }

                        // 2. 보유 종목 리스트 출력
                        if (balanceData.StockList != null && balanceData.StockList.Length > 0)
                        {
                            Debug.Log($"현재 보유 중인 주식 종목 수: {balanceData.StockList.Length}개");
                            foreach (var stock in balanceData.StockList)
                            {
                                // 증권사는 데이터가 비어있어도 빈 문자열을 주므로 종목코드가 채워져 있는지 확인
                                if (string.IsNullOrEmpty(stock.StockCode)) continue;

                                Debug.Log($"▶ [종목] {stock.StockName}({stock.StockCode}) | 수량: {stock.Quantity}주 | 평단가: {stock.AveragePrice}원 | 수익률: {stock.ProfitRate}%");
                            }
                        }
                        else
                        {
                            Debug.Log("현재 보유 중인 주식이 없습니다. (클린 계좌)");
                        }
                    }
                    else
                    {
                        Debug.LogError($"잔고 조회 실패 (증권사 에러): {balanceData?.msg1}");
                    }
                }
                else
                {
                    Debug.LogError($"HTTP 통신 실패: {response.StatusCode}\nDetails: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"잔고 조회 중 예외 발생: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 한국투자증권 OAuth2 API를 통해 Access Token 발급
    /// </summary>
    private async Task<bool> RequestAccessTokenAsync()
    {
        string url = $"{_baseUrl}/oauth2/tokenP";

        // 증권사 API 매뉴얼에 명세된 필수 데이터 구조
        var requestBody = new
        {
            grant_type = "client_credentials",
            appkey = _appKey,
            appsecret = _appSecret
        };

        string jsonPayload = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // 응답받은 JSON에서 access_token 필드만 추출
                var definition = new { access_token = "", token_type = "", expires_in = 0 };
                var result = JsonConvert.DeserializeAnonymousType(responseBody, definition);

                _accessToken = result.access_token;
                return !string.IsNullOrEmpty(_accessToken);
            }

            Debug.LogError($"서버 응답 에러: {response.StatusCode}\nDetails: {responseBody}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"네트워크 통신 예외 발생: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 주식 현금 매수 주문 (모의투자 전용)
    /// </summary>
    /// <param name="stockCode">종목코드 (예: 삼성전자 "005930")</param>
    /// <param name="quantity">주문 수량</param>
    private async Task PlaceBuyOrderAsync(string stockCode, int quantity)
    {
        // 매수 주문 API 주소
        string url = $"{_baseUrl}/uapi/domestic-stock/v1/trading/order-cash";

        // 서버에 보낼 주문서 작성 (JSON으로 변환될 객체)
        var requestBody = new
        {
            CANO = _accountNo,             // 종합계좌번호 앞 8자리
            ACNT_PRDT_CD = "01",           // 계좌상품코드 (주식은 01)
            PDNO = stockCode,              // 종목코드
            ORD_DVSN = "01",               // 주문구분 (01: 시장가 매수)
            ORD_QTY = quantity.ToString(), // 주문 수량
            ORD_UNPR = "0"                 // 주문 단가 (시장가이므로 0원으로 세팅해야 함)
        };

        // 주문서를 JSON 문자열로 밀봉(Serialize)
        string jsonPayload = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
        {
            // 증권사 보안 헤더 세팅 
            request.Headers.Add("authorization", $"Bearer {_accessToken}");
            request.Headers.Add("appkey", _appKey);
            request.Headers.Add("appsecret", _appSecret);
            request.Headers.Add("tr_id", "VTTC0802U"); // 모의투자 '매수' 전용 TR ID

            request.Content = content;

            try
            {
                // 서버로 전송
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                // 응답 결과에서 성공 코드(rt_cd)와 메시지(msg1)만 빠르게 파싱
                var result = JsonConvert.DeserializeAnonymousType(responseBody, new { rt_cd = "", msg1 = "" });

                if (result != null && result.rt_cd == "0")
                {
                    Debug.Log($"<color=green>====== [매수 주문 성공!] ======</color>\n종목: {stockCode} | 수량: {quantity}주 | 서버메시지: {result.msg1}");
                }
                else
                {
                    Debug.LogError($"[매수 주문 실패]: {result?.msg1}\n상세: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"매수 통신 중 예외 발생: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 주식 현금 매도 주문 (모의투자 전용)
    /// </summary>
    /// <param name="stockCode">종목코드 (예: 삼성전자 "005930")</param>
    /// <param name="quantity">매도 수량</param>
    private async Task PlaceSellOrderAsync(string stockCode, int quantity)
    {
        // 주문 API 주소 
        string url = $"{_baseUrl}/uapi/domestic-stock/v1/trading/order-cash";

        // 매도 주문서 작성
        var requestBody = new
        {
            CANO = _accountNo,             // 종합계좌번호 앞 8자리
            ACNT_PRDT_CD = "01",           // 계좌상품코드 (주식: 01)
            PDNO = stockCode,              // 종목코드
            ORD_DVSN = "01",               // 주문구분 (01: 시장가 매도)
            ORD_QTY = quantity.ToString(), // 매도 수량
            ORD_UNPR = "0"                 // 시장가 주문이므로 단가는 0원
        };

        string jsonPayload = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
        {
            //증권사 보안 헤더 세팅
            request.Headers.Add("authorization", $"Bearer {_accessToken}");
            request.Headers.Add("appkey", _appKey);
            request.Headers.Add("appsecret", _appSecret);
            request.Headers.Add("tr_id", "VTTC0801U"); // 모의투자 '매수' 전용 TR ID

            request.Content = content;

            try
            {
                // 서버로 전송
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeAnonymousType(responseBody, new { rt_cd = "", msg1 = "" });

                if (result != null && result.rt_cd == "0")
                {
                    Debug.Log($"<color=blue>====== [매도 주문 성공!] ======</color>\n종목: {stockCode} | 수량: {quantity}주 | 서버메시지: {result.msg1}");
                }
                else
                {
                    Debug.LogError($"[매도 주문 실패]: {result?.msg1}\n상세: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"매도 통신 중 예외 발생: {ex.Message}");
            }
        }
    }
}