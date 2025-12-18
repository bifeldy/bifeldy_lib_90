namespace bifeldy_lib_90.Libraries {

    public sealed class Crc32 {

        private uint[] _table { get; set; }

        public Crc32() {
            this._table = this.CreateTable();
        }

        private uint[] CreateTable() {
            const uint poly = 0xEDB88320u;
            uint[] table = new uint[256];

            for (uint i = 0; i < table.Length; i++) {
                uint crc = i;
                for (int j = 0; j < 8; j++) {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
                }

                table[i] = crc;
            }

            return table;
        }

        public uint Compute(Stream stream) {
            uint crc = 0xFFFFFFFFu;
            int b;

            while ((b = stream.ReadByte()) != -1) {
                crc = (crc >> 8) ^ this._table[(crc ^ (byte)b) & 0xFF];
            }

            return ~crc;
        }

    }

}
