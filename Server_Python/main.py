from fastapi import FastAPI #서버 도구 가져오기
import uvicorn  #서버 엔진 가져오기

app = FastAPI() #서버 객체 생성

# 테스트용 테마 데이터
theme_data = {
    "themes": [
        {
            "id": "T01",
            "name": "반도체",
            "stocks": [{"code": "005930", "name": "삼성전자"}, {"code": "042700", "name": "한미반도체"}]
        }
    ]
}

@app.get("/api/themes") #서버주소
def get_themes():
    return theme_data #요청이 오면 데이터 반환

if __name__ == "__main__": # 서버 실행
    uvicorn.run(app, host="127.0.0.1", port=8000)