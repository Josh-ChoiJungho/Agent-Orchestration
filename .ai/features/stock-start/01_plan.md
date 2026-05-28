# 01_plan - stock-start

작성: Antigravity
일시: 2026-05-28

## 기능 목표
- WPF 데스크톱 애플리케이션에서 코스피 지수, SK하이닉스 보통주, 삼성전자 보통주 주가를 주기적으로 조회하여 화면 상단에 카드 형태로 보여줍니다.
- 그 아래 영역에 USD/KRW 환율과 국내 금 시세(KRW/g)를 실시간에 준하는 주기로 갱신하여 표시합니다.
- C# .NET 9.0 환경에서 MVVM 패턴과 Prism.DryIoc 프레임워크를 적용하여 UI 비주얼, 상태 프레젠테이션, 외부 데이터 조회 비즈니스 로직을 조화롭게 격리합니다.

## 구현 접근 방식
- **경량 네이버 모바일 JSON API 사용**: 기존의 무겁고 불안정한 HTML 스크래핑(HtmlAgilityPack 등) 대신, 네이버 금융 모바일 페이지에서 사용하는 비공식 JSON API 엔드포인트를 호출합니다. 이를 통해 매우 적은 네트워크 리소스와 연산 비용으로 정확하고 정형화된 JSON 데이터를 가져옵니다.
- **Prism.DryIoc 활용 DI 설계**: 데이터 조회 서비스(`IFinanceService`)를 컨테이너에 등록하고 `MainWindowViewModel` 생성자를 통해 주입받아 모듈 간 결합도를 낮추고 테스트 가능성을 극대화합니다.
- **예외 복원력 확보**: 5개 조회 항목(KOSPI, 삼성전자, SK하이닉스, 환율, 금시세)을 개별 Task로 비동기 실행(`Task.WhenAll`)하되, 각각 `try-catch`로 감싸 특정 항목 조회 실패 시에도 전체 앱이 멈추거나 중단되지 않고 개별 항목만 오류 상태(`FinanceStatus.Error`) 및 에러 메시지를 표시하도록 디자인합니다.

## 검토한 대안
- **대안 1: BeautifulSoup / HtmlAgilityPack 기반 HTML 크롤링**
  - **장점**: 브라우저가 보는 웹 화면의 모든 정보를 수집 가능.
  - **단점**: 파싱 연산 오버헤드가 크고, 네이버 금융의 웹 레이아웃이 미세하게 변경되어도 파싱 에러(NRE 등)가 발생해 유지보수 리스크가 극도로 높음.
  - **채택하지 않은 이유**: 유지보수 편의성과 성능 측면에서 경량 JSON API를 사용하는 최종 안이 압도적으로 유리함.
- **대안 2: 공공데이터포털 또는 금융감독원 Open API 사용**
  - **장점**: 공식적이고 합법적이며 차단 우려가 없음.
  - **단점**: 실시간 데이터가 아니며(대부분 전일자 또는 지연 데이터), API 신청 및 승인 프로세스가 요구되고 C# 코드 내에 관리해야 할 비밀키가 늘어남.
  - **채택하지 않은 이유**: 사용자가 요구한 실시간성(5초 간격 준실시간) 요구사항을 충족하지 못함.
- **대안 3: 야후 파이낸스(Yahoo Finance) 라이브러리 사용**
  - **장점**: 해외에서 널리 쓰이며 안정적인 오픈소스 패키지가 존재함.
  - **단점**: 한국 시장 종목코드 형식(`.KS`)을 맞춰야 하고 국내 금시세(원화 기준 g당 가격)나 USD/KRW 환율의 로컬 실시간 정보 반영 속도가 한국 포털 대비 지연될 수 있음.
  - **채택하지 않은 이유**: 한국 대표 포털인 네이버의 데이터 소스가 국내 주식, 국내 환율, 국내 금시세를 원화 기준으로 가장 빠르고 정확하게 가져올 수 있음.

