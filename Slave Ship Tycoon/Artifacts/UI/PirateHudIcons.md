# Pirate HUD 아이콘 제작 기록

- 도구: 기본 image_gen, transparent_background=true. API 키/CLI/외부 패키지 사용 없음.
- 스타일 참고: `Assets/SB/Art/Sprites/Ships/TransportShip.png`의 굵은 짙은 외곽선, 따뜻한 금색/붉은색, 크림색 하이라이트, 셀 셰이딩.
- 적용 파일: `Assets/SB/Art/Sprites/UI/Gold.png`, `Diamond.png`, `Emerald.png`, `TotalOffensive.png`, `FocusFire.png`.
- 각 PNG는 개별 이미지로 생성했습니다. Unity에서 Single Sprite, Point, mipmap off, uncompressed, max texture size 256으로 임포트했습니다. 런타임 UV 분할이나 Sprite 메타데이터 직접 편집을 사용하지 않습니다.
- 연결: SampleScene의 InGameToolkitScreen.Currency Slots에 재화 3개, 기존 TotalOffensive/FocusFire 데이터의 Icon에 스킬 2개. 게임 수치/효과는 기존 데이터를 유지합니다.

## 생성 프롬프트

공통 프롬프트(개별 subject 삽입):

> Create a single transparent game UI icon: {subject}. Attached pirate ship is STYLE REFERENCE ONLY, do not draw ship. Match thick very dark burgundy outlines, warm saturated colors, bold simple chunky shapes, clean cel shading and cream highlights, same fantasy pirate game visual style as ship. One isolated centered object, 15 percent transparent padding on all sides, clear readable silhouette at small 32px HUD size. Absolutely transparent empty background; no backdrop, no aura, no text, no frame, no buttons. Output square image.

| 파일 | subject |
| --- | --- |
| Gold | one shiny golden pirate doubloon coin with an embossed skull, slightly tilted |
| Diamond | one faceted cyan blue diamond gem, wide top tapering to bottom point |
| Emerald | one faceted vivid green emerald gem, elongated hexagonal silhouette |
| TotalOffensive | one command skill icon: a red pirate pennant above two bold golden upward arrows, fleet offensive rally |
| FocusFire | one command skill icon: two crossed iron pirate cannons firing one bright golden explosive cannonball, focused salvo |
