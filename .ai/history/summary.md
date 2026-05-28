# 프로젝트 히스토리

로컬 하네스가 자동 생성한 장기 기록 요약입니다.

- 기록된 run: 1
- 열린 리스크 / 미래 개선점: 15
- 열린/보류 미해결 항목: 14

## 최근 완료 Run

- 2026-05-28T12:06:39 stock-start (full, complete, verify=PASS)
  - WPF, MVVM, Prism 기반 시세 표시 앱의 개발 가능 스펙을 확정했다.
  - 코스피, SK하이닉스, 삼성전자, USD/KRW 환율, KRW/g 금시세 표시 범위를 정의했다.
  - 조회 주기, 오류 처리, 데이터 공급자 추상화, 제외 범위를 명시했다.

## 열린 리스크와 후속 개선점

- [medium] 무료 공개 시세 데이터 소스는 응답 형식 변경, 지연, 요청 제한 리스크가 있다. (from stock-start)
- [medium] 단기 주기적 폴링에 따른 속도 제한 리스크가 있으며 이를 위해 5초 제한 간격과 지수 백오프 재시도 전략을 도입한다. (from stock-start)
- [medium] 네이버 모바일 비공식 API의 구조적 차단(403 Forbidden) 리스크가 있으나 User-Agent와 Referer 헤더 지정으로 대처한다. (from stock-start)
- [medium] WPF UI 렌더링과 실제 5초 주기 갱신은 자동 UI 테스트로 검증하지 않았음 (from stock-start)
- [medium] 실제 네트워크/UI/타이머는 단위 테스트 범위 밖이며 수동 E2E 필요 (from stock-start)
- [medium] 실제 네이버 비공개 API 응답 스키마 변경이나 차단은 단위 테스트만으로 완전히 검증할 수 없음 (from stock-start)
- [medium] 백오프/재시도 정책이 단순(5초 주기 자연 재시도)에 한정 (from stock-start)
- [medium] 네이버 비공식 모바일 API 스키마 변경 시 파서 실패 가능 (응답 변경 모니터링 부재) (from stock-start)
- [medium] API 키 없는 기본 구현은 틱 단위 스트리밍 실시간을 보장하지 못한다. (from stock-start)
- [medium] ViewModel 내부 비동기 대기 구문에 .ConfigureAwait(false)를 다중 적용하여 비동기 완료 후 WPF UI 스레드 컨텍스트를 유실하고 렌더링 또는 Command 갱신 오류를 유발할 수 있는 멀티스레딩 리스크 (MAJOR) (from stock-start)
- [medium] 국내 금시세(CMDT_GD) 대신 국제 금시세(CMDT_GC)가 오적용되어 원화/g당 시세 스펙을 위반하고 온스당 달러가 표출되는 심각한 스펙 오차 리스크 (BLOCKER) (from stock-start)
- [medium] Prism 및 시세 파싱 관련 새 NuGet 의존성이 필요할 수 있다. (from stock-start)
- [medium] 갱신 간격/종목/통화 하드코딩 — 외부 설정화 필요 (from stock-start)
- [medium] 5초 폴링 방식에 따라 네트워크 트래픽 빈도가 높고 자동 차단 우려가 상존함 (from stock-start)
- [medium] 네이버 모바일 API의 비공식 사용에 따른 네이버 응답 JSON 스키마 변경 시의 유지보수 리스크 존재 (from stock-start)

## 미해결 리뷰/검증 항목

- [open/medium] Manual E2E (네트워크 차단/회복, 실시간 UI 색상 변화) — 실제 데스크톱 환경에서의 검증 필요 (from stock-start:02_develop)
- [open/minor] 그러나 JSON 원본 문자열을 인자로 받아 파싱 레코드를 반환하는 세부 가공 메서드(`ParseStockResponse`, `ParseIndexResponse`, `ParseMarketIndicatorResponse`)들이 인터페이스에 포함되어 외부로 불필요하게 노출되어 있습니다. (from stock-start:03_review)
- [open/minor] **severity**: MINOR (from stock-start:03_review)
- [open/medium] 01_plan.md의 금시세 코드 표기(CMDT_GD vs CMDT_GC) 최종 확정은 사용자 확인 가능 (from stock-start:02_develop)
- [open/minor] 이로 인해 인터페이스가 구체적인 JSON 데이터 파싱 포맷과 강하게 결합하게 되며, 다른 공급자 서비스(예: 다른 XML, gRPC, DB 기반 시세 서비스)를 추가할 때 아무 기능도 수행하지 않는 껍데기 파싱 메서드들을 억지로 구현해야 하는 불합리한 결합이 발생합니다. (from stock-start:03_review)
- [open/minor] **지적 사항**: 관심사 분리(Separation of Concerns) 아키텍처 원칙 위반. (from stock-start:03_review)
- [open/minor] **어떻게 개선해야 하는지**: (from stock-start:03_review)
- [open/minor] **왜 문제인지**: (from stock-start:03_review)
- [open/minor] 해당 파싱 메서드들을 `IFinanceService` 인터페이스 규약에서 제거합니다. (from stock-start:03_review)
- [open/medium] ViewModel 파일 내 .ConfigureAwait(false) 전면 제거를 통한 UI 스레드 동기화 마샬링 컨텍스트 복구 (from stock-start:03_review)
- [open/minor] `IFinanceService` 인터페이스는 금융 데이터를 비동기로 조회해 오는 상위 서비스 수준의 기능적 규약만 제공해야 합니다. (from stock-start:03_review)
- [open/medium] 국내 금시세 데이터 소스 CMDT_GD로의 엔드포인트 전면 전환 및 뷰모델 단위를 원화 기준으로 보정 (from stock-start:03_review)
- [open/minor] `NaverFinanceService` 구현체 내부에 `private` 또는 `internal` 헬퍼 메서드로 완전히 격리합니다. 단위 테스트를 위해 internal 메서드로 접근할 수 있도록 지정하거나, 혹은 좀 더 깔끔하게 JSON 파싱 책임만을 전담하는 별도의 `INaverFinanceParser` 등의 헬퍼를 두는 방안을 권장합니다. (from stock-start:03_review)
- [open/minor] **해결 코드 위치**: [IFinanceService.cs:L24-L28](file:///C:/_SW/Agent-Orchestration/src/StockApp/Services/IFinanceService.cs#L24-L28) (from stock-start:03_review)
