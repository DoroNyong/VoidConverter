# 에셋 출처·라이선스 목록 (초안)

제출용 에셋 목록이다. **"확인 필요"로 적은 칸은 아직 직접 확인하지 않았다.** 확인한 뒤 채운다.
작성일: 2026-11-11

## 1. 그림

| 파일 | 내용 | 만든 방법 | 만든 날 | 사용 위치 |
|---|---|---|---|---|
| `Assets/Resources/Icons/monster_physical.png` | 붉은 뿔 짐승 (물리 몬스터) | AI 이미지 생성 (Gemini, "나노바나나") | 2026-11-11 | 판 위 물리 몬스터 칸 |
| `Assets/Resources/Icons/monster_magic.png` | 파란 슬라임 (마법 몬스터) | 위와 같음 | 2026-11-11 | 판 위 마법 몬스터 칸 |
| `Assets/Resources/Icons/gold.png` | 금화 | 위와 같음 | 2026-11-11 | 골드 칸 |
| `Assets/Resources/Icons/heal.png` | 초록 하트 | 위와 같음 | 2026-11-11 | 회복 칸 |
| `Assets/Resources/Icons/trap.png` | 주황 가시 | 위와 같음 | 2026-11-11 | 함정 칸 |

- 도구: Gemini 이미지 생성 (Google). 정확한 모델 이름과 버전: **확인 필요**
- 프롬프트: 공통 스타일 문장 + 대상 한 줄. 공통 문장은 "Chunky retro pixel art game sprite, low resolution look, thick dark outline, flat shading, limited palette of 6 colors, no gradients, no anti-aliasing, no text, single sprite centered with empty margin around it, viewed from slightly above, solid flat magenta (#FF00FF) background." 대상 문장은 몬스터(붉은 뿔 짐승, 파란 슬라임), 금화, 초록 하트, 주황 가시 바닥. 실제로 입력한 문장과 다르면 고친다: **확인 필요**
- 라이선스·이용 약관: 생성물의 상업적 사용과 과제 제출이 허용되는지 **확인 필요** (도구의 이용 약관을 읽고 근거 문장과 날짜를 적는다)
- AI로 만들었음을 제출 문서에 명시한다.

### 가공 (직접 한 일)
생성 이미지는 마젠타 배경에 큰 캔버스로 나왔다. 아래를 스크립트로 처리해서 게임에 넣었다.
1. 배경(마젠타)을 가장자리에서부터 지우고 투명하게 만들었다.
2. 생성기가 구석에 넣는 로고 표시는 잘라냈다.
3. 그림 부분만 잘라내고, 도트 한 칸 크기(20~50픽셀)를 찾아 원래 도트 크기로 줄였다.
4. 가장자리에 남은 분홍빛 테두리를 지웠다.
5. Unity에서 점 필터, 압축 없음으로 가져오도록 설정했다(`Assets/Editor/IconImportSettings.cs`).

가공에는 Python의 Pillow와 numpy를 썼다(작업용 도구이고 게임에 들어가지 않는다).

### 임시 에셋 (2026-11-15, 나중에 직접 만든 그림으로 교체 예정)
후보정 시간을 아끼려고 Kenney의 CC0 팩을 임시로 넣었다. **교체 예정이며 최종 그림이 아니다.** CC0이라 표기 의무는 없지만 출처를 남긴다.

