# 검증 상태 — 2026-10-15 프로젝트 통합 결과

## 최신 조정: 회수 지속 흔들림 완화

- 회전 강도 4.5 → 0.9 → 0.2, 위치 강도 0.12 → 0.024 → 0.005로 두 차례 낮췄다.
  최신 조정은 직전 설정에서 약 80% 더 줄인 값이다. 속도 20, 시작 시간 0.08초와 적 명중 시 순간 충격은 유지했다.
- PGCameraRig의 기본값과 실제 프로토타입 씬에 동일한 값을 반영하고 문서와 수동 체크리스트를 갱신했다.
- 코드·씬 설정 일치를 확인했다. 이번 수치 조정 후 Play Mode는 재실행하지 않았으며,
  아래 23/23 결과는 조정 전 회수 피드백 기능 검증 기록이다. 체감 강도는 사용자 Play Mode 확인이 필요하다.
- 변경: `Assets/PrettyGenocide/Runtime/PGCameraRig.cs`,
  `Assets/PrettyGenocide/Generated/PrettyGenocide_Prototype.unity`, 루트 `README.md`,
  이 폴더의 `README_KO.md`, `QA_KO.md`, `VALIDATION.md`.

## 이전 후속 작업: 회수 중 흔들림·회수 명중 충격

- 실제 탄환 회수가 시작되면 지속 흔들림이 빠르게 올라오고, 마지막 탄환 도착 후 기본 0.18초에 걸쳐 사라진다.
  자동 회수 대기 중이나 필드 탄환이 없는 우클릭에서는 회수 흔들림이 발생하지 않는다.
- 회수 탄환이 적에게 실제 피해를 주면 더 강한 짧은 충격을 더한다. 처치 타격에도 적용하며,
  같은 탄환/적의 중복 콜라이더, 죽은 적, 피해 0에서는 중복 충격을 주지 않는다. 연속 충격은 기본 1.6배로 제한한다.
- 회수·명중 효과는 렌더링 카메라에만 적용한다. 조준 광선, 피해, 슬라이딩 방향과 체력 패널 대상 선택은 영향을 받지 않는다.
  회전·위치 강도, 속도, 시작/종료 시간, 명중 지속시간과 누적 상한은 PGCameraRig Inspector에서 조절한다.
- Unity 6000.3.23f1의 실제 **Play Mode 테스트 23/23 통과**, 실패 0. C# 컴파일 성공.
  결과 원본: `TestResults/PG-RecallPlayMode.xml`. 기존 19개를 유지하고 아래 4항목을 추가했다.
  1. 빈 회수 무효, 실제 회수 중 흔들림, 논리 조준선 유지, 도착 후 효과 종료.
  2. 여러 콜라이더가 있는 적의 처치 타격에서 충격 1회.
  3. 피해 0 무충격, 연속 충격 상한, Motion Scale 0에서 시각 효과 해제, 충격 시간 종료.
  4. 자동 회수 대기 중 무흔들림, 자동 회수 시 조준 유지, 무기 비활성화/재활성화 시 효과 상태 동기화.
- 사용 중인 원본 에디터를 종료하지 않고 `Logs/PG-RecallValidation` 사본에서 batchmode/nographics로 실행했다.
  로그: 프로젝트 루트 `Logs/PG-RecallPlayMode.log`. 실제 물리/프레임과 카메라 변환을 검증했으며,
  최신 흔들림의 체감 강도를 화면에서 직접 조작하여 확인하지는 않았다. 수동 확인 절차는 QA_KO.md의 '카메라와 표시'에 있다.

이번 변경 파일:
`Assets/PrettyGenocide/Runtime/PGCameraRig.cs`, `PGRevolver.cs`, `PGRecallBullet.cs`;
`Assets/PrettyGenocide/Tests/PlayMode/PGCombatPlayModeTests.cs`;
루트 `README.md`; 이 폴더의 `README_KO.md`, `QA_KO.md`, `VALIDATION.md`, `TestResults/PG-RecallPlayMode.xml`.

## 이전 후속 작업: 슬라이딩 자세·한글 문구·오른쪽 적 체력

