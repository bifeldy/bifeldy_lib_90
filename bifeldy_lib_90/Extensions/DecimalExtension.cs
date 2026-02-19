namespace bifeldy_lib_90.Extensions {

    public static class DecimalExtension {

        // Tidak bisa override `decimal` (karena tipenya `struct`)
        public static string ToString(this decimal value, bool removeTrail) {
            if (removeTrail) {
                value = value.RemoveTrail();
            }

            return value.ToString();
        }

        public static decimal RemoveTrail(this decimal value) {
            // https://learn.microsoft.com/en-us/dotnet/api/system.decimal.getbits
            return value / 1.000000000000000000000000000000000m;
        }

    }

}