using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>
    /// The texts of the game in English and Korean, looked up by key. Any text the player reads goes through here:
    /// the screens, the messages, and the texts the rules make (item and event summaries). A key missing in Korean
    /// falls back to English. Names and descriptions that live in the config (items, events, characters) are
    /// translated through keys made from their id, see <see cref="TOr"/>. The language is set from the settings.
    /// </summary>
    public static class Loc
    {
        public const int TutorialPageCount = 5;

        /// <summary>True while the language is Korean. Set by GameSettings; plain English when nothing sets it (tests, simulations).</summary>
        public static bool Korean { get; set; }

        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            // Title, tutorial, character select, end screen
            { "title.start", "Start" },
            { "title.howto", "How to Play" },
            { "title.settings", "Settings" },
            { "title.quit", "Quit" },
            { "title.tagline", "Place a block.\nOne covered cell fires." },
            { "title.hint", "or press Space" },

            { "tut.next", "Next" },
            { "tut.back", "Back" },
            { "tut.skip", "Skip" },
            { "tut.choose", "Choose Character" },
            { "tut.hint", "{0} / {1}      Space: next    Left: back    Esc: skip" },
            { "tut.0.title", "THE BOARD" },
            { "tut.0.body", "Every stage is a 5x5 board.\nCells hold monsters, gold,\nhealing, traps or the Void.\n\nSurvive and clear all stages." },
            { "tut.1.title", "PLACE A BLOCK" },
            { "tut.1.body", "Pick one of your blocks and drag\nit onto the board.\nR or right click rotates it.\n\nE swaps your blocks for new ones." },
            { "tut.2.title", "ONE CELL FIRES" },
            { "tut.2.body", "A block covers several cells,\nbut only ONE of them fires.\nThe chance shows on each cell.\n\nEvery covered cell is spent\neither way." },
            { "tut.3.title", "READ THE WARNINGS" },
            { "tut.3.body", "A red DEAD tag means that cell\ncould end your run.\nPurple marks the boss's danger\nzone, which hurts on top of the\ncell's own effect." },
            { "tut.4.title", "STAGES" },
            { "tut.4.body", "Battles, events and shops lead\nto a boss in two phases.\nSpace ends a stage early.\n\nGood luck!" },

            { "select.heading", "CHOOSE A CHARACTER" },
            { "select.hint", "Click a card, or press 1 / 2" },
            { "select.back", "Back" },

            { "end.cleared", "CLEARED!" },
            { "end.defeated", "DEFEATED" },
            { "end.again", "Play Again" },
            { "end.allDone", "All stages done" },
            { "end.reached", "Reached stage {0}/{1}" },
            { "end.items", "Items: {0}" },
            { "end.noItems", "No items" },
            { "end.stats", "Gold {0}    HP {1}/{2}    ATK {3}    MAG {4}" },

            // Settings
            { "set.menu", "Menu" },
            { "set.title", "SETTINGS" },
            { "set.music", "Music" },
            { "set.sfx", "Sound Effects" },
            { "set.fullscreen", "Fullscreen" },
            { "set.resolution", "Resolution" },
            { "set.language", "Language" },
            { "set.on", "On" },
            { "set.off", "Off" },
            { "set.toTitle", "Return to Title" },
            { "set.confirm", "Click again to confirm" },
            { "set.close", "Close" },
            { "set.languageName", "English" },

            // Words used inside other texts
            { "stat.atk", "ATK" },
            { "stat.mag", "MAG" },
            { "stat.hp", "HP" },
            { "stat.maxhp", "Max HP" },
            { "stat.gold", "Gold" },
            { "kind.magic", "Magic" },
            { "kind.physical", "Physical" },
            { "cell.Monster", "Monster" },
            { "cell.Gold", "Gold" },
            { "cell.Heal", "Heal" },
            { "cell.Void", "Void" },
            { "cell.Trap", "Trap" },
            { "cell.Empty", "Empty" },
            { "stage.Combat", "Combat" },
            { "stage.Event", "Event" },
            { "stage.Shop", "Shop" },
            { "stage.Boss", "Boss" },

            // HUD
            { "hud.stage", "Stage {0}/{1} {2}" },
            { "hud.blocks", "Blocks {0}/{1}" },
            { "hud.noEvents", "EVENT\n(no events)" },
            { "hud.items", "ITEMS" },
            { "hud.none", "none" },
            { "hud.info", "INFO" },
            { "hud.skill", "Skill: coming soon" },
            { "hud.weights", "TILE ODDS" },
            { "hud.weightsNone", "No change yet" },
            { "hud.next", "Next: {0}  ({1}/{2})" },
            { "hud.nextClear", "Next: Clear" },

            // Buttons beside the hand
            { "btn.reroll", "Reroll (E)\n{0} left" },
            { "btn.chooseOne", "Choose\none" },
            { "btn.continue", "Continue" },
            { "btn.restart", "Restart" },
            { "btn.place", "Place\n{0}/{1}" },
            { "btn.leaveCount", "Leave {0}/{1}" },
            { "btn.bossCount", "Boss {0}/{1}" },
            { "btn.defeatBoss", "Defeat\nBoss" },
            { "btn.nextPhase", "Next\nPhase" },
            { "btn.nextStage", "Next\nStage" },
            { "btn.leave", "Leave" },

            // Boss marker
            { "boss.title", "BOSS" },
            { "boss.clickDefeat", "Click to\ndefeat!" },
            { "boss.clickNext", "Click:\nnext phase" },
            { "boss.phase", "Phase {0}/{1}\n{2}/{3}" },
            { "boss.place", "Place\n{0}/{1}" },

            // Block preview panel and tags
            { "odds.nothing", "Nothing under\nthis block" },
            { "odds.fires", "1 of {0} cells fires" },
            { "odds.gold", "Gold {0}%  (+${1})" },
            { "odds.monster", "Monster {0}%  ({1})" },
            { "odds.beat", "beat {0} of {1}" },
            { "odds.win", "win {0}%" },
            { "odds.trap", "Trap {0}%  (-{1} HP)" },
            { "odds.heal", "Heal {0}%  (+{1} HP)" },
            { "odds.defeat", "DEFEAT {0}%" },
            { "odds.zone", "Zone hit {0}%  (-{1} HP)" },
            { "odds.avg", "Avg ${0}   HP {1}" },
            { "tag.dead", "DEAD" },
            { "tag.deadMaybe", "DEAD?" },
            { "tag.danger", "DANGER" },

            // Shop and event screens
            { "shop.header", "SHOP    Gold {0}    HP {1}/{2}" },
            { "shop.sold", "SOLD" },
            { "shop.heal", "Heal" },
            { "event.status", "Gold {0}    HP {1}/{2}" },

            // Messages under the HUD
            { "msg.left", "Left the stage (${0} earned)" },
            { "msg.cleared", "CLEARED! Gold {0}   (Space: restart)" },
            { "msg.bossSurvived", "The boss survived! Phase {0}/{1}" },
            { "msg.bossDefeated", "Boss defeated!" },
            { "msg.cannotChoose", "Cannot choose: {0}" },
            { "msg.choiceResult", "{0}: {1}" },
            { "msg.bought", "Bought {0} ({1}) for {2}" },
            { "msg.notEnough", "Not enough gold for {0} ({1})" },
            { "msg.hpFull", "HP is already full" },
            { "msg.sold", "Already sold" },
            { "msg.nothingUnder", "Nothing under the block" },
            { "msg.monsterWin", "{0} monster {1}: {2} {3} WIN  +${4}" },
            { "msg.monsterLose", "{0} monster {1}: {2} {3} LOSE  -{4} HP" },
            { "msg.goldCell", "Gold cell  +${0}" },
            { "msg.trap", "Trap!  -{0} HP" },
            { "msg.healCell", "Heal cell  +{0} HP" },
            { "msg.nothingHappened", "Nothing happened" },
            { "msg.zoneCenter", "DANGER ZONE CENTER!  Defeated at once" },
            { "msg.zone", "DANGER ZONE!  -{0} HP" },
            { "msg.defeatedIn", "DEFEATED in stage {0}/{1}   (Space: restart)" },
            { "msg.bossOpen", "The boss is open! (${0} earned)  Press the button to defeat it." },
            { "msg.bossHurt", "The boss is hurt! (${0} earned)  Press the button for the next phase." },
            { "msg.stageDone", "Stage {0} complete (${1} earned)  Press the button to go on." },

            // Effect texts made by the rules (items, events)
            { "fb.hpFull", "HP full" },
            { "eff.gold", "Gold {0}" },
            { "eff.heal", "HP +{0}" },
            { "eff.damage", "HP -{0}" },
            { "eff.maxhp", "Max HP {0}" },
            { "eff.loseItem", "Lose an item" },
            { "eff.getItem", "Get {0}" },
            { "eff.getUnknown", "Get ?" },
            { "eff.tile", "{0} cells {1}%" },
            { "eff.noItem", "No item" },
            { "eff.lost", "Lost {0}" },
            { "eff.nothing", "Nothing" },
            { "eff.nothingHappened", "Nothing happened" },
        };

        static readonly Dictionary<string, string> KoreanTable = new Dictionary<string, string>
        {
            { "title.start", "시작" },
            { "title.howto", "게임 방법" },
            { "title.settings", "환경 설정" },
            { "title.quit", "게임 종료" },
            { "title.tagline", "블록을 놓으세요.\n덮은 칸 중 하나만 터집니다." },
            { "title.hint", "또는 스페이스 키" },

            { "tut.next", "다음" },
            { "tut.back", "이전" },
            { "tut.skip", "건너뛰기" },
            { "tut.choose", "캐릭터 선택" },
            { "tut.hint", "{0} / {1}      스페이스: 다음    왼쪽: 이전    Esc: 건너뛰기" },
            { "tut.0.title", "게임 판" },
            { "tut.0.body", "모든 스테이지는 5x5 판입니다.\n칸에는 몬스터, 골드, 회복,\n함정, 보이드가 있습니다.\n\n살아남아 모든 스테이지를 깨세요." },
            { "tut.1.title", "블록 놓기" },
            { "tut.1.body", "블록을 하나 골라 판 위로\n끌어다 놓습니다.\nR 키나 우클릭으로 회전합니다.\n\nE 키로 블록을 새로 뽑습니다." },
            { "tut.2.title", "한 칸만 터진다" },
            { "tut.2.body", "블록은 여러 칸을 덮지만\n그중 한 칸만 터집니다.\n확률은 각 칸에 표시됩니다.\n\n덮인 칸은 어느 쪽이든\n사용된 것으로 칩니다." },
            { "tut.3.title", "경고 표시" },
            { "tut.3.body", "빨간 사망 표시는 그 칸이\n게임 오버가 될 수 있다는 뜻입니다.\n보라색은 보스의 위험 구역으로,\n칸의 효과와 별도로\n피해를 줍니다." },
            { "tut.4.title", "스테이지" },
            { "tut.4.body", "전투, 이벤트, 상점을 지나\n두 단계 보스에 도달합니다.\n스페이스로 스테이지를 일찍\n마칠 수 있습니다.\n\n행운을 빕니다!" },

            { "select.heading", "캐릭터 선택" },
            { "select.hint", "카드를 클릭하거나 1 / 2 키를 누르세요" },
            { "select.back", "뒤로" },

            { "end.cleared", "클리어!" },
            { "end.defeated", "패배" },
            { "end.again", "다시 하기" },
            { "end.allDone", "모든 스테이지 완료" },
            { "end.reached", "{0}/{1} 스테이지까지 도달" },
            { "end.items", "아이템: {0}" },
            { "end.noItems", "아이템 없음" },
            { "end.stats", "골드 {0}    HP {1}/{2}    공격 {3}    마법 {4}" },

            { "set.menu", "메뉴" },
            { "set.title", "설정" },
            { "set.music", "배경음악" },
            { "set.sfx", "효과음" },
            { "set.fullscreen", "전체 화면" },
            { "set.resolution", "해상도" },
            { "set.language", "언어" },
            { "set.on", "켜짐" },
            { "set.off", "꺼짐" },
            { "set.toTitle", "타이틀로 돌아가기" },
            { "set.confirm", "한 번 더 누르면 돌아갑니다" },
            { "set.close", "닫기" },
            { "set.languageName", "한국어" },

            { "stat.atk", "공격" },
            { "stat.mag", "마법" },
            { "stat.hp", "HP" },
            { "stat.maxhp", "최대 HP" },
            { "stat.gold", "골드" },
            { "kind.magic", "마법" },
            { "kind.physical", "물리" },
            { "cell.Monster", "몬스터" },
            { "cell.Gold", "골드" },
            { "cell.Heal", "회복" },
            { "cell.Void", "보이드" },
            { "cell.Trap", "함정" },
            { "cell.Empty", "빈" },
            { "stage.Combat", "전투" },
            { "stage.Event", "이벤트" },
            { "stage.Shop", "상점" },
            { "stage.Boss", "보스" },

            { "hud.stage", "{0}/{1} 스테이지 {2}" },
            { "hud.blocks", "블록 {0}/{1}" },
            { "hud.noEvents", "이벤트\n(이벤트 없음)" },
            { "hud.items", "아이템" },
            { "hud.none", "없음" },
            { "hud.info", "정보" },
            { "hud.skill", "스킬: 추가 예정" },
            { "hud.weights", "칸 확률 가중치" },
            { "hud.weightsNone", "아직 변화 없음" },
            { "hud.next", "다음: {0}  ({1}/{2})" },
            { "hud.nextClear", "다음: 클리어" },

            { "btn.reroll", "리롤 (E)\n{0}회 남음" },
            { "btn.chooseOne", "하나를\n고르세요" },
            { "btn.continue", "계속" },
            { "btn.restart", "다시 시작" },
            { "btn.place", "배치\n{0}/{1}" },
            { "btn.leaveCount", "나가기 {0}/{1}" },
            { "btn.bossCount", "보스 {0}/{1}" },
            { "btn.defeatBoss", "보스\n처치" },
            { "btn.nextPhase", "다음\n단계" },
            { "btn.nextStage", "다음\n스테이지" },
            { "btn.leave", "나가기" },

            { "boss.title", "보스" },
            { "boss.clickDefeat", "클릭하여\n처치!" },
            { "boss.clickNext", "클릭:\n다음 단계" },
            { "boss.phase", "{0}/{1} 단계\n{2}/{3}" },
            { "boss.place", "배치\n{0}/{1}" },

            { "odds.nothing", "블록 아래에\n아무것도 없음" },
            { "odds.fires", "{0}칸 중 1칸이 터짐" },
            { "odds.gold", "골드 {0}%  (+${1})" },
            { "odds.monster", "몬스터 {0}%  ({1})" },
            { "odds.beat", "{1}마리 중 {0}마리 처치" },
            { "odds.win", "승리 {0}%" },
            { "odds.trap", "함정 {0}%  (-{1} HP)" },
            { "odds.heal", "회복 {0}%  (+{1} HP)" },
            { "odds.defeat", "패배 {0}%" },
            { "odds.zone", "구역 피해 {0}%  (-{1} HP)" },
            { "odds.avg", "평균 ${0}   HP {1}" },
            { "tag.dead", "사망" },
            { "tag.deadMaybe", "사망?" },
            { "tag.danger", "위험" },

            { "shop.header", "상점    골드 {0}    HP {1}/{2}" },
            { "shop.sold", "품절" },
            { "shop.heal", "회복" },
            { "event.status", "골드 {0}    HP {1}/{2}" },

            { "msg.left", "스테이지를 떠남 (획득 ${0})" },
            { "msg.cleared", "클리어! 골드 {0}   (스페이스: 다시 시작)" },
            { "msg.bossSurvived", "보스가 버텼다! {0}/{1} 단계" },
            { "msg.bossDefeated", "보스를 쓰러뜨렸다!" },
            { "msg.cannotChoose", "선택할 수 없음: {0}" },
            { "msg.choiceResult", "{0}: {1}" },
            { "msg.bought", "구매: {0} ({1}) - {2}" },
            { "msg.notEnough", "골드 부족: {0} ({1})" },
            { "msg.hpFull", "HP가 이미 가득 찼습니다" },
            { "msg.sold", "이미 팔렸습니다" },
            { "msg.nothingUnder", "블록 아래에 아무것도 없음" },
            { "msg.monsterWin", "{0} 몬스터 {1}: {2} {3} 승리  +${4}" },
            { "msg.monsterLose", "{0} 몬스터 {1}: {2} {3} 패배  -{4} HP" },
            { "msg.goldCell", "골드 칸  +${0}" },
            { "msg.trap", "함정!  -{0} HP" },
            { "msg.healCell", "회복 칸  +{0} HP" },
            { "msg.nothingHappened", "아무 일도 없었다" },
            { "msg.zoneCenter", "위험 구역 중심!  즉시 패배" },
            { "msg.zone", "위험 구역!  -{0} HP" },
            { "msg.defeatedIn", "{0}/{1} 스테이지에서 패배   (스페이스: 다시 시작)" },
            { "msg.bossOpen", "보스가 열렸다! (획득 ${0})  버튼을 눌러 쓰러뜨리세요." },
            { "msg.bossHurt", "보스가 다쳤다! (획득 ${0})  버튼을 눌러 다음 단계로." },
            { "msg.stageDone", "{0} 스테이지 완료 (획득 ${1})  버튼을 눌러 계속." },

            { "fb.hpFull", "HP 가득" },
            { "eff.gold", "골드 {0}" },
            { "eff.heal", "HP +{0}" },
            { "eff.damage", "HP -{0}" },
            { "eff.maxhp", "최대 HP {0}" },
            { "eff.loseItem", "아이템 하나를 잃음" },
            { "eff.getItem", "{0} 획득" },
            { "eff.getUnknown", "획득 ?" },
            { "eff.tile", "{0} 칸 {1}%" },
            { "eff.noItem", "아이템 없음" },
            { "eff.lost", "{0} 잃음" },
            { "eff.nothing", "없음" },
            { "eff.nothingHappened", "아무 일도 없었다" },

            // Config content, found by id
            { "char.Warrior.name", "전사" },
            { "char.Warrior.desc", "물리 몬스터에 강하다.\n튼튼하다." },
            { "char.Mage.name", "마법사" },
            { "char.Mage.desc", "마법 몬스터에 강하다.\n약하다." },

            { "item.power_ring.name", "힘의 반지" },
            { "item.life_charm.name", "생명의 부적" },
            { "item.arcane_ring.name", "비전의 반지" },

            { "event.lucky_shrine.title", "행운의 사당" },
            { "event.lucky_shrine.desc", "작은 사당이 희미하게 빛난다.\n그 축복이 운을 당신 쪽으로\n기울인다." },
            { "event.lucky_shrine.c0", "건강을 빈다" },
            { "event.lucky_shrine.c1", "재물을 빈다" },
            { "event.lucky_shrine.c2", "안전을 빈다" },

            { "event.shady_peddler.title", "수상한 행상인" },
            { "event.shady_peddler.desc", "행상인이 당신을 보며 씩 웃는다.\n\"모든 것엔 값이 있지.\"" },
            { "event.shady_peddler.c0", "피를 판다" },
            { "event.shady_peddler.c1", "강장제를 산다" },
            { "event.shady_peddler.c2", "그냥 간다" },

            { "event.wandering_trader.title", "떠돌이 상인" },
            { "event.wandering_trader.desc", "상인이 짐을 푼다.\n\"적당한 값에 부적 어때?\"" },
            { "event.wandering_trader.c0", "부적을 산다" },
            { "event.wandering_trader.c1", "그냥 간다" },

            { "event.old_chest.title", "낡은 상자" },
            { "event.old_chest.desc", "녹슨 상자, 자물쇠가 손을 문다.\n안에서 무언가 반짝인다." },
            { "event.old_chest.c0", "억지로 연다" },
            { "event.old_chest.c1", "내버려 둔다" },

            { "event.cursed_altar.title", "저주받은 제단" },
            { "event.cursed_altar.desc", "제단이 제물을 갈망한다.\n당신의 아이템 하나를\n가져갈 것이다." },
            { "event.cursed_altar.c0", "제물을 바친다" },
            { "event.cursed_altar.c1", "떠난다" },

            { "event.whetstone_rune.title", "숫돌과 룬석" },
            { "event.whetstone_rune.desc", "그루터기 위에 도구 두 개가 놓여 있다.\n하나만 가져갈 수 있다." },
            { "event.whetstone_rune.c0", "칼날을 간다" },
            { "event.whetstone_rune.c1", "룬을 새긴다" },
            { "event.whetstone_rune.c2", "그냥 간다" },

            { "event.twin_altar.title", "쌍둥이 제단" },
            { "event.twin_altar.desc", "칼날과 불꽃, 두 개의 제단.\n하나는 주고, 하나는 가져간다." },
            { "event.twin_altar.c0", "칼날 앞에 무릎 꿇는다" },
            { "event.twin_altar.c1", "불꽃 앞에 무릎 꿇는다" },
            { "event.twin_altar.c2", "떠난다" },
        };

        static Dictionary<string, string> Table => Korean ? KoreanTable : English;

        /// <summary>The text for this key in the current language (English if it has no translation, the key itself if unknown).</summary>
        public static string T(string key)
        {
            if (Table.TryGetValue(key, out var text)) return text;
            return English.TryGetValue(key, out text) ? text : key;
        }

        public static string T(string key, params object[] args) => string.Format(T(key), args);

        /// <summary>Like <see cref="T(string)"/> but returns <paramref name="fallback"/> when the key is not translated (for texts that live in the config).</summary>
        public static string TOr(string key, string fallback)
        {
            if (Korean && KoreanTable.TryGetValue(key, out var text)) return text;
            return fallback;
        }

        /// <summary>The word for a stat or cell kind in the current language, e.g. T("stat.atk") or CellName(CellType.Gold).</summary>
        public static string CellName(CellType type) => T("cell." + type);

        /// <summary>A signed number such as "+2" or "-3" (nothing for zero would read oddly, so zero is "+0").</summary>
        public static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }
}
