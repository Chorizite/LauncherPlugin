using System;

namespace Launcher.Lib {
    /// <summary>
    /// Screen changed
    /// </summary>
    public class ScreenChangedEventArgs : EventArgs {
        /// <summary>
        /// The previous screen
        /// </summary>
        public LauncherScreen OldScreen { get; }

        /// <summary>
        /// The new screen
        /// </summary>
        public LauncherScreen NewScreen { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="oldScreen"></param>
        /// <param name="newScreen"></param>
        public ScreenChangedEventArgs(LauncherScreen oldScreen, LauncherScreen newScreen) {
            OldScreen = oldScreen;
            NewScreen = newScreen;
        }
    }
}