## 변경 파일 계획
- **src/StockApp/StockApp.csproj (신규)**: .NET 9.0 Windows WPF 프로젝트 설정 및 `Prism.DryIoc` (버전 9.0.537 또는 안정 버전) NuGet 패키지 의존성을 정의합니다.
- **src/StockApp/App.xaml (신규)**: Prism Application 클래스를 리소스로 등록합니다.
- **src/StockApp/App.xaml.cs (신규)**: `PrismApplication`을 상속받아 `CreateShell()`을 통한 메인 윈도우 생성 및 `RegisterTypes()`에서 `IFinanceService`와 `NaverFinanceService`를 IoC 컨테이너에 등록합니다.
- **src/StockApp/Models/FinanceItem.cs (신규)**: 개별 시세 항목 정보를 속성(이름, 코드, 현재가/지수값, 전일대비값, 전일대비율, 갱신시각, 조회상태 등)으로 가지는 데이터 모델입니다.
- **src/StockApp/Services/IFinanceService.cs (신규)**: KOSPI 지수, 주식 시세, 환율, 금시세를 비동기로 조회하는 서비스 인터페이스를 정의합니다.
- **src/StockApp/Services/NaverFinanceService.cs (신규)**: `HttpClient`를 주입받아 비공식 네이버 모바일 API를 비동기 호출하고 `System.Text.Json`으로 매핑하여 `FinanceItem` 형태로 가공하는 실 구현체입니다.
- **src/StockApp/ViewModels/MainWindowViewModel.cs (신규)**: Prism `BindableBase`를 상속하며, 5초 주기의 갱신 타이머 작동, 수동 새로고침 `DelegateCommand` 구현, 개별 시세 데이터를 바인딩 속성으로 관리합니다.
- **src/StockApp/Views/MainWindow.xaml (신규)**: Grid 레이아웃을 이용하여 상단 주식/지수 영역(3개 카드), 하단 시장지표 영역(환율, 금시세)을 고품격 모던 다크 테마 디자인으로 배치하고 데이터 바인딩을 연동합니다.
- **src/StockApp/Views/MainWindow.xaml.cs (신규)**: Prism 바인딩을 위한 단순 비하인드 코드입니다.
- **tests/StockApp.Tests/StockApp.Tests.csproj (신규)**: xUnit 및 Moq 기반의 단위 테스트 프로젝트 파일입니다.
- **tests/StockApp.Tests/NaverFinanceServiceTests.cs (신규)**: 모의 HttpClient 데이터를 이용해 Naver API 응답 JSON이 `FinanceItem` 객체로 오차 없이 파싱되는지 검증합니다.
- **tests/StockApp.Tests/MainWindowViewModelTests.cs (신규)**: 뷰모델 기동 시의 타이머 바인딩 갱신 흐름 및 새로고침 커맨드가 동작할 때 시세 모델의 프레젠테이션 값 변화를 단위 테스트합니다.

## 데이터 / 제어 흐름
### 데이터 흐름 (Data Flow)
```text
[Naver Stock JSON API] (KOSPI, 005930, 000660, USD/KRW, 금시세)
         │
         ▼ (HTTP GET JSON 응답, User-Agent 헤더 포함)
[NaverFinanceService] (System.Text.Json 비동기 역직렬화 및 가공)
         │
         ▼ (FinanceItem 객체 반환)
[MainWindowViewModel] (ObservableCollection 또는 개별 속성 갱신)
         │
         ▼ (WPF 데이터 바인딩 시스템)
[MainWindow (UI View)] (다크 테마 디자인 요소에 실시간 렌더링)
```

