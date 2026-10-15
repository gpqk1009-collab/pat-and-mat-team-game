# 프리티 제노사이드 — 1차 전투 프로토타입

## 제공 형태와 검증 범위

2026-10-15에 실제 프로젝트의 `Assets/PrettyGenocide`로 통합했습니다. 이 문서 옆의
`Assets`는 이전 소스 키트 보관본입니다. 실행·수정할 때는 프로젝트 루트의 `Assets/PrettyGenocide`를 사용하며,
보관본을 다시 복사하지 마세요. 테스트 씬과 재질도 이미 생성되어 있습니다.

작성 대상: Unity 6 계열, 우선 Unity 6.3 LTS + Universal 3D(URP). Built-in 3D를 위한 머티리얼 분기도 있습니다. HDRP는 대상이 아닙니다. 필요한 추가 패키지는 Input System뿐이며, Cinemachine·DOTween·외부 모델·유료 에셋은 사용하지 않습니다.

실제 Unity 6000.3.23f1에서 C# 컴파일, 씬 생성, 탄수 장부 검증과 최초 Play Mode 테스트 15개를 통과했습니다.
후속 한글 HUD/오른쪽 체력/슬라이딩 자세 수정 후에는 임시 프로젝트 사본에서 **19개 테스트를 모두 통과**했습니다.
최신 화면의 육안 확인은 사용자의 Esc 입력으로 중지되었습니다. 자세한 범위는 VALIDATION.md를 참고하세요.

## 먼저 실행하기

1. 현재 Unity 프로젝트를 6000.3.23f1에서 엽니다. 패키지/입력 설정은 이미 준비되어 있습니다.
2. `Tools → Pretty Genocide → Open Prototype Scene` 또는
   `Assets/PrettyGenocide/Generated/PrettyGenocide_Prototype.unity`를 엽니다.
3. Play를 누르고 Game 뷰를 클릭합니다. 포커스 복귀용 클릭은 조준/발사로 소비되지 않을 수 있습니다.
4. `Run Ammo Ledger Self Tests` 메뉴로 탄수 장부를, Window → General → Test Runner → PlayMode에서
   `PrettyGenocide.Tests`로 전투/이동 회귀 테스트를 실행할 수 있습니다.
5. 새 맵 사본이 필요할 때만 `Create Prototype Scene`을 사용합니다. 기존 맵과 재질을 덮어쓰지 않습니다.

씬 생성기는 빈 사용자 레이어에 PG_Player, PG_Target, PG_Bullet, PG_EnemyBullet을 추가합니다. 기존 레이어 이름은 덮어쓰지 않습니다. 따라서 ProjectSettings/TagManager.asset 변경은 발생합니다. 씬과 머티리얼은 매번 고유 경로에 생성하고 기존 파일을 덮어쓰지 않습니다. 생성 버튼을 여러 번 누르면 새 씬/머티리얼 세트가 추가됩니다.

## 조작과 결정한 규칙

| 입력 | 동작 |
|---|---|
| WASD | 카메라 수평 방향 기준 이동 |
| Shift 유지 | 달리기. 현재 버전에서는 조준 중에도 가능 |
| Space | 커서/조준점이 향하는 수평 방향으로 슬라이딩 |
| 첫 좌클릭 | 조준 진입만 수행. 탄환은 발사하지 않음 |
| 조준 중 좌클릭 | 클릭마다 1발 발사. 누르고 있는 자동 연사는 아님 |
| 여섯 번째 탄환 발사 | 0.25초 후 자동 회수. 조준은 유지 |
| 우클릭 | 즉시 조준 해제 + 이미 발사된 모든 탄환 조기 회수 |
| Tab | 자유 커서 방식 ↔ 중앙 조준점/마우스룩 방식 전환 |
| 마우스 가운데 버튼을 누르고 이동 | 자유 커서 방식에서 카메라 궤도 회전 |
| F2 | 고정형 테스트 몬스터 2개의 발사 켜기/끄기. 기본 꺼짐 |
| Esc | 조작/커서 해제. Game 뷰 좌클릭으로 복귀 |

