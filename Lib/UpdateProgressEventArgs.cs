namespace Launcher.Lib {
    public class UpdateProgressEventArgs {
        public float Value { get; }

        public UpdateProgressEventArgs(float value) {
            Value = value;
        }
    }
}