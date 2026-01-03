using bifeldy_lib_90.Libraries;

namespace bifeldy_lib_90.Models {

    public sealed class CTableClassModel {
        public string table_name { get; set; }
        public List<CDynamicClassProperty> properties { get; set; }
    }

    public sealed class CPocoModel {
        public string poco_name { get; set; }
        public List<CDynamicClassPropertyV2> properties { get; set; }
    }

}