기본은 사용자가 말한 '마우스 커서 방향 조준'에 맞춘 자유 커서입니다. 마우스로 캐릭터 주위를 계속 돌아보는 일반적인 숄더뷰가 더 적합하면 Tab을 누르거나 Player의 PGInput.freeCursorAim을 끕니다. 두 방식 모두 우클릭은 카메라 회전이 아니라 조준 해제/회수입니다.

슬라이딩은 Space를 누른 순간의 방향을 저장합니다. 도중에 커서나 WASD를 바꾸어도 꺾이지 않습니다. 지상에서만 시작하며 무적 판정은 없습니다. 낮은 통로 안에서는 슬라이딩 종료 후에도 머리 위 공간이 확보될 때까지 낮은 콜라이더를 유지합니다. 현재 캐릭터 포즈는 시각 모델의 높이를 줄이는 임시 표현이며, 정식 애니메이션은 아닙니다.

조준 중 일반 이동속도는 느려지지만 Shift를 누르면 달리기 속도를 우선합니다. Space 중 발사와 회수도 허용합니다.

## 리볼버 사이클

- 6발은 6개의 실제 탄환 권한입니다. `PGBulletLedger`가 발사된 탄환의 고유 ID를 보관합니다.
- 발사체는 첫 적 또는 지형 충돌 시 전진을 멈추고 낙하합니다. 적에 대한 기본 피해는 20입니다. 허공에 쏘면 최대 사거리에서 낙하합니다.
- 탄이 날아가는 중이어도 우클릭/자동 회수 대상이 됩니다.
- 전탄 소모 후에는 0.25초의 짧은 여유를 두고 회수합니다. 마지막 탄이 발사 프레임에 바로 되돌아오는 상황을 피하기 위한 초기값입니다.
- 회수 탄은 벽/바닥에 막히지 않습니다. 지나가는 경로의 모든 적에게 탄환당 35 피해를 1회씩 줍니다. 같은 적의 콜라이더가 여러 개이거나 여러 프레임 겹쳐도 같은 탄환의 회수 피해는 중복되지 않습니다.
- 서로 다른 회수 탄 6발이 같은 적을 지나가면 각각 피해를 줍니다.
- 적의 탄환이 회수 경로와 겹치면 제거합니다. 이 키트에서는 '적 탄환 관통'을 요격으로 해석했습니다. 요격 점수/소유자 추가 피해는 아직 없습니다.
- 실제로 플레이어의 회수 지점에 도착해야 탄수가 1발 복구됩니다. 단순히 일정 시간이 지났다는 이유로 탄수를 6으로 대입하지 않습니다.
- 일부 탄이 먼저 도착해도 전체 회수가 끝나기 전에는 발사를 잠급니다. 이동/달리기/슬라이딩은 가능합니다.
- 자동 회수 후에는 조준 유지. 우클릭으로 회수한 경우에는 조준 해제 상태라 다음 좌클릭이 다시 조준입니다.
- 회수 중 우클릭을 여러 번 눌러도 별도 회수 루프나 탄환을 추가 생성하지 않습니다.

회수 경로의 해석: 매 프레임 '현재 탄환 위치 → 현재 플레이어 회수 지점'으로 직선 이동합니다. 정지해 있을 때 전체 경로는 직선입니다. 플레이어가 움직이면 목적지가 갱신되므로 전체 궤적은 휠 수 있습니다. 회수 시작 위치에 경로를 고정하는 방식은 구현하지 않았습니다. 고정 직선이 기획 의도라면 그 변경을 먼저 하고 조작감을 비교해야 합니다.

회수 반경은 0.22이고 바닥의 탄환에서 플레이어 허리 높이로 돌아오므로, 낮은 위치의 적 콜라이더도 포함해야 합니다. '모든 오브젝트 관통'은 지형에 막히지 않는다는 뜻이며, 현재 지형은 파괴하거나 피해를 받지 않습니다.

## 카메라 연출

기본 시야각 72 → 조준 시 44, 뒤쪽 거리 5.8 → 2.25, 전환 시간 0.11초입니다. 조준에 진입할 때만 짧은 기울기/이동 펀치를 주고, 발사마다 짧은 반동/흔들림을 줍니다. 우클릭으로 해제하면 기본 구도로 돌아옵니다.

