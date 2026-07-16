# Gallery No.8

이상현상을 구분하고 8번의 정답을 맞추면 탈출하는 공포게임.
<img width="1570" height="880" alt="image" src="https://github.com/user-attachments/assets/446972f2-42a9-4b98-88ba-7441de26ae6a" />

---

프로젝트 소개
--

| 분류 | 내용 |
|------|------|
| **장르** | 8번 출구류 3D 3인칭 공포게임 |
| **플랫폼** | PC |
| **엔진** | Unity 6.0 LTS |
| **언어** | C# |
| **IDE** | Visual Studio 2022 Community |
| **버전 관리** | GitHub |
| **개발 인원** | 1인 개발 |
| **개발 기간** | 3주 |

---

게임 소개
--
반복되는 전시관를 탐색하며 이상 현상을 관찰하는 3인칭 공포 게임입니다.  
매 루프마다 석상 Enemy의 활성화 유무가 달라지고 활성화된다면 5개의 석상 Enemy중 하나가 플레이를 위협합니다.  
Enemy가 있으면 나왔던 곳으로 다시 돌아오고, 없다면 반대면 통로까지 진행하는 형식입니다.  
정답이면 다음 층으로 진행하고, 오답이면 루프가 초기화됩니다.  

---

인게임 화면
--

- 전경
<img width="1567" height="884" alt="image" src="https://github.com/user-attachments/assets/02922ebd-6a1f-457a-bc4b-21c344c47392" />

- 인게임 플레이
<img width="1353" height="750" alt="image" src="https://github.com/user-attachments/assets/581d0356-e52d-4005-ad14-610d54cceee8" />
<img width="1342" height="748" alt="image" src="https://github.com/user-attachments/assets/002e3281-5ba6-4b9d-a3e4-8bb6bf57c0d1" />

- 엔딩 크레딧
<img width="1257" height="756" alt="image" src="https://github.com/user-attachments/assets/1629e398-1463-41f4-af9d-6e8a69d661c6" />
<img width="1295" height="364" alt="image" src="https://github.com/user-attachments/assets/0d45f954-d75f-40b6-ab52-68386c044f15" />

---

조작법
--

| 키 / 입력 | 동작 |
|-----------|------|
| `W` `A` `S` `D` | 캐릭터 이동 |
| 마우스 이동 | 시점 회전 |

---
핵심 기술
--
- Enemy FSM 구현
- Input System과 BlendTree 연동
- Avatar Mask & IK Rigging
- NavMesh
- Shader Graph

---
트러블 슈팅
--
- URP에 대한 이해


| 단면 렌더링 | Shader Graph 작업 | 양면 렌더링 |
|-----------|------|------|
| <img width="283" height="346" alt="image" src="https://github.com/user-attachments/assets/e5e04d4b-a1c7-4d8d-a373-eea71ab9b136" /> | <img width="390" height="327" alt="image" src="https://github.com/user-attachments/assets/c39fc806-d98a-4325-a6ce-c14522619457" /> | <img width="284" height="335" alt="image" src="https://github.com/user-attachments/assets/5cf6bfa4-c4f8-419d-b3c4-b984f0733ced" /> |

Q : 해당 커튼은 8번출구 게임처럼 꺾인 복도 형태가 아니기 때문에 전시실을 가릴 필요가 있었으나 단면 렌더링에 의해 뒷면에서 반대편이 보이는 이슈 발생  
A : URP의 최적화 기능 중 하나인 Backface Culling에 의하여 물체의 뒷면을 렌더링 하지 않음을 인식. 이것을 해결하기 위해 Shader Graph를 이용하여 Render Face를 Both로 설정하여 문제를 해결.