### 제어 흐름 (Control Flow)
```text
App 기동 ──> App.xaml.cs (Prism 부트스트랩)
                 │
                 ├──> RegisterTypes() (NaverFinanceService 서비스 컨테이너 등록)
                 └──> CreateShell() (MainWindow 윈도우 인스턴스화)
                          │
                          ▼ (Prism Auto-Wire ViewModel에 의해 의존성 자동 주입)
                      MainWindowViewModel 생성자
                          │
                          ├──> 비동기 초기 시세조회 (Task.WhenAll)
                          └──> 5초 주기 DispatcherTimer 기동
                                   │
                                   ├──> [타이머 틱 / 수동 갱신 커맨드 트리거]
                                   └──> 백그라운드 스레드에서 API 호출 후 UI 데이터 갱신
```

## 구현 단계 분할
1. **1단계: 인프라 스트럭처 및 솔루션 세팅**
   - **대상 파일**: `src/StockApp/StockApp.csproj`, `src/StockApp/App.xaml`, `src/StockApp/App.xaml.cs`
   - **완료 기준**: Prism.DryIoc 패키지가 포함된 WPF 프로젝트가 에러 없이 빌드되고 기본 윈도우가 실행됨.
2. **2단계: 데이터 모델 및 데이터 서비스 구현**
   - **대상 파일**: `src/StockApp/Models/FinanceItem.cs`, `src/StockApp/Services/IFinanceService.cs`, `src/StockApp/Services/NaverFinanceService.cs`
   - **완료 기준**: 네이버 API의 JSON 엔드포인트 5개(`KOSPI`, `005930`, `000660`, `FX_USDKRW`, `CMDT_GD`)에 비동기 접근하여 정확한 금액, 변동폭, 변동률 데이터를 가공 및 추출 완료함.
3. **3단계: 뷰 모델 및 비즈니스 갱신 로직 구현**
   - **대상 파일**: `src/StockApp/ViewModels/MainWindowViewModel.cs`
   - **완료 기준**: 뷰모델 기동 시 5개 항목이 정상 로드되고, 5초마다 타이머가 작동하며, 새로고침 커맨드가 동기/비동기 예외 없이 원활하게 구동됨.
4. **4단계: 모던 다크 테마 UI 뷰 작성 및 데이터 바인딩**
   - **대상 파일**: `src/StockApp/Views/MainWindow.xaml`, `src/StockApp/Views/MainWindow.xaml.cs`
   - **완료 기준**: 아름다운 다크 테마 디자인 바탕으로 상승 시 빨간색(또는 네온 코랄), 하락 시 파란색(또는 네온 블루) 테마가 실시간으로 반영되고 최종 갱신시각 및 로딩/에러 상태가 세련되게 표현됨.
5. **5단계: 테스트 케이스 작성 및 최종 검증**
   - **대상 파일**: `tests/StockApp.Tests/NaverFinanceServiceTests.cs`, `tests/StockApp.Tests/MainWindowViewModelTests.cs`
   - **완료 기준**: API 목업(Mock) 데이터 파싱 단위 테스트와 뷰모델 타이머 틱 비즈니스 테스트 100% 통과.

## 위험 구간
- **위험 항목 1: 네이버의 비공식 API 차단(403 Forbidden) 리스크**
  - **완화 방안**: HttpClient 요청 헤더에 브라우저와 동일한 `User-Agent` 및 `Referer` 헤더(`https://m.stock.naver.com`)를 의무적으로 추가하여 자동화 봇 차단 정책을 우회합니다.
- **위험 항목 2: 빈번한 네트워크 조회 실패 및 지연**
  - **완화 방안**: 각 조회 태스크를 독립적으로 예외 처리하여, 특정 API가 응답 불능이어도 나머지 요소는 성공적으로 시각화하고, 실패 항목은 '조회 실패(Error)' 상태로 UI를 전환하여 복원력을 높입니다.
- **위험 항목 3: 5초 반복 갱신에 따른 리소스 누수(Memory Leak)**
  - **완화 방안**: HttpClient 인스턴스는 싱글톤 패턴으로 단 하나만 생성하여 재사용하고, 타이머가 트리거될 때마다 불필요한 가비지가 생성되지 않도록 가볍게 설계합니다.