카메라의 논리 자세(입력·조준)와 실제 렌더링 자세(흔들림)를 분리했습니다. 발사 흔들림이 다음 프레임의 조준 회전에 누적되지 않습니다. 조준 전환에 따른 카메라 위치/FOV 변경은 조준 광선에도 반영됩니다. 따라서 자유 커서 모드에서 조준 전환 전후 동일한 픽셀이 항상 같은 월드 지점을 의미하지는 않습니다.

`CameraRig → PGCameraRig.motionScale`을 0으로 하면 발사 흔들림/조준 펀치가 꺼집니다. 줌/숄더 전환까지 끄지는 않습니다. 줌 변화도 줄이려면 normalFov와 aimFov, normalOffset과 aimOffset을 가까운 값으로 조정합니다. 과한 연출 때문에 표적 추적이 어려우면 aimTransitionSeconds를 0.15~0.20으로 늘리는 비교를 권합니다.

카메라 벽 충돌 보정은 포함했지만, 모든 복잡한 실내 공간에서 검증된 카메라 솔버는 아닙니다.

### 탄환 회수와 회수 명중의 화면 피드백

우클릭 조기 회수 또는 6발 소모 후 자동 회수가 실제로 시작하면 카메라가 가볍게 흔들립니다.
체감 피드백에 따라 회수 중 흔들림을 두 차례 줄였습니다. 회전 강도는 0.2도, 위치 강도는 0.005이며 시작도 부드럽게 조정했습니다.
자동 회수 대기 시간에는 시작하지 않으며, 마지막 탄환이 플레이어에게 도착하면 0.18초에 걸쳐 잦아듭니다.
회수 탄환이 적에게 실제 피해를 입힐 때는 지속 흔들림 위에 더 큰 회전·위치 충격을 더합니다.
처치하는 마지막 타격에도 적용합니다. 같은 탄환/같은 적의 중복 콜라이더에서는 한 번만 발생하고,
피해가 0이거나 이미 죽은 적에는 추가 충격이 발생하지 않습니다.
여러 적과 여러 탄환이 동시에 명중해도 충격은 기본 최대 1.6배까지만 누적됩니다.

Hierarchy에서 **CameraRig → PGCameraRig**를 선택해 다음 값을 조절합니다.

| Inspector 필드 | 기본값 | 역할 |
|---|---:|---|
| Recall Shake Degrees | 0.2 | 회수하는 동안의 회전 흔들림 강도 |
| Recall Shake Position | 0.005 | 회수하는 동안 카메라 위치 흔들림 |
| Recall Shake Frequency | 20 | 지속 흔들림의 빠르기 |
| Recall Shake Attack | 0.08 | 회수 시작 후 최대 강도까지 걸리는 시간 |
| Recall Shake Release | 0.18 | 회수 종료 후 가라앉는 시간 |
| Recall Hit Shake Degrees | 8 | 회수 탄환 명중 시 추가 회전 흔들림 |
| Recall Hit Shake Position | 0.22 | 명중 시 추가 위치 흔들림 |
| Recall Hit Kick Degrees | 5 | 명중 순간 위로 튕기는 충격 |
| Recall Hit Shake Seconds | 0.24 | 명중 충격 지속시간 |
| Recall Hit Max Stack | 1.6 | 여러 번 명중할 때의 충격 누적 상한 |

회수 자체를 더 격하게 하려면 Recall Shake Degrees/Position을,
명중감을 더 강하게 하려면 Recall Hit Shake Degrees/Kick Degrees를 올립니다.
Motion Scale은 발사·조준·회수 효과 전체의 공통 배율이며 0이면 화면 흔들림이 꺼집니다.
논리 조준선, 탄환 이동/피해, 슬라이딩 방향과 적 체력 패널의 대상 선택에는 흔들림을 적용하지 않습니다.
회수 탄환이 플레이어 손에 도착하는 장전 동작은 적에게 피해를 주는 명중과 구분합니다.

## 먼저 조절할 값