- 기존 Space 슬라이딩에 시각 모델의 기울기와 HUD의 준비/대기/슬라이딩 표시를 추가했다.
- 적 머리 위 체력 표시를 제거했다. 실제 사격과 같은 논리 조준선으로 살아 있는 적 하나를 선택하여
  오른쪽 이름·체력 수치·체력바를 표시한다. 벽, 빈 공간, 죽은 적, 포커스 해제와 조준 해제에서는 숨긴다.
  Only While Aiming을 끄면 비조준 상태에서도 커서가 가리키는 적을 표시할 수 있다.
- 모든 기본 HUD 문구를 한국어로 제공하며 PrototypeHUD의 Texts에서 직접 편집할 수 있다.
  적별 Display Name, HUD Font, 패널 위치/너비/색상도 Inspector에 공개했다.
- 기존 리볼버/카메라/이동 검증과 추가 4항목을 합쳐 **19/19 Play Mode 테스트 통과**, 실패 0.
  결과: `TestResults/PG-HudPlayMode.xml`.
- 추가 항목: 자식 콜라이더에서 적 선택·체력 갱신·사망 시 숨김 / 벽·빈 공간·조준 해제 시 숨김 /
  자유 커서와 반동에 독립적인 선택 / 슬라이딩 시각 자세 전환·복원 및 똑바른 물리 캡슐 유지.
- 사용 중인 원본 Unity 에디터를 종료하지 않기 위해 Assets/Packages/ProjectSettings의 임시 사본
  `Temp/PG-HudValidation`에서 동일 Unity 6000.3.23f1로 실제 테스트했다. 원본 에디터 로그에서도 C# 컴파일 오류가 발견되지 않았다.
- 별도로 시도한 배치 모드 ScreenCapture는 이미지 파일을 만들지 않아 캡처 검증이 실패했다.
  그 임시 캡처 코드는 제거했으며 게임 로직 테스트 19개에는 포함되지 않는다.
  현재 원본 화면을 확인하려던 Computer Use는 사용자의 물리 Esc로 중지되었다.
  따라서 최신 한글 글꼴·레이아웃의 육안 확인은 미완료이며 렌더링 검증 완료라고 주장하지 않는다.
- 원본 복셀 파일을 임의로 교체하지 않았다. 기존 balloonEnemy/grass를 기준으로 플레이어·표적·총·배경을
  직접 교체하는 절차를 README_KO.md의 '복셀 모델 교체'에 작성했다.

후속 변경 파일:
`Assets/PrettyGenocide/Runtime/PGPlayerMotor.cs`, `PGPrototypeHUD.cs`, `PGCameraRig.cs`, `PGDamageable.cs`;
`Assets/PrettyGenocide/Editor/PGPrototypeSceneBuilder.cs`;
`Assets/PrettyGenocide/Generated/PrettyGenocide_Prototype.unity`;
`Assets/PrettyGenocide/Tests/PlayMode/PGCombatPlayModeTests.cs`;
루트 `README.md`; 이 폴더의 `README_KO.md`, `QA_KO.md`, `VALIDATION.md`, `TestResults/PG-HudPlayMode.xml`.

아래 15개 결과는 이번 후속 수정 이전에 수행한 첫 통합 검증 기록이다.

아래 최신 결과는 프로젝트 루트의 `Assets/PrettyGenocide`에 대한 것이다.
이 폴더의 `Assets`는 기존 키트 보관본이며 Unity에서 컴파일되지 않는다.

## 실제 수행 결과

- Unity 6000.3.23f1, URP 17.3.0, Input System 1.20.0에서 C# 컴파일 성공.
- Unity 에디터 batch mode로 테스트 씬과 13개 재질 생성, 모든 참조 직렬화.
- 기존 SampleScene을 유지하고 Build Settings에 프로토타입 씬 추가.
- 빈 사용자 레이어 8~11에 PG_Player, PG_Target, PG_Bullet, PG_EnemyBullet 추가.
- 탄수 장부: 6발 제한, 고유 ID, 중복/알 수 없는 회수 거부, 10,000회 무작위 연산 통과.
- Unity Test Framework의 실제 **Play Mode 테스트 15/15 통과**, 실패 0.
  테스트는 실제 씬/Update/CharacterController/물리 엔진을 사용하고 입력 스냅샷을 주입한다.
  기본 테스트 시간 간격은 1/60초로 고정하며 슬라이딩 거리는 30/60/120 FPS 간격을 각각 검증한다.
