namespace bifeldy_lib_90.Models {

    public sealed class CCsvColumn {
        public string ColumnName { get; set; }
        public int Position { get; set; } = 0;
        public Type FieldType { get; set; }
        public string FieldName { get; set; }
    }

}