| 위치 | 필드 | 초기값 |
|---|---|---:|
| Player / PGPlayerMotor | walkSpeed | 5 |
| Player / PGPlayerMotor | sprintSpeed | 8.5 |
| Player / PGPlayerMotor | aimMoveSpeed | 3.8 |
| Player / PGPlayerMotor | slideSpeed | 15 |
| Player / PGPlayerMotor | slideDuration | 0.35 |
| Player / PGPlayerMotor | slideCooldown | 0.8 |
| Player / PGRevolver | shotInterval | 0.18 |
| Player / PGRevolver | autoRecallDelay | 0.25 |
| Player / PGRevolver | bulletSpeed / returnSpeed | 55 / 44 |
| Player / PGRevolver | outboundDamage / returnDamage | 20 / 35 |
| CameraRig / PGCameraRig | normalFov / aimFov | 72 / 44 |
| CameraRig / PGCameraRig | aimTransitionSeconds | 0.11 |
| CameraRig / PGCameraRig | motionScale | 1 |

속도 단위는 Unity 월드 단위/초이고 시간은 초입니다. 수치는 검증 완료된 밸런스가 아니라 시작점입니다. 플레이 중 Inspector 수정은 보통 정지 시 되돌아가므로 최종 값은 Play를 끈 뒤 반영합니다.

## 코드 구성

`Runtime/PGInput.cs`: 입력, 자유 커서/마우스룩, 포커스 처리.

`Runtime/PGCameraRig.cs`: 추적·궤도·줌·벽 보정·연출과 조준 광선 분리.

`Runtime/PGPlayerMotor.cs`: CharacterController 이동, 중력, 슬라이딩, 낮은 천장 확인, 테스트용 체력/리스폰.

`Runtime/PGRevolver.cs`: 조준 상태, 클릭 발사, 자동/조기 회수, 탄환 생성과 회수 완료.

`Runtime/PGBulletLedger.cs`: Unity에 의존하지 않는 6발 장부. 중복 회수 콜백 방지.

`Runtime/PGRecallBullet.cs`: 전진·낙하·정착·회수, 일반 타격, 관통 타격, 적탄 요격.

`Runtime/PGDamageable.cs`: 표적 체력, 일반/회수 피격 횟수, 재생성.

`Runtime/PGEnemyProjectile.cs`, `PGTrainingTurret.cs`: 선택형 고정 사격 몬스터.

`Runtime/PGPrototypeHUD.cs`: 탄수/상태/체력/피격 횟수 표시, 몬스터 발사 토글.

`Editor/PGPrototypeSceneBuilder.cs`: 한 번의 메뉴 명령으로 시험장 생성.

`Editor/PGPrototypeSelfTests.cs`: 탄수 장부 검증 메뉴. 별도 NUnit 패키지 불필요.

## 현재 들어 있지 않은 것

그랩패링, 리본 공격, 각인/점수/강화창, 구역 진행, 적 웨이브, 추적/길찾기 AI, 보스, 정식 캐릭터 리깅/걷기/슬라이딩 애니메이션, 오디오, 세이브, 옵션 저장, 키 리바인딩, 게임패드, 모바일, 네트워크는 없습니다. 화면의 블록 캐릭터는 프리미티브로 만든 자리표시자입니다.

프로덕션 최적화도 아직 아닙니다. 탄환은 생성/제거 방식이고, 물리 검사 중 배열 할당과 회수 시 Physics.SyncTransforms가 있습니다. 먼저 6발 사이클의 플레이 감각을 검증한 다음 풀링/NonAlloc 검사/업데이트 순서를 개선합니다. 물리 콜라이더가 비활성화되거나 Terrain 데이터가 없는 오브젝트는 충돌 검사 대상이 아닙니다.

HP가 0이거나 시험장 아래로 떨어지면 플레이어 위치와 체력만 초기화합니다. 리볼버/표적 전체를 초기화하는 정식 체크포인트 시스템은 아닙니다.

## 복셀 모델 교체

현재 프로젝트에 `Assets/Model/balloonEnemy.obj`, `.mtl`, `.png`와
`Assets/Model/grass.obj`, `.mtl`, `.png`가 있습니다. OBJ 헤더상 MagicaVoxel에서 내보낸 리소스입니다.
아래 작업은 **Play를 끈 상태**에서 진행하고, 마지막에 Ctrl+S로 씬을 저장합니다.
Project 창은 원본 파일, Hierarchy 창은 현재 씬에 놓인 오브젝트입니다.

