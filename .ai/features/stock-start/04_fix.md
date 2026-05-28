# 04_fix - stock-start

작성: Antigravity
일시: 2026-05-28

## 입력으로 처리한 지적
- **03_review.md must_fix**:
  - `[BLOCKER] 국제 금시세 API 오적용에 따른 국내 시세 스펙 위반`: 금시세 조회 대상을 국제 금(`CMDT_GC`, USD/oz)에서 **국내 금(`CMDT_GD`, 원/g)**으로 전면 수정하고 뷰모델 기본 표기 단위를 `원`으로 복원.
  - `[MAJOR] ViewModel 비동기 메서드의 .ConfigureAwait(false) 오용으로 인한 WPF UI 크래시 리스크`: `MainWindowViewModel.cs` 내부의 비동기 호출 부에서 모든 `.ConfigureAwait(false)` 제거 (UI 컨텍스트 유지 보장).
- **03_review.md should_consider**:
  - `[MINOR] IFinanceService 인터페이스 내 JSON 파싱 메서드 강제 노출`: `IFinanceService` 인터페이스 정의에서 `Parse...` JSON 파서 전용 메서드 3개를 탈락시키고 `NaverFinanceService` 내부 `internal` 헬퍼로 변경하여 캡슐화 개선.
  - `[MINOR] 타이머 틱 예외 먹기(Swallow Exception) 및 로깅 부재`: `MainWindowViewModel.cs` 내 `OnTimerTick` 비정상 예외 처리에 대한 빈 catch 방지 및 디버그 로깅 보완.
- **03_review.md optional**:
  - `[NIT] IDisposable 구현 시 백그라운드 태스크 취소를 위한 CancellationTokenSource 연동 미비`: `MainWindowViewModel` 소멸 시 백그라운기 비동기 작업을 제어할 `CancellationTokenSource` 취소 메커니즘을 `Dispose()`와 연동.
- **05_verify.md 실패 항목**: 없음
- **05_verify.md가 추가한 테스트 파일**: 없음

## 수용한 항목
- **출처**: 03_review
- **severity**: BLOCKER, MAJOR, MINOR, NIT
- **지적 내용 요약**:
  1. 국내 금시세 대신 국제 금시세를 사용하여 원화 g당 시세 표출 요구사항을 위반함.
  2. 뷰모델 내부에서 `.ConfigureAwait(false)`를 과도하게 적용하여 WPF의 UI 동기화 컨텍스트가 손실되는 잠재적 크래시 리스크를 초래함.
  3. 서비스 인터페이스 내에 JSON 파서 로직이 강제 노출되어 결합도가 높음.
  4. 타이머 콜백 예외가 추적 없이 비어있어 로깅 보강이 필요함.
  5. 뷰모델 소멸 시 비동기 백그라운드 테스크를 효과적으로 취소할 수 있는 `CancellationTokenSource`가 연동되지 않음.