## 새 의존성
- **Prism.DryIoc (버전 9.0.537)**: WPF 애플리케이션의 MVVM 구조화, Command 바인딩 및 DI 컨테이너 관리를 위해 사용합니다. (WPF 개발 표준)
- **Microsoft.Xaml.Behaviors.Wpf (버전 1.1.135)**: Prism이 UI 이벤트를 ViewModel Command로 트리거할 때 필요한 표준 의존성입니다.

## 테스트 전략
- **검증 케이스**:
  1. `NaverFinanceService`가 KOSPI API 호출 시 정상 응답 JSON 구조를 올바르게 파싱하는가 (Unit Test)
  2. 주식 종목코드 오기입 등으로 인한 404/500 에러 시, `FinanceItem` 객체의 `Status`가 `Error`로 세팅되고 예외가 던져지지 않는가 (Unit Test)
  3. `MainWindowViewModel`이 수동 새로고침 실행 시, 타이머 간격 초기화 및 즉시 조회 연동이 매끄럽게 호출되는가 (Unit Test)
  4. 로컬 인터넷 해제 시, 앱 전체가 죽지 않고 모든 카드가 조회 실패 상태를 나타내는가 (Manual E2E)

## 롤백 / 복구 방향
- WPF 신규 프로젝트 및 테스트 폴더 구성 중 심각한 빌드 장애나 라이브러리 충돌이 발생하는 경우, `git checkout src/` 및 `git clean -fd`를 호출해 새로 추가된 신규 폴더/파일을 안전하게 날리고 이전 스펙 기준점으로 복구합니다.
- 데이터베이스나 비가역적인 파일 마이그레이션이 없으므로, 로컬 작업 트리 복구만으로 완전 롤백이 가능합니다.

## 실행 승인
- risk_level: medium
- human_gate_required: false
- human_gate_reason: 모호한 결정은 모두 하네스 권장 기본 정책과 안정한 비공식 JSON API 구조를 통해 결정되었으며 비가역적인 위험성이 전혀 없어 즉시 2단계 구현으로 PASS 가능합니다.
- approval_required_before_develop: false

## 스펙 모호점 처리
- **코스피 종목코드 처리**: KOSPI는 일반 주식 종목이 아니라 시장 지수이므로 종목코드 대신 `KOSPI` 인덱스 기호로 식별하여 `https://api.stock.naver.com/index/KOSPI/basic`을 통해 조회합니다.
- **금시세 환산 여부**: 네이버 국내 금시세 API인 `CMDT_GD`는 이미 원화(KRW) 기준 g당 가격을 완벽하게 가공하여 반환하므로, 코드 상에서 추가적인 USD-KRW 곱셈 수식을 통한 수동 환산 연산은 필요치 않으며 원천 데이터를 신뢰하여 그대로 바인딩합니다.
- **실시간 주기의 정의**: 무료 API 호출 빈도의 안정성을 고려해 기본 5초 주기를 유지하되 백그라운드 비동기 처리를 준수합니다.

## Git 기준점
- base_commit: 4b0624f779b0bab122e70d112c893354f1bbb2a8
- diff_base_command: git diff 4b0624f779b0bab122e70d112c893354f1bbb2a8

## 사용자 확인 사항
- 질문과 사용자 답변 기록: defaults_mode 가 true로 강제되었으므로, 모든 설계 의사결정은 권장 모범 설계와 안전한 디폴트(Naver API, Prism.DryIoc 구조)를 따라 PASS 확정했습니다.

## 단계 결과
- status: PASS
- next_stage: 02_develop
- human_gate_required: false
- blocking_reason: 없음
- risk_level: medium
- produced_files:
  - .ai/features/stock-start/01_plan.md
  - .ai/features/stock-start/01_plan.result.json
- changed_files:
  - .ai/features/stock-start/01_plan.md
  - .ai/features/stock-start/01_plan.result.json
- commit_created: false
- commit_message:
- model_mismatch: false
- actual_model: Antigravity
