using System.Collections.Generic;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// Texts of the screens that are translated so far (title, tutorial, character select, end screen, settings).
    /// Looked up by key in the current language; a key missing in Korean falls back to English. Everything else in the
    /// game (HUD, shop, events, boss) is still English only.
    /// </summary>
    public static class Loc
    {
        public const int TutorialPageCount = 5;

        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "title.start", "Start" },
            { "title.howto", "How to Play" },
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
        };

        static readonly Dictionary<string, string> Korean = new Dictionary<string, string>
        {
            { "title.start", "시작" },
            { "title.howto", "게임 방법" },
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
            { "tut.3.body", "빨간 DEAD 표시는 그 칸이\n게임 오버가 될 수 있다는 뜻입니다.\n보라색은 보스의 위험 구역으로,\n칸의 효과와 별도로\n피해를 줍니다." },
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

            { "char.Warrior.name", "전사" },
            { "char.Warrior.desc", "물리 몬스터에 강하다.\n튼튼하다." },
            { "char.Mage.name", "마법사" },
            { "char.Mage.desc", "마법 몬스터에 강하다.\n약하다." },
        };

        static Dictionary<string, string> Table => GameSettings.Language == Language.Korean ? Korean : English;

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
            if (GameSettings.Language == Language.Korean && Korean.TryGetValue(key, out var text)) return text;
            return fallback;
        }
    }
}