- **수정한 파일과 변경 내용**:
  - [IFinanceService.cs](file:///C:/_SW/Agent-Orchestration/src/StockApp/Services/IFinanceService.cs): `Parse...` 응답 가공용 메서드 정의 3개 제거
  - [NaverFinanceService.cs](file:///C:/_SW/Agent-Orchestration/src/StockApp/Services/NaverFinanceService.cs): 금시세 심볼을 `CMDT_GD`로 교체, `Parse...` 메서드 한정자를 `public`에서 `internal`로 변경
  - [MainWindowViewModel.cs](file:///C:/_SW/Agent-Orchestration/src/StockApp/ViewModels/MainWindowViewModel.cs): `GoldSymbol` 및 단위 `원` 교체, 뷰모델 내부 모든 `.ConfigureAwait(false)` 제거, `CancellationTokenSource` 선언 및 `RefreshAsync`에 전달, `OnTimerTick` 내 예외 로깅 추가, `Dispose` 시 `_cts.Cancel()` 호출 연동
  - [NaverFinanceServiceTests.cs](file:///C:/_SW/Agent-Orchestration/tests/StockApp.Tests/NaverFinanceServiceTests.cs): `CMDT_GD` 전용 파싱 테스트 케이스인 `ParseMarketIndicatorResponse_GoldGD_ReadsCloseValueAndCompareToPreviousPrice` 추가
  - [MainWindowViewModelTests.cs](file:///C:/_SW/Agent-Orchestration/tests/StockApp.Tests/MainWindowViewModelTests.cs): 모킹 데이터를 `CMDT_GD` 및 단위 `원`으로 업데이트하고, `Dispose` 기능 및 다중 Dispose 안전성 검증을 위한 `Dispose_StopsTimerAndCancelsCancellationTokenSource` 추가
- **왜 수용했는가**:
  - 지적이 전적으로 명확하며, WPF MVVM 아키텍처 및 .NET 비동기 Best Practice를 충족하여 프로덕션 애플리케이션의 안정성과 스펙 정합성을 완벽하게 끌어올리기 위해 수용하였습니다.

## 거부한 항목
- 없음

## 보류한 항목
- 없음

## 사용자 판단 요청 항목
- 없음

## 추가 변경 사항
- 없음

## 변경 파일 목록
- `src/StockApp/Services/IFinanceService.cs`: 파싱 인터페이스 탈락
- `src/StockApp/Services/NaverFinanceService.cs`: 국내 금시세 엔드포인트 연동 및 파싱 메서드 internal 변경
- `src/StockApp/ViewModels/MainWindowViewModel.cs`: UI 컨텍스트 보존(ConfigureAwait 제거), 타이머 예외 로깅 보강, CancellationTokenSource Dispose 연동
- `tests/StockApp.Tests/NaverFinanceServiceTests.cs`: 국내 금시세(CMDT_GD)에 최적화된 단위 파싱 상세 단위 테스트 추가
- `tests/StockApp.Tests/MainWindowViewModelTests.cs`: CMDT_GD 규격 Mock 반영 및 Dispose 취소 테스트 추가

## 테스트
- **실행한 테스트 명령**: `dotnet test`
- **결과**: 통과 (실패: 0, 통과: 12, 건너뜀: 0, 전체: 12)
- **추가한 테스트**:
  - `NaverFinanceServiceTests.ParseMarketIndicatorResponse_GoldGD_ReadsCloseValueAndCompareToPreviousPrice`
  - `MainWindowViewModelTests.Dispose_StopsTimerAndCancelsCancellationTokenSource`

## Git 정보
- **fix_base_commit**: 145ecbd215d0e7884c16a4e1e02990f8990c427a
- **harness_commit_required**: true
- **commit_created_by_model**: false
- **commit_mode_suggestion**: create
- **commit_message_suggestion**: stock-start[20260528-115200][04_fix]
- **no_code_changes**: false
- **no_code_changes_reason**:
- **pre_commit_diff_command**: `git diff 145ecbd215d0e7884c16a4e1e02990f8990c427a`
- **changed_files**:
  - `src/StockApp/Services/IFinanceService.cs`
  - `src/StockApp/Services/NaverFinanceService.cs`
  - `src/StockApp/ViewModels/MainWindowViewModel.cs`
  - `tests/StockApp.Tests/NaverFinanceServiceTests.cs`
  - `tests/StockApp.Tests/MainWindowViewModelTests.cs`
- **harness_commit_blocking_reason**:

## 단계 결과
- **status**: PASS
- **next_stage**: 05_verify
- **human_gate_required**: false
- **blocking_reason**: 없음
- **risk_level**: low
- **produced_files**:
  - `.ai/features/stock-start/04_fix.md`
  - `.ai/features/stock-start/04_fix.result.json`
- **changed_files**:
  - `src/StockApp/Services/IFinanceService.cs`
  - `src/StockApp/Services/NaverFinanceService.cs`
  - `src/StockApp/ViewModels/MainWindowViewModel.cs`
  - `tests/StockApp.Tests/NaverFinanceServiceTests.cs`
  - `tests/StockApp.Tests/MainWindowViewModelTests.cs`
- **harness_commit_required**: true
- **commit_created_by_model**: false
- **commit_mode_suggestion**: create
- **commit_message_suggestion**: stock-start[20260528-115200][04_fix]
- **test_commands**:
  - `dotnet test`
- **model_mismatch**: false
- **actual_model**: Antigravity
