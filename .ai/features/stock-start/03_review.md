# 03_review - stock-start

작성: Antigravity
일시: 2026-05-28

## 리뷰 대상
- **검토한 파일 목록**
  - `src/StockApp/StockApp.csproj`
  - `src/StockApp/App.xaml`
  - `src/StockApp/App.xaml.cs`
  - `src/StockApp/Models/FinanceItem.cs`
  - `src/StockApp/Services/IFinanceService.cs`
  - `src/StockApp/Services/NaverFinanceService.cs`
  - `src/StockApp/ViewModels/MainWindowViewModel.cs`
  - `src/StockApp/Views/MainWindow.xaml`
  - `src/StockApp/Views/MainWindow.xaml.cs`
  - `tests/StockApp.Tests/StockApp.Tests.csproj`
  - `tests/StockApp.Tests/NaverFinanceServiceTests.cs`
  - `tests/StockApp.Tests/MainWindowViewModelTests.cs`
  - `StockApp.sln`
- **base_commit**: 4b0624f779b0bab122e70d112c893354f1bbb2a8
- **review_target_commit**: 145ecbd215d0e7884c16a4e1e02990f8990c427a
- **diff_command**: `git diff 4b0624f779b0bab122e70d112c893354f1bbb2a8..145ecbd215d0e7884c16a4e1e02990f8990c427a`
- **diff_range**: 4b0624f779b0bab122e70d112c893354f1bbb2a8..145ecbd215d0e7884c16a4e1e02990f8990c427a

## 지적 사항 요약
- **BLOCKER**: 1개 (국제 금시세 API 오적용에 따른 원화/g당 국내 시세 스펙 불일치 및 변환 공식 결여)
- **MAJOR**: 1개 (ViewModel 비동기 컨텍스트에서 `.ConfigureAwait(false)` 오용에 따른 WPF UI 크래시 리스크)
- **MINOR**: 2개 (IFinanceService 인터페이스 내 JSON 파서 노출로 인한 관심사 분리 위반, OnTimerTick 예외 삼키기 및 로깅 부재)
- **NIT**: 1개 (IDisposable 구현 시 백그라운드 태스크 취소를 위한 CancellationTokenSource 연동 미비)

---

## 코드 품질

