# SlogEngine

파일 기반 저장소를 사용하는 Markdown 블로그 관리 시스템입니다.

## 프로젝트 구조

- **SlogEngine.Server**: ASP.NET Core Web API 서버
- **SlogEngine.WebAssembly**: Blazor WebAssembly 클라이언트 애플리케이션
- **SlogEngine.Migrator**: 데이터 마이그레이션 도구

## 기술 스택

### 서버 (.NET 10.0)
- ASP.NET Core Web API
- YamlDotNet (YAML 파싱)
- Microsoft.AspNetCore.OpenApi (API 문서화)

### 클라이언트 (.NET 10.0)
- Blazor WebAssembly
- Markdig (Markdown 파싱)
- Microsoft.AspNetCore.Components.WebAssembly

## 주요 기능

### 블로그 관리
- 블로그 포스트 CRUD 작업 (생성, 읽기, 수정, 삭제)
- Markdown 형식의 포스트 작성 및 편집
- YAML Front Matter를 통한 메타데이터 관리
- 페이징을 통한 포스트 목록 조회
- 검색 및 태그 필터링 기능

### 포스트 편집
- Markdown 텍스트 편집기
- 클립보드 이미지 붙여넣기 지원
- 이미지 업로드 및 자동 마크다운 변환
- 태그 관리 시스템

### 블로그 메타데이터
- 블로그 제목 설정 및 관리
- 사용자별 블로그 설정 저장

### 이미지 관리
- 이미지 파일 업로드 및 저장
- 정적 파일 서빙
- 다양한 이미지 포맷 지원 (PNG, JPG, JPEG, GIF, WebP)

### 사용자 인터페이스
- 반응형 웹 디자인
- 다크/라이트 테마 지원
- 모바일 친화적 인터페이스
- 실시간 페이지 제목 업데이트

## API 엔드포인트

### 블로그 포스트
- `GET /blog/{username}` - 포스트 목록 조회
- `GET /blog/{username}/paged` - 페이징된 포스트 목록 조회
- `GET /blog/{username}/{postId}` - 개별 포스트 조회
- `POST /blog/{username}` - 새 포스트 작성
- `PUT /blog/{username}/{postId}` - 포스트 수정
- `DELETE /blog/{username}/{postId}` - 포스트 삭제

### 블로그 메타데이터
- `GET /blog/{username}/meta` - 블로그 메타데이터 조회
- `PUT /blog/{username}/meta` - 블로그 메타데이터 수정

### 이미지 업로드
- `POST /blog/{username}/images/upload` - 이미지 파일 업로드

### 기타
- `GET /weatherforecast` - 날씨 예보 (샘플 API)
- `GET /ping` - 서버 상태 확인

## 시작하기

### 전제 조건
- .NET 10.0 SDK 이상
- Visual Studio 2022 또는 VS Code

### 빌드 및 실행

#### 서버 실행
```bash
dotnet build SlogEngine.Server/SlogEngine.Server.csproj
dotnet run --project SlogEngine.Server
```

#### 클라이언트 실행
```bash
dotnet build SlogEngine.WebAssembly/SlogEngine.WebAssembly.csproj
dotnet run --project SlogEngine.WebAssembly
```

### 설정

#### 서버 설정
- 기본 포트: 5023
- CORS 정책: 모든 오리진 허용 (개발용)
- 정적 파일 경로: `wwwroot/blogs`

#### 클라이언트 설정
API 베이스 URL은 `wwwroot/appsettings.json`에서 설정:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5023"
  }
}
```

## 파일 구조

### 서버
```
SlogEngine.Server/
├── Controllers/
│   └── BlogController.cs
├── Services/
│   ├── MarkdownBlogService.cs
│   └── WeatherService.cs
├── Interfaces/
│   ├── IBlogService.cs
│   └── IWeatherService.cs
├── Models/
│   ├── BlogPost.cs
│   ├── BlogMeta.cs
│   └── PagedRequest.cs
└── wwwroot/blogs/
    └── {username}/
        ├── meta.json
        └── posts/
            ├── {postId}.md
            └── {postId}/
                └── {images}
```

### 클라이언트
```
SlogEngine.WebAssembly/
├── Pages/
│   ├── Blog.razor
│   ├── BlogDetail.razor
│   ├── BlogList.razor
│   └── Home.razor
├── Components/
│   ├── EditPostModal.razor
│   ├── ImageGallery.razor
│   └── Pagination.razor
├── Layout/
│   ├── BlogLayout.razor
│   └── MainLayout.razor
├── Services/
│   ├── BlogApiService.cs
│   ├── ClipboardService.cs
│   └── MarkdownService.cs
└── Models/
    ├── BlogPost.cs
    ├── BlogMeta.cs
    └── PagedResult.cs
```

## 개발 정책

프로젝트는 다음 개발 정책을 따릅니다:

### 객체 결합 정책
- 인터페이스 또는 추상 클래스를 통한 객체 결합
- 정적 클래스나 sealed 클래스 예외
- SOLID 원칙 준수

### 컬렉션 인터페이스 정책
- 구체적인 컬렉션 클래스 대신 인터페이스 사용
- `IReadOnlyList<T>`, `IDictionary<TKey, TValue>` 등 선호

### 문서화 정책
- 모든 public 메서드에 XML 문서 주석 필수
- 한국어로 작성된 명확하고 간결한 설명

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다.