| 팩 | 출처 | 라이선스 | 쓰는 곳 |
|---|---|---|---|
| Tiny Dungeon 1.0 (Kenney) | https://kenney.nl/assets/tiny-dungeon, 2026-11-15에 내려받음 | CC0 1.0 (`Assets/Resources/Sprites/Monsters/Kenney-TinyDungeon-License.txt`) | 몬스터 4종 (`Monsters/*_idle.png`: slime=타일 108, beast=110, wisp=121, brute=109), 골드 칸(`Icons/gold.png`=타일 89 상자), 회복 칸(`Icons/heal.png`=타일 115 물약), 캐릭터 선택 카드와 게임 화면의 캐릭터 그림(`Sprites/Characters/warrior.png`=타일 96 기사, `mage.png`=타일 84 마법사, 2026-11-16 추가) |
| Pixel UI Pack 1.0 (Kenney) | https://kenney.nl/assets/pixel-ui-pack, 2026-11-15에 내려받음 | CC0 1.0 (`Assets/Resources/Sprites/UI/Kenney-PixelUIPack-License.txt`) | 버튼과 패널 9-slice (`Sprites/UI/box.png` = 9-Slice/Colored/grey.png) |

- 몬스터는 16x16 한 장을 그대로 쓰고, 2프레임 시트로 만들었다(두 번째 프레임은 1픽셀 위로 올린 것). 그 외 후보정은 없다.
- 이전 AI 그림은 `Assets/Art/AI_Monsters`(몬스터 4종 시트)와 `Assets/Art/AI_Icons`(골드, 회복)에 보관했다. 게임에는 들어가지 않는다. 함정 아이콘(`Icons/trap.png`)과 마법/물리 몬스터 아이콘은 위 AI 그림을 그대로 쓴다.
- 교체 방법: 같은 파일 이름의 시트를 `Assets/Resources/Sprites/Monsters`에 넣으면 된다. 프레임 크기가 16이 아니면 `TileArt.MonsterBasePixels`를 맞춘다.

## 2. 폰트

| 폰트 | 출처 | 라이선스 |
|---|---|---|
| **Galmuri11** (`Assets/Resources/Fonts/Galmuri11.ttf`) | 갈무리 v2.40.4 (Lee Minseo, quiple). https://github.com/quiple/galmuri 릴리스 `Galmuri-v2.40.4.zip`에서 2026-11-15에 내려받음 | SIL Open Font License 1.1. 저작권 문구와 라이선스 전문은 `Assets/Resources/Fonts/Galmuri-LICENSE.txt`로 같이 둠. 직접 읽어 확인함: 번들, 재배포, 게임에 포함해 판매 가능. 폰트 단독 판매와 개조본에 같은 이름 사용은 불가 |
| Unity 기본 제공 폰트 (`LegacyRuntime.ttf`) | Unity 엔진에 포함 | Galmuri 파일이 없을 때만 쓰는 대체 폰트. Unity 사용 조건을 따름: **확인 필요** |

- 영어, 한국어 모두 Galmuri11을 쓴다(한글, 영문, 일본어, 한자 포함 도트 폰트). 도트가 선명하도록 글자 크기를 11의 배수로 맞춘다(`TextUtil.cs`).
- 받은 압축 파일에는 크기별(7, 9, 11, 14)과 모노, 볼드, 비트맵 파일이 있었고 지금은 Galmuri11.ttf만 넣었다. 다른 크기는 필요할 때 같은 릴리스에서 추가한다.
- 제출물에 LICENSE.txt를 같이 둔다(OFL 조건 2).

## 3. 소리

| 소리 | 출처 |
|---|---|
| 타격, 골드, 회복 등 효과음 | 오디오 파일 없이 코드로 만든 소리 (`SfxSynth.cs`). 외부 음원 없음 |

외부에서 가져온 음원은 없다.

## 4. 그 밖의 외부 요소

| 요소 | 출처 | 비고 |
|---|---|---|
| Unity 엔진, URP(2D), Input System | Unity | 엔진과 공식 패키지 |
| 칸 모양(마름모), 효과 그림(링, 테두리) | 코드로 그린 도형 (`SpriteUtil.cs`) | 외부 그림 아님 |

## 5. 아직 안 한 일

- 도구 약관 확인과 근거 기록 (위 "확인 필요")
- 사용한 프롬프트 원문 확인
- 위습, 브루트 등 나머지 몬스터 그림은 아직 없다 (색 마름모로 표시됨)
