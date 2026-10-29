using System;

namespace ProjectVector.Core
{
    public enum ProjectScene { Unknown, Login, Lobby, Match, Result, RankResult, Practice, Tutorial }

    // Serialized scene assets keep their existing names and GUIDs.
    // Character selection/loading are phases within Match, not separate live scenes.
    public static class SceneCatalog
    {
        public static string Name(ProjectScene scene) => scene switch
        {
            ProjectScene.Login => "Login",
            ProjectScene.Lobby => "Lobby",
            ProjectScene.Match => "Game_TestVersion",
            ProjectScene.Result => "Result",
            ProjectScene.RankResult => "RankResult",
            ProjectScene.Practice => "WeaponTest",
            ProjectScene.Tutorial => "Tutorial",
            _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, "Unknown scene destination")
        };

        public static ProjectScene Classify(string name) => name switch
        {
            "Login" => ProjectScene.Login,
            "Lobby" => ProjectScene.Lobby,
            "Game_TestVersion" => ProjectScene.Match,
            "Result" => ProjectScene.Result,
            "RankResult" => ProjectScene.RankResult,
            "WeaponTest" => ProjectScene.Practice,
            "Tutorial" => ProjectScene.Tutorial,
            _ => ProjectScene.Unknown
        };

        public static bool IsMatch(string name) => Classify(name) == ProjectScene.Match;
        public static bool CanReconnect(string name) => Classify(name) is
            ProjectScene.Match or ProjectScene.Result or ProjectScene.RankResult;

        public static bool CanShowVoice(string name, bool authenticated, bool gameplayVisible, bool readyRoom, bool privateRoom) =>
            authenticated && (Classify(name) switch
            {
                ProjectScene.Match => gameplayVisible,
                ProjectScene.Lobby => readyRoom || privateRoom,
                _ => false
            });
    }
}
