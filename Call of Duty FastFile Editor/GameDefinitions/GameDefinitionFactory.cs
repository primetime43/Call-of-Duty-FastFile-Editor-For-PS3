using Call_of_Duty_FastFile_Editor.Models;

namespace Call_of_Duty_FastFile_Editor.GameDefinitions
{
    /// <summary>
    /// Factory for creating game-specific definition instances.
    /// Provides the appropriate IGameDefinition based on the FastFile being opened.
    /// </summary>
    public static class GameDefinitionFactory
    {
        // Singleton instances for each game (they're stateless so reuse is safe)
        private static readonly CoD4GameDefinition _cod4 = new();
        private static readonly CoD5GameDefinition _cod5 = new();
        private static readonly MW2GameDefinition _mw2 = new();

        /// <summary>
        /// Gets the appropriate game definition for the given FastFile.
        /// </summary>
        /// <param name="fastFile">The opened FastFile.</param>
        /// <returns>The game-specific definition.</returns>
        /// <exception cref="NotSupportedException">Thrown when the game is not supported.</exception>
        public static IGameDefinition GetDefinition(FastFile fastFile)
        {
            if (fastFile.IsCod4File)
                return _cod4;
            if (fastFile.IsCod5File)
                return _cod5;
            if (fastFile.IsMW2File)
                return _mw2;

            throw new NotSupportedException($"Unsupported game version: 0x{fastFile.GameVersion:X}");
        }

        /// <summary>
        /// Gets the appropriate game definition by version value.
        /// </summary>
        /// <param name="versionValue">The game version value from the FastFile header.</param>
        /// <returns>The game-specific definition, or null if not recognized.</returns>
        public static IGameDefinition? GetDefinitionByVersion(int versionValue)
        {
            // CoD4
            if (versionValue == CoD4Definition.VersionValue || versionValue == CoD4Definition.PCVersionValue)
                return _cod4;

            // CoD5/WaW
            if (versionValue == CoD5Definition.VersionValue)
                return _cod5;

            // MW2
            if (versionValue == MW2Definition.VersionValue || versionValue == MW2Definition.PCVersionValue)
                return _mw2;

            return null;
        }

        /// <summary>
        /// Checks if a game version is supported.
        /// </summary>
        public static bool IsSupported(int versionValue)
        {
            return GetDefinitionByVersion(versionValue) != null;
        }

        /// <summary>
        /// Gets the CoD4 game definition.
        /// </summary>
        public static IGameDefinition CoD4 => _cod4;

        /// <summary>
        /// Gets the CoD5/WaW game definition.
        /// </summary>
        public static IGameDefinition CoD5 => _cod5;

        /// <summary>
        /// Gets the MW2 game definition.
        /// </summary>
        public static IGameDefinition MW2 => _mw2;
    }
}