### 1. 새 복셀 리소스 가져오기

1. 복셀 도구에서 메시를 OBJ 또는 FBX로 내보냅니다. 원본 `.vox`만으로 이 프로젝트의 3D 모델을 교체하지는 못합니다.
2. OBJ는 같은 이름의 `.obj`, `.mtl`, `.png`를 한 폴더에 함께 보관합니다. 예: MyGirl.obj / MyGirl.mtl / MyGirl.png.
3. Unity Project 창의 Assets 아래에 원하는 폴더를 만들고 파일을 넣습니다. 기존 리소스는 이미 가져와져 있으므로 다시 복사할 필요가 없습니다.
4. `.obj` 또는 `.fbx`를 선택하면 Inspector에 Model/Materials 가져오기 설정이 나옵니다.
   스케일 설정을 바꾸면 Apply를 누릅니다. 복셀 모서리가 둥글게 보이면 모델이 제공하는 법선을 Import하는 설정을 확인합니다.
5. 원본 형식/가져오기 설정은 [Unity 모델 가져오기 문서](https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/asset-types/models/importing/importing-model-files)를 참고할 수 있습니다.

### 2. URP에서 색이 정상적으로 나오게 만들기

1. Project 창에서 우클릭 → Create → Material로 재질을 만듭니다. 예: BalloonEnemy_URP.
2. 재질 Inspector의 Shader를 `Universal Render Pipeline/Lit`로 선택합니다.
3. `Base Map`의 텍스처 칸에 `balloonEnemy.png`를 넣고, 옆의 색상은 흰색으로 둡니다.
4. 시험장에 놓은 모델을 펼쳐 실제 `Mesh Renderer`가 붙은 자식을 선택합니다.
5. Mesh Renderer → Materials → Element 0에 만든 재질을 넣습니다. 메시가 여러 개면 각 메시에도 연결합니다.
6. 색이 번지면 PNG를 선택해 Filter Mode를 Point (no filter), Compression을 None으로 설정하고 Apply합니다.
   특히 MagicaVoxel 팔레트 PNG는 작은 색상표이므로 인접 색상이 섞이지 않는지 확인합니다.
7. 모델이 분홍색이면 셰이더/렌더 파이프라인을, 흰색이면 Base Map과 Renderer 재질 연결을 확인합니다.

이 설정은 현재 URP 프로젝트용입니다. 원본 OBJ/MTL을 텍스트 편집기로 바꿀 필요는 없습니다.

### 3. 플레이어를 내 복셀 캐릭터로 교체하기

1. Hierarchy에서 `Player`를 펼치고 `VisualPose_REPLACE_WITH_VOXEL_MODEL`을 펼칩니다.
2. Project 창의 캐릭터 모델 파일을 이 VisualPose 위로 드래그해 **자식**으로 넣습니다.
   모델 이름은 `MyVoxelCharacter` 등 알아보기 쉽게 바꿔도 됩니다.
3. 추가한 모델 자식만 선택하고 Transform의 Local Position을 (0,0,0), Rotation을 (0,0,0)에서 시작합니다.
4. Scene 뷰에서 발바닥이 Player 원점의 Y=0 부근에 오도록 모델 자식의 위치를 조절합니다.
   모델의 중심점이 몸 중앙이면 로컬 Y를 올려야 합니다.
5. 캐릭터 높이가 기존 CharacterController 높이인 약 1.8에 맞도록 **모델 자식의 Scale**을 균일하게 조절합니다.
   예를 들어 현재 높이가 18이라면 기존 Scale에 0.1을 곱하면 약 1.8입니다. 값은 모델마다 다릅니다.
6. 앞면이 Player의 로컬 +Z, 즉 파란색 축을 향하게 모델 자식만 회전합니다.
   뒤를 보고 있다면 Y=180부터 확인합니다. Player 자체의 축을 뒤집지 않습니다.
7. 기존 큐브 신체인 Boot_L/R, Leg_L/R, SkirtHem, Skirt, Torso, Arm_L/R, Head,
   HairTop, HairBack, Tail_L/R, Ribbon_L/R은 오브젝트 이름 왼쪽 활성 체크를 꺼서 숨깁니다.
   새 모델이 정상인지 확인하기 전에는 삭제할 필요가 없습니다.