### [BLOCKER] 국제 금시세 API 오적용에 따른 국내 시세 스펙 위반
- **severity**: BLOCKER
- **지적 사항**: 국내 금시세(원화 기준 g당 가격) 스펙 요구사항 미준수 및 오작동.
- **해결 코드 위치**: 
  - [MainWindowViewModel.cs:L20](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs#L20) (`private const string GoldSymbol = "CMDT_GC";`)
  - [MainWindowViewModel.cs:L49-L50](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs#L49-L50) (`new() { DisplayName = "금시세", Symbol = GoldSymbol, Unit = "USD/oz" }`)
  - [NaverFinanceService.cs:L47](file:///C:/_SW/Agent-Orchestration/src/StockApp/Services/NaverFinanceService.cs#L47) (`const string goldCode = "CMDT_GC";`)
- **왜 문제인지**:
  - `00_spec.md` 23번 라인에 따르면 *"금시세, 기본 표시는 원화 기준 g당 가격이며 데이터가 국제 금시세만 제공되면 환율을 적용해 환산값을 표시한다."*라고 정의되어 있습니다.
  - 또한 `01_plan.md` 124번 라인에서는 *"네이버 국내 금시세 API인 `CMDT_GD`는 이미 원화(KRW) 기준 g당 가격을 완벽하게 가공하여 반환하므로..."*라고 계획되어 있었습니다.
  - 그러나 실제 구현을 검토한 결과, 국내 금시세(`CMDT_GD`) 대신 국제 금시세(`CMDT_GC`) API를 사용하여 데이터를 조회하고 있습니다. 이 API는 온스당 달러(`USD/oz`) 시세를 반환하므로, 사용자 화면에는 국내 금시세(원화 g당 가격, 예: 약 8~10만 원대)가 아닌 국제 금시세(예: 2,300달러대)가 무보정으로 그대로 노출됩니다. 
  - 환율과 g당 단위로 환산하는 보정 수식도 전혀 탑재되어 있지 않아, 원래의 스펙(국내 금시세 원화 g당 표시)을 명백하게 위반하는 기능 실패 수준의 BLOCKER 문제입니다.
- **어떻게 개선해야 하는지**:
  - 네이버 금융 모바일 API 엔드포인트는 국내 금시세(`CMDT_GD`)를 명확히 지원합니다.
  - 상수를 `CMDT_GD`로 전면 교체하고, 기본 단위를 `원` 또는 `KRW/g`로 지정하여 네이버 API가 반환하는 원화 g당 국내 금시세 원천 데이터가 바인딩되도록 수정을 지시해야 합니다.

### [MAJOR] ViewModel 비동기 메서드의 `.ConfigureAwait(false)` 오용으로 인한 WPF UI 크래시 리스크
- **severity**: MAJOR
- **지적 사항**: 뷰모델 레이어 내 과도한 `.ConfigureAwait(false)` 적용으로 인한 UI 컨텍스트 상실 및 크래시 유발.
- **해결 코드 위치**:
  - [MainWindowViewModel.cs:L52](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs#L52) (`RefreshCommand = new DelegateCommand(async () => await RefreshAsync().ConfigureAwait(false))...`)
  - [MainWindowViewModel.cs:L121](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs#L121) (`await Task.WhenAll(...).ConfigureAwait(false);`)
  - [MainWindowViewModel.cs:L141](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs#L141) (`var quote = await fetch().ConfigureAwait(false);`)
  - [MainWindowViewModel.cs:L207](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs#L207) (`await RefreshAsync().ConfigureAwait(false);`)
- **왜 문제인지**:
  - C# 비동기 설계 원칙(Best Practice)에 따르면, **데이터베이스/네트워크 등의 순수 비즈니스 라이브러리(Service/Repository) 단**에서는 성능 최적화 및 컨텍스트 스위칭 예방을 위해 `.ConfigureAwait(false)`를 적극 장려합니다.
  - 그러나 **UI와 맞닿아 있는 뷰모델(ViewModel) 단**에서는 비동기 연산 완료 후 WPF UI 요소의 조작과 데이터 바인딩 갱신이 안전하게 동작하도록 하기 위해 원래의 UI 스레드 동기화 컨텍스트(SynchronizationContext)를 보존(캡처)해야 하므로 `.ConfigureAwait(false)`를 사용해서는 안 됩니다.
  - 현재 뷰모델의 `RefreshAsync` 및 타이머 틱 핸들러 내부에서 이를 과도하게 호출하여, 비동기 처리가 끝난 이후에 백그라운드 스레드(스레드 풀 스레드) 상에서 UI 바인딩 대상 속성(`StatusMessage`, `LastSyncedAt`) 및 Prism의 `RefreshCommand.RaiseCanExecuteChanged()`를 강제로 발신하게 됩니다. 
  - 이는 디버그 환경에서 간헐적 크래시 또는 WPF 크로스 스레드 예외(`InvalidOperationException`, "이 스레드는 다른 스레드가 소유하고 있으므로 호출 스레드가 이 개체에 액세스할 수 없습니다.")를 일으켜 안정성을 심각하게 저해할 수 있습니다.
- **어떻게 개선해야 하는지**:
  - `MainWindowViewModel.cs` 내부의 모든 `.ConfigureAwait(false)` 호출을 전면 삭제하여 기본값(UI 스레드 컨텍스트 유지)으로 복원해야 합니다.
  - 순수 API 호출 계층인 `NaverFinanceService.cs` 내부의 `.ConfigureAwait(false)` 호출은 고성능 I/O 처리를 위해 그대로 유지합니다.

---

## 구조 및 가독성

### [MINOR] IFinanceService 인터페이스 내 JSON 파싱 메서드 강제 노출
- **severity**: MINOR
- **지적 사항**: 관심사 분리(Separation of Concerns) 아키텍처 원칙 위반.
- **해결 코드 위치**: [IFinanceService.cs:L24-L28](file:///C:/_SW/Agent-Orchestration/src/StockApp/Services/IFinanceService.cs#L24-L28)
- **왜 문제인지**:
  - `IFinanceService` 인터페이스는 금융 데이터를 비동기로 조회해 오는 상위 서비스 수준의 기능적 규약만 제공해야 합니다.
  - 그러나 JSON 원본 문자열을 인자로 받아 파싱 레코드를 반환하는 세부 가공 메서드(`ParseStockResponse`, `ParseIndexResponse`, `ParseMarketIndicatorResponse`)들이 인터페이스에 포함되어 외부로 불필요하게 노출되어 있습니다.
  - 이로 인해 인터페이스가 구체적인 JSON 데이터 파싱 포맷과 강하게 결합하게 되며, 다른 공급자 서비스(예: 다른 XML, gRPC, DB 기반 시세 서비스)를 추가할 때 아무 기능도 수행하지 않는 껍데기 파싱 메서드들을 억지로 구현해야 하는 불합리한 결합이 발생합니다.
- **어떻게 개선해야 하는지**:
  - 해당 파싱 메서드들을 `IFinanceService` 인터페이스 규약에서 제거합니다.
  - `NaverFinanceService` 구현체 내부에 `private` 또는 `internal` 헬퍼 메서드로 완전히 격리합니다. 단위 테스트를 위해 internal 메서드로 접근할 수 있도록 지정하거나, 혹은 좀 더 깔끔하게 JSON 파싱 책임만을 전담하는 별도의 `INaverFinanceParser` 등의 헬퍼를 두는 방안을 권장합니다.

### [MINOR] 타이머 틱 예외 먹기(Swallow Exception) 및 로깅 부재
- **severity**: MINOR
- **지적 사항**: 예외 정보 묵살 및 비식별 에러 추적 무력화.
- **해결 코드 위치**: [MainWindowViewModel.cs:L203-L213](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs#L203-L213)
- **왜 문제인지**:
  - `OnTimerTick` 콜백 내에서 `RefreshAsync` 실행 중 예외가 발생하면 빈 catch 블록으로 모든 에러를 조용히 묻어버리고 있습니다.
  - 개별 API 수준의 오류는 `SafeFetchAsync` 내에서 안전하게 격리되지만, 뷰모델 내부 스레딩 장애나 동기화 락 경합, 혹은 `RefreshAsync` 자체에서 발생하는 중명한 런타임 오류가 누적될 경우 개발 및 운영 시 전혀 원인을 추적할 수 없는 먹통 상태를 야기합니다.
- **어떻게 개선해야 하는지**:
  - 빈 catch 블록을 방지하고 `System.Diagnostics.Debug.WriteLine(ex)`과 같은 최소한의 개발용 로깅이나, Prism의 로깅 프레임워크 또는 이벤트를 통해 예외의 흔적을 기록하도록 보완해야 합니다.

---

## 계획 대비 구현 일치성
- **severity**: MINOR
- **01_plan.md 대비 일치/불일치 항목**:
  - **일치**: WPF 프로젝트 환경, .NET 9.0 구성, Prism.DryIoc 컨테이너 부트스트랩, HttpClient 싱글톤 등록, 개별 SafeFetch 비동기 격리 구조 등 핵심 아키텍처는 일치합니다.
  - **불일치**: `01_plan.md` 124번 라인에서 국내 금시세 API인 `CMDT_GD`를 명확히 활용해 가공 없이 원화/g으로 직접 바인딩하겠다고 선언하였으나, 실제 구현에서는 `CMDT_GC`(국제 금, 달러/온스)를 사용하여 단위와 환산 처리가 누락되는 스펙 불일치가 일어났습니다.
- **구체적 차이**:
  - 계획상으로는 국내 시세를 원화 기준 g당 가격으로 보여주는 것이 목표였으나, 코드 상에서는 `CMDT_GC`에 접근하여 `USD/oz` 단위의 국제 달러 표시 값을 UI에 뿌려줍니다.
- **이 차이가 문제인지, 허용 가능한지**:
  - 스펙 요구사항의 핵심적 가치를 침해하는 비정상 데이터 노출이므로 **허용 불가능**합니다. 다음 4단계 Fix에서 무조건 국내 금시세 `CMDT_GD`로 복구해야 합니다.

---

## 구현 의도 타당성
- **severity**: MINOR
- **02_dev.md에 적힌 판단에 대한 동의 또는 반론**:
  - **반론**: `02_dev.md` 44번 항목의 *"실제 네이버 모바일 API의 안정적 엔드포인트가 CMDT_GC(국제 금)인 점을 고려해 보수적으로 CMDT_GC를 기본값으로 사용했다"*라는 판단에 강력히 반론합니다.
  - **반론 근거**: 네이버 금융 모바일 시장지표 API 엔드포인트는 국내 금시세인 `CMDT_GD`도 완벽히 지원하며, 호출 안정성은 `CMDT_GC`와 동일합니다. 굳이 단위를 환율 환산도 거치지 않은 채 `USD/oz` 국제 단위로 스펙을 변경하여 사용자에게 무보정 상태로 제공하는 판단은 타당하지 않으며, 단지 엔드포인트 문자열 값 하나만 `CMDT_GD`로 수정하면 간단히 스펙을 준수할 수 있으므로 강제 보정이 필요합니다.

---

## 테스트
- **severity**: MINOR
- **누락된 테스트 케이스**:
  - 뷰모델 비동기 동시성 실행 시 스레드 흐름과 예외 차단 단위 테스트가 부족합니다.
  - `NaverFinanceServiceTests`에서 `ParseMarketIndicatorResponse` 테스트 시, 금시세 데이터 파싱에 대한 별도의 상세 테스트가 부재합니다.
- **각 케이스가 왜 필요한지**:
  - 금시세 응답 파싱 시 `CMDT_GD`의 원천 필드 명세와 정상 응답이 다소 다를 수 있으므로 이에 대한 파싱 안정성을 확인하기 위한 단위 테스트 케이스 보강이 요망됩니다.

---

## 04_fix 입력
- **must_fix**:
  - **[BLOCKER]** 금시세 조회 대상을 국제 금(`CMDT_GC`, USD/oz)에서 **국내 금(`CMDT_GD`, 원/g)**으로 전면 수정하고 뷰모델 기본 표기 단위를 `원`으로 복원.
  - **[MAJOR]** `MainWindowViewModel.cs` 내부의 비동기 호출 부에서 모든 `.ConfigureAwait(false)` 제거 (UI 컨텍스트 유지 보장).
- **should_consider**:
  - **[MINOR]** `IFinanceService` 인터페이스 정의에서 `Parse...` JSON 파서 전용 메서드 3개를 탈락시키고 `NaverFinanceService` 내부 헬퍼로 숨기거나 별도 파서 유틸로 분리.
  - **[MINOR]** `MainWindowViewModel.cs` 내 `OnTimerTick` 비정상 예외 처리에 대한 빈 catch 방지 및 디버그 로깅 보완.
- **optional**:
  - **[NIT]** `MainWindowViewModel` 소멸 시 백그라운드 비동기 작업을 제어할 `CancellationTokenSource` 취소 메커니즘을 `Dispose()`와 연동.

---

## 총평
- **전체적인 구현 품질 요약**:
  - Prism.DryIoc 9.0 부트스트랩, WPF 다크 테마 디자인 뷰 구성 및 `HttpClient` 싱글톤 등록, 개별 SafeFetch 예외 복원 태스크 병렬화 설계는 전반적으로 매우 탁월하고 높은 품질로 작성되었습니다.
  - 그러나 금시세 데이터 소스가 계획과 다르게 국제 시세로 치우치면서 변환 식조차 누락된 부분(BLOCKER)과 뷰모델에서의 불필요한 비동기 컨텍스트 제거(`.ConfigureAwait(false)` 오용에 따른 크래시 리스크, MAJOR)는 WPF 앱의 치명적인 안정성/스펙 정합성을 훼손하는 요소이므로 반드시 수정되어야 합니다.
  - 04_fix 단계를 거치면 완벽하고 안정적인 고품격 실시간 시세 보드 프로그램으로 정상 완성될 것으로 판단합니다.

## 단계 결과
- **status**: PASS
- **next_stage**: 04_fix
- **human_gate_required**: false
- **blocking_reason**: 없음
- **risk_level**: medium
- **produced_files**:
  - `.ai/features/stock-start/03_review.md`
- **changed_files**:
  - `.ai/features/stock-start/03_review.md`
- **commit_created**: false
- **commit_message**:
- **model_mismatch**: false
- **actual_model**: Antigravity
