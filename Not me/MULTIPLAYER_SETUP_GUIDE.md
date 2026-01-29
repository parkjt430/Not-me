# Unity NGO 멀티플레이 설정 가이드

## 리팩토링 완료 항목

### ✅ 수정된 스크립트
1. **PlayerController.cs** - NetworkBehaviour로 변경, 입력/상태 동기화
2. **TaserDrone.cs** - NetworkObject 스폰 및 타겟팅 시스템
3. **UIManager.cs** - 로컬 플레이어 전용 UI 표시
4. **CameraManager.cs** - 로컬 플레이어 자동 추적
5. **GroundLooper.cs** - 각 클라이언트 독립 실행 (주석 추가)

### ✅ 새로 생성된 스크립트
1. **NetworkManagerSetup.cs** - Host/Client/Server 연결 UI 관리
2. **PlayerSpawnManager.cs** - 플레이어 스폰 위치 관리

---

## Unity 에디터에서 설정해야 할 것들

### 1. NetworkManager 설정
1. 씬에 빈 GameObject 생성 → 이름: "NetworkManager"
2. 컴포넌트 추가:
   - `NetworkManager` (Unity.Netcode.NetworkManager)
   - `UnityTransport` (Unity.Netcode.Transports.UTP.UnityTransport)

3. **NetworkManager 설정:**
   - **Player Prefab**: 플레이어 프리팹 할당 (아래 참조)

### 2. 플레이어 프리팹 설정
1. 기존 플레이어 GameObject를 Prefab으로 만들기
2. 프리팹에 다음 컴포넌트 추가:
   - ✅ `NetworkObject` (필수)
   - ✅ `NetworkTransform` (위치 동기화)
   - ✅ `NetworkRigidbody2D` (물리 동기화 - 선택사항)
   - ✅ PlayerController (이미 있음)

3. **NetworkObject 설정:**
   - ☑️ **Synchronize Transform** 체크
   - **Ownership**: Owner (플레이어가 소유)

4. **Tag**: "Player"로 설정 (이미 되어있을 것)

### 3. TaserDrone 프리팹 설정
1. TaserDrone 프리팹 열기
2. 컴포넌트 추가:
   - ✅ `NetworkObject`
   - ✅ `NetworkTransform` (위치/회전 동기화)

3. **NetworkObject 설정:**
   - ☑️ **Synchronize Transform** 체크
   - **Destroy With Scene** 체크

4. **NetworkManager에 등록:**
   - NetworkManager → **Network Prefabs List**에 TaserDrone 프리팹 추가

### 4. 네트워크 연결 UI 생성
1. Canvas에 GameObject 생성 → 이름: "NetworkUI"
2. 자식 오브젝트로 3개 버튼 생성:
   - "HostButton" (텍스트: "Host Game")
   - "ClientButton" (텍스트: "Join Game")
   - "ServerButton" (텍스트: "Start Server")
3. TextMeshProUGUI 추가:
   - "StatusText" (초기 텍스트: "Ready to connect...")
4. NetworkUI에 `NetworkManagerSetup` 스크립트 추가
5. Inspector에서 버튼과 텍스트 연결

### 5. PlayerSpawnManager 설정 (선택사항)
1. 씬에 빈 GameObject 생성 → 이름: "SpawnManager"
2. `PlayerSpawnManager` 스크립트 추가
3. 씬에 스폰 포인트 생성:
   - 빈 GameObject 여러 개 생성 (Transform만 사용)
   - SpawnManager의 **Spawn Points** 리스트에 추가

---

## 주요 변경 사항 요약

### PlayerController.cs
- `MonoBehaviour` → `NetworkBehaviour`
- 상태 변수를 `NetworkVariable<T>`로 변경
- `IsOwner` 체크로 로컬 플레이어만 입력 처리
- `[ServerRpc]` / `[ClientRpc]` 메서드 추가
- 네트워크 스폰/디스폰 처리

### TaserDrone.cs
- `MonoBehaviour` → `NetworkBehaviour`
- 타겟을 `NetworkObjectId`로 동기화
- 서버에서만 충돌 처리 및 스폰/디스폰
- 5초 후 자동 삭제 (서버에서만)

### UIManager.cs
- NetworkVariable 값 접근 (`.Value`)
- 로컬 플레이어만 등록하도록 수정

