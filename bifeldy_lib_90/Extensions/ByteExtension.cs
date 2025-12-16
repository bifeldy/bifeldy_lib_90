namespace bifeldy_lib_90.Extensions {

    public static class ByteExtension {

        public static string ToStringHex(this byte[] bytes, bool removeHypens = true, bool lower = true) {
            string hex = BitConverter.ToString(bytes);
            string ret = removeHypens ? hex.Replace("-", "") : hex;
            return lower ? ret.ToLower() : ret;
        }

        public static IEnumerable<byte[]> Split(this byte[] value, int bufferLength) {
            int countOfArray = value.Length / bufferLength;
            if (value.Length % bufferLength > 0) {
                countOfArray++;
            }

            for (int i = 0; i < countOfArray; i++) {
                yield return value.Skip(i * bufferLength).Take(bufferLength).ToArray();
            }
        }

    }

}