8. **VisualPose, WeaponPivot, Muzzle, MuzzleFlash, RecallSocket은 유지**합니다.
   특히 VisualPose 전체를 비활성화하면 새 모델과 총까지 함께 사라집니다.
9. Player의 PGPlayerMotor → Visual Pose에는 기존 VisualPose가 연결되어 있어야 합니다.
   새 모델 자식으로 이 참조를 바꾸지 않아도 됩니다.
10. Player 루트의 Scale은 (1,1,1)을 유지합니다. CharacterController와 PGInput, PGPlayerMotor,
    PGRevolver도 그대로 유지합니다. 모델 자식에는 불필요한 Rigidbody/Collider를 추가하지 않습니다.
11. Play에서 WASD, Space, 첫 클릭/발사/우클릭 회수를 확인합니다. VisualPose 아래에 넣은 새 모델에는
    현재 슬라이딩의 낮은 자세와 기울기가 그대로 적용됩니다.

권장 Hierarchy는 다음과 같습니다. 이름의 들여쓰기는 부모/자식 관계를 뜻합니다.

```text
Player                           ← 이동·물리·입력·리볼버 컴포넌트 유지
  VisualPose_REPLACE_WITH_VOXEL_MODEL
    MyVoxelCharacter             ← 내 모델, 크기/축/발 위치는 여기서 조절
    기존 큐브 신체들              ← 오브젝트 비활성화
    WeaponPivot                  ← 손 위치에 맞게 로컬 위치 조절
      RevolverBody / Barrel / Grip
      Muzzle                     ← 총알이 출발하는 총구
        MuzzleFlash
    RecallSocket                 ← 탄환이 실제 도착하는 회수 지점
```

새 캐릭터의 손 위치가 다르면 WeaponPivot의 로컬 위치만 손으로 옮깁니다.
조준 중 회전은 코드가 제어하므로 WeaponPivot의 회전으로 모델의 잘못된 앞뒤를 고치지 않습니다.
총 모델도 교체하려면 WeaponPivot 아래에 총 모델을 넣고 기존 Body/Barrel/Grip만 숨긴 뒤,
Muzzle을 새 총구 끝으로 이동합니다. PGRevolver의 Muzzle/Recall Socket 참조는 유지합니다.
몸 크기가 크게 달라질 때만 CharacterController의 Height/Radius/Center를 함께 조정하고,
Slide Height가 지름보다 작지 않은지 확인합니다.

### 4. 기존 표적을 balloonEnemy로 교체하기

1. Hierarchy에서 `Target_A`를 선택합니다. 루트의 PGDamageable과 BoxCollider를 유지합니다.
2. Project 창의 `balloonEnemy.obj`를 Target_A 아래로 드래그합니다.
3. 모델 자식의 Local Position/Rotation/Scale을 조절해 기존 표적 위치에 맞춥니다.
   공중에 뜬 풍선처럼 보이게 하려면 모델 자식의 Y를 올릴 수 있습니다.
4. 기존 `Body`, `Head`, `Face` **게임오브젝트**를 비활성화합니다.
   MeshRenderer 체크만 끄면 리스폰 시 기존 렌더러가 다시 켜질 수 있으므로 오브젝트를 끕니다.
5. Target_A 루트의 BoxCollider → Edit Collider로 새 모델을 감싸도록 Center와 Size를 조절합니다.
   모델만 위로 띄웠다면 콜라이더 중심도 위로 옮겨야 합니다. Is Trigger는 끕니다.
6. Target_A와 모든 자식의 Layer를 `PG_Target`으로 설정합니다. 자식에도 적용할지 묻는 창은 적용합니다.
7. PGDamageable의 Display Name을 `풍선 괴물`, Max Health를 원하는 체력으로 바꿉니다.
   Renderers/Colliders 배열은 비워 두면 시작할 때 활성 자식에서 자동 수집합니다.
   이전 모델을 수동 연결해 둔 배열이 있으면 그 항목을 제거하거나 새 모델로 바꿉니다.