- Unity GUI에서 시험장, 캐릭터, 표적, HUD와 URP 재질 렌더링 확인.
  이후 사용자의 물리 Esc 입력으로 Computer Use가 중지되었으므로 실제 마우스·키보드 조작과
  반동 감각을 끝까지 검증했다고 주장하지 않는다.

테스트 결과 원본: `TestResults/PG-PlayMode.xml`.
상세 로컬 로그: 프로젝트 루트 `Logs/PG-Setup.log`, `Logs/PG-PlayMode.log`, `Logs/PG-Editor.log`.

## 통과한 Play Mode 항목

1. 첫 클릭은 조준만, 다음 클릭은 1발, 추가 입력 없는 프레임에는 반복 발사하지 않음.
2. 6발 소모 후 자동 회수, 조준 유지, 회수 중 추가 발사 차단.
3. 우클릭 조기 회수, 조준 해제, 실제 도착 전 탄수 유지, 반복 클릭 안전성.
4. 일부 탄환만 도착해도 전체 회수가 끝날 때까지 발사 잠금.
5. 벽 뒤 여러 적 관통, 여러 콜라이더의 동일 적에 피해 1회, 적탄 제거.
6. 회수 시작부터 적 내부에 겹쳐 있어도 타격, 여러 프레임 중복 피해 없음.
7. 일반 탄환의 적 1회 피격, 전진 정지, 바닥에 낙하 후 잔존.
8. 고속 일반 탄환이 벽을 통과하지 않음.
9. 발사 반동은 실제 카메라를 움직이지만 논리 조준 광선은 바꾸지 않음.
10. WASD와 다른 조준 방향 슬라이딩, 슬라이딩 중 카메라 회전, 움직인 플레이어에게 회수.
11. 카메라 수평 방향 기준 이동, 달리기 속도 증가, 입력 해제 후 감속·정지.
12. 낮은 천장 아래에서 일어서지 않고 통로를 나오면 높이 복구.
13. 외부에서 탄환을 제거해도 같은 ID와 필드 위치에서 복구하며 즉시 장전하지 않음.
14. 고속 적탄과 회수 탄환이 프레임 사이에서 교차해도 상대 이동 경로로 요격.
15. 30/60/120 FPS 시간 간격에서 슬라이딩 거리가 동일한 설정값에 수렴.

첫 실행에서는 13개 중 9개가 통과했다. 테스트가 발사 간격보다 빠르게 클릭하던 시간 설정을
고쳤고, 고프레임에서 CharacterController의 minMoveDistance 때문에 작은 이동이 사라지는
실제 문제를 수정했다. 최종 실행은 추가된 2개 항목을 포함해 15개 모두 통과했다.

## 재실행

Unity에서 Play Mode를 종료한 다음 Window → General → Test Runner → PlayMode에서
`PrettyGenocide.Tests.PGCombatPlayModeTests`를 실행한다. 테스트는 저장된 원본 씬을 매번 불러오므로
현재 편집 내용을 먼저 저장한다. 탄수 장부만 확인하려면
Tools → Pretty Genocide → Run Ammo Ledger Self Tests를 선택한다.

