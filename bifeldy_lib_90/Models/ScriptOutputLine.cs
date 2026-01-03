namespace bifeldy_lib_90.Models {

    public sealed class CScriptOutputLine {

        public string Line { get; private set; }
        public bool IsErrorLine { get; private set; }

        public CScriptOutputLine(string line, bool isErrorLine) {
            this.Line = line;
            this.IsErrorLine = isErrorLine;
        }

    }

}