8. 위 재질 절차로 `balloonEnemy.png`를 모델 Renderer에 연결합니다.
9. Play → 좌클릭으로 조준 → 풍선에 커서를 올리면 오른쪽에 이름/체력이 표시되어야 합니다.
   발사 시 체력이 줄고, 커서를 빼거나 벽으로 가리면 패널이 사라져야 합니다.
10. 새 모델 자식에 PGDamageable을 또 붙이지 않습니다. 피격 판정은 부모 Target_A의 한 컴포넌트를 사용합니다.

포탑의 외형을 바꿀 때도 같은 방식이며 `PGTrainingTurret`과 `EnemyMuzzle`을 유지하고
EnemyMuzzle을 새 모델의 발사 위치로 옮깁니다. 외형 교체만으로 추적 AI가 추가되지는 않습니다.

### 5. grass 등 배경과 장애물 교체

`grass.obj`는 시험장 바닥 위에 장식으로 배치할 수 있습니다. 장식만 한다면 Collider 없이 사용하고,
실제로 이동/탄환을 막을 벽과 상자는 Default 레이어의 BoxCollider 등 단순 충돌체를 유지합니다.
기존 Floor를 바꿀 때는 바닥 충돌체가 남아 있어야 CharacterController가 떨어지지 않습니다.
장식 메시만 바꾼 뒤 Scene 뷰에서 Collider 외곽과 모델 위치가 맞는지 확인하세요.

### 6. 반복해서 사용할 수 있게 저장

교체가 끝난 Player나 Target_A 루트를 Project 창의 원하는 Prefabs 폴더로 드래그하면
컴포넌트와 자식 모델을 함께 재사용할 수 있습니다. 현재 씬도 Ctrl+S로 저장합니다.
Create Prototype Scene은 새 시험장을 생성하는 메뉴이므로 내 모델 교체 작업을 계속하려면
수정한 씬을 다시 열어 사용하세요.

## 게임 글자를 내가 한국어로 바꾸는 방법

화면 문구의 기본값은 한국어로 바꾸었으며, 더 원하는 표현은 코드를 고치지 않고 편집할 수 있습니다.
현재 HUD는 Canvas/TMP 텍스트가 아니라 PGPrototypeHUD가 그리는 UI입니다.

1. Unity 상단 Play를 끕니다. 실행 중 바꾼 씬 값은 종료하면 돌아갈 수 있습니다.
2. Hierarchy에서 `PrototypeHUD`를 선택합니다.
3. Inspector의 `PG Prototype HUD (Script)`에서 `Texts` 항목을 펼칩니다.
4. 원하는 필드를 직접 수정합니다. 영어 필드 이름은 편집 항목 이름이고, 입력 칸 안의 값이 게임에 나타납니다.
5. Ctrl+S로 씬을 저장하고 Play로 확인합니다. 컴포넌트 Reset은 참조도 초기화하므로 사용할 필요가 없습니다.

| Texts 필드 | 나타나는 위치 | 예시 |
|---|---|---|
| Title | 왼쪽 상단 제목 | 프리티 제노사이드 |
| Ammo / Field Bullets | 장탄·필드 탄환 수 앞 | 남은 탄환 / 회수할 탄환 |
| Player Health | 플레이어 체력 앞 | 내 체력 |
| Hip / Aiming | 조준 전/조준 중 상태 | 전투 준비 / 조준 중 |
| Recall Delay / Recalling | 자동 회수 대기/회수 상태 | 회수 준비 / 탄환 회수 중 |
| Sliding / Slide Ready / Slide Cooldown | 슬라이딩 상태 | 슬라이딩! / 사용 가능 / 재사용 대기 |
| Fire Hint / Move Hint / Enemy Fire Hint | 왼쪽 조작 안내 | 원하는 키 설명 |
| Free Cursor Hint / Shoulder Hint | 화면 하단 조작 설명 | 커서 조준 모드 / 중앙 조준 모드 |
| Resume | 포커스를 잃었을 때 중앙 안내 | 화면을 클릭해 계속하기 |
| Target Heading / Target Health | 오른쪽 적 패널 | 조준한 적 / 체력 |
| Unnamed Target | 적의 이름이 비어 있을 때 | 이름 없는 적 |