### CameraManager.cs
- `IsOwner` 체크로 로컬 플레이어 찾기
- 네트워크 스폰 후 자동으로 타겟 설정

### GroundLooper.cs
- 각 클라이언트에서 독립적으로 실행
- 주석으로 멀티플레이 노트 추가

---

## 테스트 방법

### 로컬 테스트 (같은 PC)
1. **빌드 1개 + 에디터 1개:**
   - Build Settings → Build
   - 빌드 실행 → "Host Game" 클릭
   - Unity Editor Play → "Join Game" 클릭

2. **빌드 2개:**
   - 빌드를 2개 폴더에 복사
   - 첫 번째 빌드 → "Host Game"
   - 두 번째 빌드 → "Join Game"

### 네트워크 테스트 (다른 PC)
1. 호스트 PC:
   - 방화벽에서 포트 7777 열기
   - "Host Game" 또는 "Start Server" 클릭
   - 공인 IP 확인 (ipconfig 또는 공유기 설정)

2. 클라이언트 PC:
   - NetworkManagerSetup의 `ipAddress` 필드에 호스트 IP 입력
   - "Join Game" 클릭

---

## 알려진 이슈 및 주의사항

### 1. NetworkTransform 설정
- PlayerController가 Rigidbody2D를 사용하므로 NetworkTransform 필요
- 설정: **Interpolate** 체크, **Sync Position/Rotation** 활성화

### 2. 물리 충돌
- 장애물/Checkpoint 충돌은 로컬 플레이어만 감지 (`IsOwner` 체크)
- 서버에서 상태 변경 후 모든 클라이언트에 동기화

### 3. 아이템 게이지
- `itemGauge.Value` 접근 시 NetworkVariable 사용
- Owner만 값 변경 가능 (WritePermission.Owner)

### 4. 드론 스폰
- 반드시 서버에서만 `NetworkObject.Spawn()` 호출
- NetworkManager의 **Network Prefabs List**에 등록 필수

### 5. 카메라
- 각 클라이언트가 자신의 플레이어만 추적
- NetworkManager 초기화 후 자동으로 타겟 찾기

---

## 다음 단계 (선택사항)

### 추가 기능 구현
1. **게임 시작/종료 동기화**
   - GameStateManager 생성
   - 모든 플레이어 준비 완료 후 시작

2. **랭킹/점수 시스템**
   - 누가 먼저 결승선에 도달했는지 추적

3. **장애물 동기화**
   - GroundLooper의 `RepositionObstacles()`를 ServerRpc로 변경
   - 랜덤 장애물을 모든 클라이언트에 동일하게 생성

4. **로비 시스템**
   - Unity Lobby 패키지 사용
   - 방 생성/참가 UI

5. **플레이어 커스터마이징**
   - 색상, 이름 등을 NetworkVariable로 동기화

---

## 도움이 필요할 때

- Unity Netcode 공식 문서: https://docs-multiplayer.unity3d.com/
- 디버그 로그 확인: NetworkManager의 **Log Level**을 "Developer"로 설정
- 네트워크 통계 확인: Runtime Net Stats Monitor (패키지 매니저에서 설치)

---

## 코드 요약

### NetworkVariable 접근
```csharp
// 읽기
float gauge = itemGauge.Value;

// 쓰기 (Owner만 가능)
itemGauge.Value = 1.0f;
```

### ServerRpc 호출
```csharp
// 클라이언트에서 호출 → 서버에서 실행
UseItemServerRpc();
```

### ClientRpc 호출
```csharp
// 서버에서 호출 → 모든 클라이언트에서 실행
UpdateGodVisualClientRpc(true);
```

### IsOwner 체크
```csharp
// 로컬 플레이어만 실행
if (!IsOwner) return;
```

---

## 체크리스트

- [ ] NetworkManager GameObject 생성 및 설정
- [ ] 플레이어 프리팹에 NetworkObject + NetworkTransform 추가
- [ ] TaserDrone 프리팹에 NetworkObject 추가
- [ ] NetworkManager에 Player Prefab 할당
- [ ] NetworkManager에 TaserDrone 프리팹 등록
- [ ] NetworkUI 생성 및 NetworkManagerSetup 설정
- [ ] 로컬 테스트 (빌드 + 에디터)
- [ ] 네트워크 테스트 (다른 PC, 선택사항)

---

완료! 이제 멀티플레이 게임을 테스트할 준비가 되었습니다! 🎮
