namespace bifeldy_lib_90.Attributes {

    public abstract class DenyAccessAttribute : Attribute {
        //
    }

    /* ** */

    public class DenyAccessNonDc : DenyAccessAttribute {
        //
    }

    public class DenyAccessHo : DenyAccessAttribute {
        //
    }

    public class DenyAccessDcHo : DenyAccessHo {
        //
    }

    public class DenyAccessWhHo : DenyAccessHo {
        //
    }

    public class DenyAccessAllDc : DenyAccessAttribute {
        //
    }

    /* ** */

    public class DenyAccessInduk : DenyAccessAllDc {
        //
    }

    public class DenyAccessDepo : DenyAccessAllDc {
        //
    }

    public class DenyAccessKonvinience : DenyAccessAllDc {
        //
    }

    public class DenyAccessIplaza : DenyAccessAllDc {
        //
    }

    public class DenyAccessFrozen : DenyAccessAllDc {
        //
    }

    public class DenyAccessPerishable : DenyAccessAllDc {
        //
    }

    public class DenyAccessLpg : DenyAccessAllDc {
        //
    }

    public class DenyAccessSewa : DenyAccessAllDc {
        //
    }

}