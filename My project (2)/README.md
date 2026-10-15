# pat-and-mat-team-game
2026 2학기 게임엔진(중간 팀과제)

## 프리티 제노사이드 프로토타입 (2026-10-15)

Unity **6000.3.23f1**, URP **17.3.0**, Input System **1.20.0** 환경에 통합했습니다.
실제 사용하는 코드는 `Assets/PrettyGenocide`입니다. `PrettyGenocidePrototype/Assets`는
기존 소스 키트 보관본이므로 다시 복사하면 최신 수정 사항을 덮어쓸 수 있습니다.

1. Unity에서 `Tools → Pretty Genocide → Open Prototype Scene`을 선택합니다.
   직접 열기: `Assets/PrettyGenocide/Generated/PrettyGenocide_Prototype.unity`.
2. Play를 누르고 Game 뷰를 클릭합니다. 포커스 복귀용 클릭은 발사하지 않습니다.
3. WASD 이동, Shift 달리기, Space 조준 방향 슬라이딩. 첫 좌클릭은 조준, 이후 좌클릭은 발사,
   우클릭은 조준 해제와 회수입니다. 6발 소모 시 자동 회수하며 조준을 유지합니다.
4. 자유 커서 모드에서는 가운데 마우스 버튼을 누르고 카메라를 회전합니다.
   Tab으로 중앙 조준 방식, F2로 테스트 포탑 발사를 전환합니다.

주요 수치는 Player의 `PGPlayerMotor`·`PGRevolver`, CameraRig의 `PGCameraRig`,
각 표적의 `PGDamageable`·`PGTrainingTurret` Inspector에서 조절합니다.
기존 SampleScene과 모델, 패키지 설정은 유지했습니다. 프로토타입 씬은 Build Settings에
추가했으며, 실행 파일로 빌드할 때 이 씬으로 시작하려면 씬 목록 첫 번째로 옮기세요.

Unity C# 컴파일, 씬 생성, 탄수 장부 10,000회 무작위 검증과 Play Mode 테스트 **15/15 통과**.
화면 렌더링은 확인했으며, 이후 Esc로 Computer Use가 중지되어 직접 조작 검증은 완료하지 않았습니다.
자세한 결과와 재실행 방법: [VALIDATION.md](PrettyGenocidePrototype/VALIDATION.md).
수동 체크리스트: [QA_KO.md](PrettyGenocidePrototype/QA_KO.md).

2026-10-15 후속 수정: 적 머리 위 체력 표시를 제거하고, 조준선의 살아 있는 적 한 명을
오른쪽 이름·체력·체력바로 표시합니다. PrototypeHUD → PGPrototypeHUD → Texts에서
한국어 문구를 직접 편집할 수 있습니다. 슬라이딩에는 자세 기울기와 준비/대기 표시를 추가했습니다.
후속 수정 후 Play Mode 테스트는 **19/19 통과**했습니다. 최신 한글 화면의 육안 확인은 Esc로 중지되었습니다.
복셀 리소스·총·표적 교체, 한글 문구 편집, 슬라이딩 수치 조절은
[상세 사용 설명서](PrettyGenocidePrototype/README_KO.md)를 참고하세요.

회수 중에는 화면이 가볍게 흔들리고, 회수 탄환이 적에게 피해를 주면 더 큰 명중 충격이 추가됩니다.
마지막 탄환 도착 후에는 효과가 가라앉으며 실제 조준선은 흔들리지 않습니다.
CameraRig → PGCameraRig에서 Recall Shake/Recall Hit 항목으로 강도·지속시간을 조절할 수 있습니다.
회수 피드백 기능 추가 후 Unity Play Mode 테스트는 **23/23 통과**했습니다.
이후 회수 지속 흔들림을 두 차례 낮춰 회전 강도 0.2, 위치 강도 0.005로 조정했습니다.
이 수치 조정은 코드·씬 설정 일치를 확인했으며 Play Mode 재실행과 체감 강도 직접 조작 검증은 하지 않았습니다.
