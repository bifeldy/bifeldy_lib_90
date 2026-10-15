namespace bifeldy_lib_90.Libraries {

    public static class ToCsv {

        public static string CheckHeaderLineCsv(string name, bool useDoubleQuote = true, bool allUppercase = true) {
            if (allUppercase) {
                name = name.ToUpper();
            }

            if (useDoubleQuote) {
                name = $"\"{name.Replace("\"", "\"\"")}\"";
            }

            return name;
        }

        public static string CheckRowLineCsv(object value, string delimiter, bool useDoubleQuote = true, bool allUppercase = true) {
            if (value == null) {
                return "";
            }

            string text;
            bool isBinary = false;
            if (value is byte[] rawBytes) {
                text = Convert.ToBase64String(rawBytes);
                isBinary = true;
            }
            else if (value is byte?[] nullableBytes) {
                text = Convert.ToBase64String([.. nullableBytes.Select(b => b ?? 0)]);
                isBinary = true;
            }
            else if (value is DateTime dt) {
                text = dt.ToString("O");
            }
            else {
                text = value.ToString();
            }

            if (allUppercase && !isBinary) {
                text = text.ToUpper();
            }

            bool mustQuote = text.Contains(delimiter) || text.Contains('"') || text.Contains('\n') || text.Contains('\r');
            if (useDoubleQuote || mustQuote) {
                text = $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }

        public record CsvColumnMapping(string Name, Func<object, object> GetValue);

    }

}