에디터를 닫은 상태에서 프로젝트 루트를 작업 디렉터리로 하여 PowerShell로 실행할 수도 있다.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform PlayMode -testFilter PrettyGenocide.Tests -testResults 'Logs/PG-PlayMode.xml' -logFile 'Logs/PG-PlayMode.log'
```

## 변경 파일 목록

- `Assets/PrettyGenocide/Runtime/PGInput.cs`: 기존 새 Input System 입력 코드 통합.
- `PGPlayerMotor.cs`: 기존 이동/슬라이딩 재사용, 가감속·슬라이딩 감속·시간 적분·작은 이동 보존 보완.
- `PGCameraRig.cs`: 기존 숄더 줌/조준 분리 재사용, 강한 반동 기본값·복구 속도 설정·viewport 계산 보완.
- `PGRevolver.cs`: 기존 6발 사이클 재사용, 실제 탄환 객체·거리 확인 후 장전, 소실 탄환 위치/ID 보존.
- `PGRecallBullet.cs`: 기존 낙하/관통 재사용, 중력/수명 설정, 빠른 적탄의 상대 이동 요격 보완.
- `PGBulletLedger.cs`, `PGDamageable.cs`, `PGPrototypeHUD.cs`: 기존 코드를 그대로 통합.
- `PGEnemyProjectile.cs`, `PGTrainingTurret.cs`: 기존 테스트 포탑 재사용, 상대 이동 요격·피해/수명 설정 보완.
- `Assets/PrettyGenocide/Editor/PGPrototypeSceneBuilder.cs`: 기존 생성기 재사용, 씬 열기 메뉴·batch 진입점·Build Settings 추가.
- `Assets/PrettyGenocide/Editor/PGPrototypeSelfTests.cs`: 기존 탄수 장부 검증 메뉴 통합.
- `Assets/PrettyGenocide/Tests/PlayMode/PGCombatPlayModeTests.cs`: Play Mode 회귀 테스트 15개 추가.
- Runtime, Editor, Tests/PlayMode의 `.asmdef` 3개 및 Unity가 생성한 관련 `.meta`.
- `Assets/PrettyGenocide/Generated/PrettyGenocide_Prototype.unity`, `Generated/PrototypeAssets/*.mat` 13개와 `.meta`.
- `ProjectSettings/TagManager.asset`, `ProjectSettings/EditorBuildSettings.asset`.
- 루트 `README.md`, 이 폴더의 `README_KO.md`, `QA_KO.md`, `VALIDATION.md`, `TestResults/PG-PlayMode.xml`.

## 남은 범위

직접 입력/조준 전환/반동의 체감 확인은 QA_KO.md를 따라 진행해야 한다. 실제 화면 주사율의
프레임 페이싱 측정과 standalone 빌드는 수행하지 않았다. 복잡한 실내 카메라, XR, 렌더 텍스처,
움직이는 정식 몬스터는 미검증이다. 객체 풀링/NonAlloc 최적화도 적용하지 않았다.
그랩패링, 강화, 정식 AI와 캐릭터 애니메이션은 요청 범위에 따라 추가하지 않았다.

---

아래는 통합 이전 소스 키트의 과거 기록이며 현재 프로젝트의 검증 결과가 아니다.

## 이 환경에서 수행한 것

- Python으로 소스 키트의 12개 C# 파일 존재 확인.
- 문자열/주석을 제외한 괄호 쌍과 전처리 지시문의 균형 검사.
- 파일명과 주요 클래스명 일치 검사.
- 이 키트의 PG 클래스 참조 이름이 정의돼 있는지 검사.
- 런타임 폴더에 UnityEditor 참조가 없는지 검사.
- 새 Input System 사용, 4개 전용 레이어 설정 코드, 고유 씬/재질 경로 생성 코드 확인.
- README, Codex 규칙/프롬프트, 수동 QA 문서 존재 확인.
- 수동 코드 리뷰: 첫 클릭 분리, 6발 제한, 고유 탄환 ID 복구, 중복 회수 방지,
  회수 중 발사 잠금, 일반/회수 충돌 분리, 같은 적의 콜라이더 중복 타격 방지.

구조 점검 실행 명령:

```bash
python Tools/check_source_structure.py
```

위 점검은 C# 파싱/컴파일이나 실제 Unity 실행을 대체하지 않는다.

## 수행하지 않은 것

- Unity Editor에서 C# 컴파일.
- 씬 생성 메뉴 실행과 실제 직렬화/참조 연결 검증.
- 제공한 C# 탄수 장부 테스트 실행.
- Play Mode의 입력, 이동, 카메라, 충돌, 요격, 체력, 리스폰 검사.
- 프레임레이트별 슬라이딩/카메라/관통 판정 확인.
- 빌드/플랫폼별 테스트, URP/Built-in의 실제 머티리얼 렌더링 확인.

Unity 에디터/컴파일러가 없는 환경에서 작성했다. 따라서 '실행 검증 완료', '버그 없음',
'즉시 출시 가능'을 의미하지 않는다. 사용자의 에디터에서 먼저 컴파일하고 QA_KO.md를 실행한다.
