namespace Launcher.Lib {
    /// <summary>
    /// Arguments for the UpdateProgress event
    /// </summary>
    public class UpdateProgressEventArgs {
        /// <summary>
        /// The progress, from 0-1
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value"></param>
        public UpdateProgressEventArgs(float value) {
            Value = value;
        }
    }
}