적마다 이름을 다르게 쓰려면 **각 적 루트 → PGDamageable → Display Name**을 수정합니다.
Only While Aiming을 끄면 조준 버튼을 누르기 전에도 커서가 적을 가리킬 때 패널이 나타납니다.
Target Panel Width는 패널 너비, Target Panel Offset의 X는 오른쪽 여백, Y는 중앙 기준 아래쪽 이동량입니다.
Health Color는 체력바 색입니다. Show Hit Counts를 켜면 선택한 적의 일반/회수 피격 횟수도 표시합니다.

한글 폰트는 Windows에서 맑은 고딕을 우선 사용합니다. 다른 PC에서도 같은 글꼴을 보장하려면
사용 가능한 한글 `.ttf`/`.otf`를 프로젝트로 가져와 HUD Font 칸에 연결하세요.
OS 폰트 사용 동작은 [Unity Font API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Font.CreateDynamicFontFromOSFont.html)에 설명되어 있습니다.
매우 긴 문구는 고정 너비를 넘어갈 수 있으므로 실제 Game 뷰에서 길이를 확인합니다.

## 슬라이딩 조작과 조절

Game 뷰를 클릭해 조작을 활성화하고 지상에서 Space를 누릅니다. 처음 누른 순간의 평면 조준 방향으로
빠르게 미끄러지며, WASD 이동 방향이 달라도 조준 방향을 따릅니다. 도중에 카메라를 돌려도
슬라이딩 경로는 꺾이지 않습니다. 자유 커서 모드의 카메라 회전은 가운데 버튼, 중앙 조준 모드는 마우스 이동입니다.
탄환 회수 중에도 Space를 사용할 수 있습니다.

Player → PGPlayerMotor에서 조절합니다.

| 필드 | 기본값 | 의미 |
|---|---:|---|
| Slide Speed | 15 | 시작 속도 |
| Slide Duration | 0.35 | 지속 시간(초) |
| Slide End Speed Ratio | 0.6 | 종료 시 시작 속도 대비 비율 |
| Slide Cooldown | 0.8 | 시작 시점부터 다음 사용까지 시간(초) |
| Slide Height | 0.95 | 슬라이딩 물리 캡슐 높이 |
| Slide Visual Tilt | (-18, 0, -12) | 시각 모델의 X/Y/Z 기울기 |
| Pose Sharpness | 25 | 낮은 자세·기울기로 전환하는 빠르기 |

기본 설정의 슬라이딩 구간 이동거리는 약 4.2입니다. 종료 직후 남은 속도가 감속되는 거리는 별도입니다.
공중이나 재사용 대기 중에는 시작하지 않습니다. 벽에 부딪히면 멈추고, 낮은 통로에서는 머리 위 공간이
확보될 때까지 낮은 충돌체를 유지합니다. 현재 자세 표현은 모델 Transform으로 만든 임시 연출이며
팔다리가 따로 움직이는 리깅 애니메이션은 아닙니다. HUD에서 준비/슬라이딩/재사용 대기 상태를 확인할 수 있습니다.

## Codex와 함께 이어서 작업

루트의 AGENTS.md는 프로젝트 규칙 초안입니다. 기존 AGENTS.md가 있으면 덮어쓰지 말고 필요한 규칙만 합칩니다. 이 README, QA_KO.md, CODEX_PROMPTS_KO.md를 프로젝트 루트의 Docs/PrettyGenocide 같은 폴더에 두고 첫 프롬프트에서 위치를 알려 줍니다. 사용자 프로젝트를 실제로 읽지 않은 상태에서 다른 에디터 버전이나 패키지로 변경하도록 지시하지 않습니다.

## 참고한 공식 문서

- Unity 6 LTS/지원 정책: `https://unity.com/kr/releases/unity-6/support`
- Input System 설치/활성화: `https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/manual/Installation.html`
- CharacterController.Move: `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html`
- Physics.SphereCast: `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SphereCast.html`
- Physics.OverlapCapsule: `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapCapsule.html`
- Codex 작업 지침: `https://developers.openai.com/codex/learn/best-practices`

문서는 API/설정 확인용입니다. 위 문서가 이 키트 코드의 실제 실행 검증을 대신하지 않습니다.
