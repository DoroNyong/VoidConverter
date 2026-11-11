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

## 2. 폰트

| 폰트 | 출처 | 라이선스 |
|---|---|---|
| Unity 기본 제공 폰트 (`LegacyRuntime.ttf`) | Unity 엔진에 포함 | Unity 사용 조건을 따름: **확인 필요** |

글자는 영어(ASCII)만 쓴다. 외부에서 내려받은 폰트는 없다.

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
